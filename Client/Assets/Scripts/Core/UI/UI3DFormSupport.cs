using System.Collections.Generic;
using Framework.Log;
using Framework.View;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Core.UI
{
    /// <summary>
    /// 3D UI 支持：有界面需要 3D 时把 UI 相机从正交切透视，全部关闭后恢复正交。
    /// 对应参考项目 ScriptsC#/Core/UI/UI3DFormSupport.cs。
    /// 依赖 URP（UniversalAdditionalCameraData / CameraRenderType）；KJ 的 ScreenHelper.UICamera 改为 UICamera.Camera。
    /// </summary>
    [RequireComponent(typeof(BaseForm))]
    public sealed class UI3DFormSupport : MonoBehaviour
    {
        // 可变 static（非 readonly）：软重启时 StaticReset 会置 null，经 RecordUseList 惰性重建，
        // 避免残留已销毁 Form 引用导致 Need3D 误判（static readonly 会被 StaticReset 跳过）。
        private static List<BaseForm> _recordUseList;

        private static List<BaseForm> RecordUseList => _recordUseList ??= new List<BaseForm>();

        /// <summary>是否已有界面需要 3D（UI 相机应切透视）。</summary>
        public static bool Need3D => RecordUseList.Count != 0;

        private BaseForm _form;

        private void Awake()
        {
            _form = GetComponent<BaseForm>();
            _form.RenderingChanged += OnFormRenderingStateChanged;
        }

        private void OnFormRenderingStateChanged(ViewBase view)
        {
            var form = view as BaseForm;
            if (form != _form) return;
            if (!form.Rendering) FormOnFormHide(form);
            else FormOnFormShow(form);
        }

        private void OnDestroy()
        {
            if (_form != null)
            {
                _form.RenderingChanged -= OnFormRenderingStateChanged;
            }
        }

        private static void FormOnFormHide(BaseForm obj)
        {
#if UNITY_EDITOR
            if (!RecordUseList.Contains(obj))
            {
                GameLog.Error($"{obj.AssetName} 没有记录打开信息就隐藏，需要检查 OnShow/OnHide 是否匹配", module: "Core.UI.UI3DFormSupport");
                return;
            }
#endif
            RecordUseList.Remove(obj);
            if (RecordUseList.Count == 0)
                SetUIRootCameraOrthoGraphic();
        }

        private static void FormOnFormShow(BaseForm obj)
        {
#if UNITY_EDITOR
            if (RecordUseList.Contains(obj))
            {
                GameLog.Error($"{obj.AssetName} 重复加载！需要检查", module: "Core.UI.UI3DFormSupport");
                return;
            }
#endif
            RecordUseList.Add(obj);
            if (RecordUseList.Count == 1)
                SetUIRootCameraPerspective();
        }

        /// <summary>UI 相机切正交。</summary>
        public static void SetUIRootCameraOrthoGraphic()
        {
            var cam = UICamera.Camera;
            if (cam == null) return;

            var data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            if (data == null) return;

            cam.nearClipPlane = 0;
            cam.orthographic = true;

            var old = data.renderType;
            data.renderType = CameraRenderType.Base; // 单个相机截图需改 Base 模式
            cam.Render(); // 渲染一帧，避免同帧坐标转换问题
            data.renderType = old;
        }

        /// <summary>UI 相机切透视。</summary>
        public static void SetUIRootCameraPerspective()
        {
            var cam = UICamera.Camera;
            if (cam == null) return;

            cam.nearClipPlane = 10;
            cam.orthographic = false;
        }
    }
}
