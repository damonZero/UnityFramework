using System.Collections.Generic;
using Framework.Log;
using Framework.Restart;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Framework.UIEffectExtensions
{
    /// <summary>
    /// 相机渲染 3D 模型到 RawImage 组件。
    /// 用专用相机把挂在「UIModel」层上的 3D 模型（通用 GameObject，不依赖 Spine）渲染到 RenderTexture，再显示到 RawImage。
    /// 对应参考项目 Package/UIEffectExtension/Runtime/UIModelImage/UIModelImg.cs。
    /// 注意：UIEffectExtension 其余 UI 特效组件（EffectImage/FrameAnimation/ImageBlur/MaskImg/GrayUI）依赖 37 专属 shader 与第三方库，按需后置，本文件仅移植 UIModelImage（UI 3D 模型）。
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RawImage))]
    [RequireComponent(typeof(RectTransform))]
    public class UIModelImg : MonoBehaviour
    {
        // buffer 设置
        public enum DepthEnum
        {
            NO_BUFFER_0,        // 无深度 buffer
            ONLY_DEPTH_16,      // 只有深度 buffer，至少 16bit
            DEPTH_STENCIL_24    // 包含深度 buffer 和模板 buffer，至少 24bit
        }

        // 播放状态
        public enum PlayStatus
        {
            STOP,    // 停止
            PAUSE,   // 暂停
            PLAYING  // 渲染
        }

        [SerializeField] private bool _renderOnAwake = true; // 是否一开始就运行
        [SerializeField] private Camera _camera; // 渲染相机
        [SerializeField] private GameObject _content; // 渲染对象父级容器，用于整体设置位置
        [SerializeField] private DepthEnum _bufferType = DepthEnum.NO_BUFFER_0; // RenderTexture 深度设置
        [SerializeField] private int _rtWidth = 128; // RenderTexture 宽
        [SerializeField] private int _rtHeight = 128; // RenderTexture 高
        [SerializeField] private GameObject _modelContainer; // 模型容器
        [SerializeField] private string layerName = "UIModel";

        // 隐藏时销毁 RenderTexture（用于低端机）
        public static bool isHideStop = false;

        // 调整 RenderTexture 精度（软重启需重置回 1，否则 RT 尺寸变 0）
        [SoftRestartField(initialValue: 1f)]
        public static float rtScale = 1;

        // 共用光源
        public static GameObject selfLight;

        // 内容节点
        public GameObject Content => _content;

        private PlayStatus _playStatus = PlayStatus.STOP;
        private RenderTexture _rt;
        private Vector2 _rtSize;

        // 模型层 id（-1 表示未初始化；软重启需重置回 -1）
        [SoftRestartField(initialValue: -1)]
        private static int _layerId = -1;

        private Vector3 _contentScale;
        private Vector3 _contentPos;
        private Quaternion _contentRotation;
        private Vector3 _rotationCache;

        private int _index = -1; // 模型的位置索引

        private void Awake()
        {
            _layerId = LayerMask.NameToLayer(layerName);
            if (_content != null)
            {
                // 保存初始缩放、旋转
                _contentScale = _content.transform.localScale;
                _contentRotation = _content.transform.localRotation;
            }
        }

        private void OnEnable()
        {
            // 显示时计算初始位置
            UIModelLocMgr.GetEmptyPos(out _index, out _contentPos);
            Init();
            // 启用时自动播放
            if (_renderOnAwake)
            {
                Play();
            }
            else if (_rt != null)
            {
                _rt.DiscardContents(true, true);
            }
        }

        private void OnDisable()
        {
            if (!isHideStop)
            {
                Pause();
            }
            else
            {
                Stop();
            }

            // 不显示时回收位置
            UIModelLocMgr.RecyclePos(_index);
        }

        private void Update()
        {
            if (Application.isPlaying)
                UpdateContentTransform();

#if UNITY_EDITOR
            UpdateRt();
#endif
            if (!Application.isPlaying && _camera)
            {
                _camera.Render();
            }
        }

        private void OnDestroy()
        {
            ReleaseRt();
        }

        public void OnDrawGizmos()
        {
            if (_modelContainer)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(_modelContainer.transform.position, 0.5f);
            }

            if (selfLight)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(selfLight.transform.position, 0.5f);
            }
        }

        /// <summary>播放（开始渲染）。</summary>
        public void Play()
        {
            if (_playStatus != PlayStatus.PLAYING)
            {
                _playStatus = PlayStatus.PLAYING;
                CreateRenderTexture();
                if (_camera != null)
                {
                    _camera.enabled = true;
                    _camera.targetTexture = _rt;
                }
            }
        }

        /// <summary>暂停渲染。</summary>
        public void Pause()
        {
            if (_playStatus != PlayStatus.PLAYING) return;
            _playStatus = PlayStatus.PAUSE;
            if (_camera != null) _camera.enabled = false;
        }

        /// <summary>停止渲染（清除 RenderTexture）。</summary>
        public void Stop()
        {
            if (_playStatus == PlayStatus.STOP) return;
            _playStatus = PlayStatus.STOP;
            if (_camera != null) _camera.enabled = false;
            ReleaseRt();
        }

        /// <summary>增加一个显示的模型。</summary>
        public void AddObject(GameObject obj)
        {
            AddObject(obj, Vector3.zero, Vector3.zero);
        }

        public void AddObject(GameObject obj, Vector3 localPos, Vector3 localEuler)
        {
            ChangeModelLayer(obj);
            obj.transform.SetParent(_modelContainer.transform, false);
            obj.transform.localPosition = localPos;
            obj.transform.localEulerAngles = localEuler;
        }

        /// <summary>获取当前所有显示的对象。</summary>
        public GameObject[] GetAllShowObjects()
        {
            if (_modelContainer == null) return System.Array.Empty<GameObject>();
            var list = new List<GameObject>(_modelContainer.transform.childCount);
            for (int i = 0; i < _modelContainer.transform.childCount; i++)
            {
                list.Add(_modelContainer.transform.GetChild(i).gameObject);
            }

            return list.ToArray();
        }

        /// <summary>更新容器 Transform，固定位置/旋转/缩放，避免模型受父 UI 节点影响。</summary>
        public void UpdateContentTransform()
        {
            if (!Application.isPlaying || _content == null) return; // 编辑模式不要改
            _content.transform.position = _contentPos;
            _content.transform.rotation = _contentRotation;
            var parentLossyScale = _content.transform.parent.lossyScale;
            _rotationCache.x = _contentScale.x / parentLossyScale.x;
            _rotationCache.y = _contentScale.y / parentLossyScale.y;
            _rotationCache.z = _contentScale.z / parentLossyScale.z;
            _content.transform.localScale = _rotationCache;
        }

        /// <summary>替换显示的模型（清理掉之前所有的显示模型）。</summary>
        public void ReplaceObject(GameObject obj)
        {
            ReplaceObject(obj, Vector3.zero, Vector3.zero);
        }

        public void ReplaceObject(GameObject obj, Vector3 localPos, Vector3 localEuler)
        {
            ClearObject();
            AddObject(obj, localPos, localEuler);
        }

        /// <summary>清理所有显示模型。</summary>
        public void ClearObject()
        {
            if (_modelContainer == null) return;
            for (int i = _modelContainer.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(_modelContainer.transform.GetChild(i).gameObject);
            }
        }

        /// <summary>获取渲染相机。</summary>
        public Camera GetCamera()
        {
            return _camera;
        }

        /// <summary>获取容器。</summary>
        public GameObject GetContainer()
        {
            return _modelContainer;
        }

        /// <summary>快照功能：非播放状态下一次性绘制。</summary>
        public void Snapshot()
        {
            if (_playStatus != PlayStatus.PLAYING)
            {
                if (_rt == null)
                {
                    CreateRenderTexture();
                }

                if (_camera != null)
                {
                    _camera.targetTexture = _rt;
                    _camera.Render();
                }
            }
        }

        /// <summary>设置 RenderTexture 属性。</summary>
        public void SetRenderTextureSize(int width, int height)
        {
            _rtWidth = width;
            _rtHeight = height;
            ReleaseRt();
            CreateRenderTexture();
        }

        // 修改显示模型的层级
        private void ChangeModelLayer(GameObject model)
        {
            if (_layerId < 0 || _layerId > 31)
            {
                var tmpTransform = transform;
                var str = "";
                while (tmpTransform != null)
                {
                    str = tmpTransform.name + "." + str;
                    tmpTransform = tmpTransform.parent;
                }

                GameLog.Error($"ChangeModelLayer = {_layerId} Error !!! {str}", module: nameof(UIModelImg));
                return;
            }

            if (model == null || model.layer == _layerId) return;
            model.layer = _layerId;
            Transform[] trans = model.GetComponentsInChildren<Transform>(true); // 包含非激活的 GameObject
            if (trans == null || trans.Length <= 0) return;
            foreach (Transform form in trans)
            {
                if (form == null) continue;
                form.gameObject.layer = _layerId;
            }
        }

        // 初始化（内部函数调用顺序敏感，请勿随意调整顺序）
        private void Init()
        {
            CreateRenderTexture();
            CreateContent();
            CreateSubContainers();
            CreateCamera();
        }

        // 创建相机
        private void CreateCamera()
        {
            GameObject camObj = null;
            if (_camera == null)
            {
                var camT = transform.Find("UIModelCamera");
                if (camT)
                {
                    camObj = camT.gameObject;
                }

                if (camObj == null)
                {
                    camObj = new GameObject { name = "UIModelCamera" };
                    camObj.transform.SetParent(_content.transform, false);
                    _camera = camObj.AddComponent<Camera>();
                    _camera.allowHDR = false;
                    _camera.transform.localPosition = new Vector3(0, 0, -5);
                }
                else
                {
                    _camera = camObj.GetComponent<Camera>();
                }

                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = Color.clear;
                _camera.depth = -100;
            }

            _camera.targetTexture = _rt;
            _camera.gameObject.layer = _layerId;
            _camera.cullingMask = 1 << _layerId;
            _camera.enabled = false;

            var t = _camera.gameObject.GetComponent<UIModelCam>();
            if (t == null)
            {
                t = _camera.gameObject.AddComponent<UIModelCam>();
            }

            t.lightObject = selfLight;
        }

        // 创建父级容器
        private void CreateContent()
        {
            if (_content == null)
            {
                Transform targetT = transform.Find("UIModelContent");
                if (targetT)
                {
                    _content = targetT.gameObject;
                }

                if (_content == null)
                {
                    _content = new GameObject("UIModelContent");
                    _content.transform.SetParent(transform, false);
                }

                _content.layer = _layerId;
            }
        }

        // 创建子容器
        private void CreateSubContainers()
        {
            var targetT = _content.transform.Find("ModelContainer");
            if (targetT == null)
            {
                _modelContainer = new GameObject("ModelContainer");
                _modelContainer.transform.SetParent(_content.transform, false);
                _modelContainer.gameObject.layer = _layerId;
                _modelContainer.transform.localPosition = new Vector3(0, 0, 5);
            }
            else
            {
                _modelContainer = targetT.gameObject;
            }
        }

        /// <summary>创建一个 RenderTexture。</summary>
        public static RenderTexture CreateRt(int width, int height, int depth)
        {
            var w = width;
            var h = height;
            var rt = RenderTexture.GetTemporary(Mathf.Max(w, 1), Mathf.Max(h, 1), depth);
            rt.hideFlags = HideFlags.DontSave;
            rt.name = "RenderModel " + w + " x " + h;
            rt.dimension = TextureDimension.Tex2D;
            rt.antiAliasing = 1; // 抗锯齿不修改
            rt.useDynamicScale = false;
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.filterMode = FilterMode.Bilinear;
            if (rt.format != RenderTextureFormat.ARGB32)
            {
                rt.format = RenderTextureFormat.ARGB32;
            }

            if (rt.useMipMap)
            {
                rt.useMipMap = false;
            }

            rt.depth = depth;
            return rt;
        }

        // 创建 RenderTexture
        private void CreateRenderTexture()
        {
            int depth = GetBufferBit();
            if (_rt == null)
            {
                int w = (int)(_rtWidth * rtScale);
                int h = (int)(_rtHeight * rtScale);
                _rt = RenderTexture.GetTemporary(Mathf.Max(w, 1), Mathf.Max(h, 1), depth, RenderTextureFormat.ARGB32);
                _rt.hideFlags = HideFlags.DontSave;
                _rt.name = "RenderModel " + w + " x " + h;
                _rt.dimension = TextureDimension.Tex2D;
                _rt.antiAliasing = 1;
                _rt.useDynamicScale = false;
                _rt.wrapMode = TextureWrapMode.Clamp;
                _rt.filterMode = FilterMode.Bilinear;
            }

            _rt.depth = depth;
            GetComponent<RawImage>().texture = _rt;
        }

        // 释放 RenderTexture
        private void ReleaseRt()
        {
            if (_camera)
            {
                _camera.targetTexture = null;
            }

            if (_rt)
            {
                RenderTexture.ReleaseTemporary(_rt);
                _rt = null;
            }
        }

        // 检查 RenderTexture 尺寸/深度变化
        private void UpdateRt()
        {
            if (_rt)
            {
                bool sizeChange = false;
                bool depthChange = false;
                int w = (int)(_rtWidth * rtScale);
                int h = (int)(_rtHeight * rtScale);
                if ((_rt.width != w || _rt.height != h))
                {
                    sizeChange = true;
                }

                if (_rt.depth != GetBufferBit())
                {
                    depthChange = true;
                }

                if (sizeChange || depthChange)
                {
                    ReleaseRt();
                    CreateRenderTexture();
                    if (_camera)
                    {
                        _camera.targetTexture = _rt;
                    }
                }
            }
        }

        // Depth 枚举到 buffer 数值
        private int GetBufferBit()
        {
            switch (_bufferType)
            {
                case DepthEnum.DEPTH_STENCIL_24:
                    return 24;
                case DepthEnum.ONLY_DEPTH_16:
                    return 16;
                default:
                    return 0;
            }
        }
    }
}
