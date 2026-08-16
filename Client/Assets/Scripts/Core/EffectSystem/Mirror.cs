using System;
using System.Collections;
using Framework.Log;
using Framework.Restart;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Core.EffectSystem
{
    /// <summary>
    /// 平面反射（镜子/水面）：运行时动态创建反射相机，把反射画面渲染到 RenderTexture 供材质使用。
    /// 对应参考项目 Core/EffectSystem/Mirror/Mirror.cs（去掉 37 未使用的 UniCore 依赖）。
    /// </summary>
    [ExecuteInEditMode]
    public class Mirror : MonoBehaviour
    {
        [SoftRestartField(initialValue: 1f)]
        private static float _globalTextureScale = 1f;

        /// <summary>全局倒影贴图系数参数（0 时配合 shader 分级，≤0 时跳过渲染）。</summary>
        public static float GlobalTextureScale
        {
            get => _globalTextureScale;
            set => _globalTextureScale = value;
        }

        [SoftRestartField(initialValue: 30)]
        private static int _renderingRare = 30;

        /// <summary>默认渲染帧率。</summary>
        private static int RenderingRare
        {
            get => _renderingRare;
            set => _renderingRare = value;
        }

        private static double RenderInterval { get; set; }

        [Header("贴图尺寸")] public int m_TextureSize = 256;
        [Header("反射位置偏移")] public float m_ClipPlaneOffset;
        [Header("是否翻转贴图")] public bool m_IsFlatMirror = true;
        [Header("反射贴图属性")] public string m_refProperty = "_RefTex";
        [Header("反射层")] public LayerMask m_ReflectLayers = -1;
        [Header("继承主相机环境")] public bool useMainBg = true;
        [Header("自定义环境颜色")] public Color bgCol = Color.black;
        [Header("URP渲染对象")] public int m_RenderIndex = 1;
        [Header("静态反射(只在OnEnable渲染一次)")] public bool isStatic = false;
        [Header("是否降低渲染频率)")] public bool _lowRare = true;
        [Header("指定渲染对象)")] public new Renderer renderer;
        [Header("使用世界空间方向")] public bool useWorldUp = true;

        private readonly Hashtable _mReflectionCameras = new Hashtable();
        private RenderTexture _mReflectionTexture;
        private int _mOldReflectionTextureSize;

        private bool _sInsideRendering;
        private int _mRefPropertyId;
        private Material[] _waterMats;

        private UniversalRenderPipelineAsset _urpasset;

        private bool _canIntervalRendering;
        private float _cumulativeInterval;

        public void ChangeReflectLayers(int layers)
        {
            m_ReflectLayers.value = layers;
        }

        private void MyCameraRendering(ScriptableRenderContext context, Camera[] camera)
        {
            if (GlobalTextureScale <= 0 || !enabled || _waterMats == null)
                return;

            if (_lowRare)
            {
                if (!_canIntervalRendering)
                    return;

                _canIntervalRendering = false;
            }

            var cam = Camera.main;
            if (cam == null) return;
            if (cam.orthographic) return;

            if (_sInsideRendering)
                return;
            _sInsideRendering = true;

            CreateMirrorObjects(cam, out var reflectionCamera);

            var pos = transform.position;
            Vector3 normal;
            if (m_IsFlatMirror)
                normal = useWorldUp ? Vector3.up : transform.up;
            else
            {
                normal = transform.position - cam.transform.position;
                normal.Normalize();
            }

            UpdateCameraModes(cam, reflectionCamera);

            var d = -Vector3.Dot(normal, pos) - m_ClipPlaneOffset;
            var reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);

            var reflection = Matrix4x4.zero;
            CalculateReflectionMatrix(ref reflection, reflectionPlane);
            var oldpos = cam.transform.position;
            var newpos = reflection.MultiplyPoint(oldpos);
            reflectionCamera.worldToCameraMatrix = cam.worldToCameraMatrix * reflection;

            var clipPlane = CameraSpacePlane(reflectionCamera, pos, normal, 1.0f);
            var projection = cam.projectionMatrix;
            CalculateObliqueMatrix(ref projection, clipPlane);
            reflectionCamera.projectionMatrix = projection;

            reflectionCamera.cullingMask = ~(1 << 4) & m_ReflectLayers.value;
            reflectionCamera.targetTexture = _mReflectionTexture;
            GL.invertCulling = true;
            var transform1 = reflectionCamera.transform;
            transform1.position = newpos;
            var euler = cam.transform.eulerAngles;
            transform1.eulerAngles = new Vector3(0, euler.y, euler.z);

            if (!reflectionCamera.orthographic)
            {
                UniversalRenderPipeline.RenderSingleCamera(context, reflectionCamera);
            }

            foreach (var mat in _waterMats)
            {
                if (mat.HasProperty(_mRefPropertyId))
                    mat.SetTexture(_mRefPropertyId, _mReflectionTexture);
            }

            reflectionCamera.transform.position = oldpos;
            GL.invertCulling = false;
            _sInsideRendering = false;
        }

        /// <summary>更新刷新频率。</summary>
        public static void UpdateRenderingRare(int rate)
        {
            if (rate <= 0)
            {
                GameLog.Error("set mirror render rate err " + rate, nameof(Mirror));
                return;
            }

            // 默认 24 否则会抖动
            RenderingRare = Math.Clamp(rate, 24, 60);
            RenderInterval = Math.Round(100d / RenderingRare) * 0.01;
        }

        private void Awake()
        {
            _urpasset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
        }

        private void OnEnable()
        {
            if (_lowRare && !isStatic)
                UpdateRenderingRare(RenderingRare);

            _mRefPropertyId = Shader.PropertyToID(m_refProperty);
            var r = renderer ? renderer : GetComponent<Renderer>();
            if (r == null) return;
            _waterMats = r.sharedMaterials;
            RenderPipelineManager.beginFrameRendering += MyCameraRendering;

            if (isStatic && Application.isPlaying)
                RenderPipelineManager.endFrameRendering += OnEndFrameRendering;

#if UNITY_EDITOR
            if (Application.isPlaying && (m_ReflectLayers.value & LayerMask.GetMask("UI")) != 0)
            {
                GameLog.Error("Mirror m_ReflectLayers 不应包含 UI 层", nameof(Mirror));
            }
#endif
        }

        private void OnDisable()
        {
            if (_mReflectionTexture)
            {
                DestroyObject(_mReflectionTexture);
                _mReflectionTexture = null;
            }

            ClearRendering();
            _cumulativeInterval = 0;
        }

        private void Update()
        {
            if (_canIntervalRendering || isStatic || !_lowRare) return;

            _cumulativeInterval += Time.deltaTime;
            if (_cumulativeInterval < RenderInterval) return;

            _cumulativeInterval = 0;
            _canIntervalRendering = true;
        }

        private void ClearRendering()
        {
            foreach (DictionaryEntry kvp in _mReflectionCameras)
                DestroyObject(((Camera)kvp.Value).gameObject);
            _mReflectionCameras.Clear();
            RenderPipelineManager.beginFrameRendering -= MyCameraRendering;
            RenderPipelineManager.endFrameRendering -= OnEndFrameRendering;
        }

        private void UpdateCameraModes(Camera src, Camera dest)
        {
            if (dest == null) return;

            if (useMainBg)
            {
                dest.clearFlags = src.clearFlags;
                dest.backgroundColor = src.backgroundColor;
            }
            else
            {
                dest.clearFlags = CameraClearFlags.SolidColor;
                dest.backgroundColor = bgCol;
            }

            dest.farClipPlane = src.farClipPlane;
            dest.nearClipPlane = src.nearClipPlane;
            dest.orthographic = src.orthographic;
            dest.fieldOfView = src.fieldOfView;
            dest.aspect = src.aspect;
            dest.orthographicSize = src.orthographicSize;
            dest.renderingPath = src.renderingPath;
        }

        private void CreateMirrorObjects(Camera currentCamera, out Camera reflectionCamera)
        {
            reflectionCamera = null;
            var nowSize = (int)(GlobalTextureScale * m_TextureSize * _urpasset.renderScale);
            if (!_mReflectionTexture || _mOldReflectionTextureSize != nowSize)
            {
                if (_mReflectionTexture)
                    DestroyObject(_mReflectionTexture);
                _mReflectionTexture = new RenderTexture(nowSize, nowSize, 16)
                {
                    name = "__MirrorReflection" + GetInstanceID(),
                    isPowerOfTwo = true,
                    hideFlags = HideFlags.DontSave
                };
                _mOldReflectionTextureSize = nowSize;
            }

            reflectionCamera = _mReflectionCameras[currentCamera] as Camera;
            if (reflectionCamera) return;

            var go = new GameObject(
                "Mirror Refl Camera id" + GetInstanceID() + " for " + currentCamera.GetInstanceID(),
                typeof(Camera), typeof(Skybox));
            reflectionCamera = go.GetComponent<Camera>();
            reflectionCamera.enabled = false;
            var transform1 = reflectionCamera.transform;
            var transform2 = transform;
            transform1.position = transform2.position;
            transform1.rotation = transform2.rotation;
            reflectionCamera.gameObject.AddComponent<FlareLayer>();
            go.hideFlags = HideFlags.HideAndDontSave;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.SetRenderer(m_RenderIndex);
            _mReflectionCameras[currentCamera] = reflectionCamera;
        }

        private void OnEndFrameRendering(ScriptableRenderContext context, Camera[] cameras)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                var reflectionCamera = _mReflectionCameras[cam] as Camera;
                if (reflectionCamera != null) reflectionCamera.targetTexture = null;
            }

            ClearRendering();
        }

        private static float Sgn(float a)
        {
            if (a > 0.0f) return 1.0f;
            if (a < 0.0f) return -1.0f;
            return 0.0f;
        }

        private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
        {
            var offsetPos = pos + normal * m_ClipPlaneOffset;
            var m = cam.worldToCameraMatrix;
            var cpos = m.MultiplyPoint(offsetPos);
            var cnormal = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
        }

        private static void CalculateObliqueMatrix(ref Matrix4x4 projection, Vector4 clipPlane)
        {
            var q = projection.inverse * new Vector4(
                Sgn(clipPlane.x),
                Sgn(clipPlane.y),
                1.0f,
                1.0f
            );
            var c = clipPlane * (2.0F / Vector4.Dot(clipPlane, q));

            projection[2] = c.x - projection[3];
            projection[6] = c.y - projection[7];
            projection[10] = c.z - projection[11];
            projection[14] = c.w - projection[15];
        }

        private static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
        {
            reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
            reflectionMat.m01 = (-2F * plane[0] * plane[1]);
            reflectionMat.m02 = (-2F * plane[0] * plane[2]);
            reflectionMat.m03 = (-2F * plane[3] * plane[0]);

            reflectionMat.m10 = (-2F * plane[1] * plane[0]);
            reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
            reflectionMat.m12 = (-2F * plane[1] * plane[2]);
            reflectionMat.m13 = (-2F * plane[3] * plane[1]);

            reflectionMat.m20 = (-2F * plane[2] * plane[0]);
            reflectionMat.m21 = (-2F * plane[2] * plane[1]);
            reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
            reflectionMat.m23 = (-2F * plane[3] * plane[2]);

            reflectionMat.m30 = 0F;
            reflectionMat.m31 = 0F;
            reflectionMat.m32 = 0F;
            reflectionMat.m33 = 1F;
        }

        private new static void DestroyObject(Object obj)
        {
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
    }
}
