using Framework.Coverage;
using Framework.UIEffectExtensions;
using Framework.View;
using UnityEngine;

namespace Core.ViewSystem
{
    /// <summary>
    /// Coverage 运行时检测组件（对应参考项目 ScriptsC#/Core/ViewSystem/Coverage/CoverageChecker）。
    /// 挂载在 SafeUIRoot 上，监听动态创建的相机（UIModelCam），为其补挂 <see cref="CameraCoverageChild"/>
    /// （界面内相机）或 <see cref="CameraCoverage"/>（场景相机），使其参与遮挡渲染检测。
    /// </summary>
    public class CoverageChecker : MonoBehaviour
    {
        /// <summary>UI 相机引用。</summary>
        public Camera uiCamera;

        /// <summary>基础场景相机引用（当前 KJ 无独立 base 相机，对应参考项目 prefab 中 baseCamera=null）。</summary>
        public Camera baseCamera;

        /// <summary>全局单例。</summary>
        public static CoverageChecker Inst { get; private set; }

        private void Awake()
        {
            if (Inst != null)
                Destroy(Inst);
            Inst = this;

            // Mirror.OnCreateCamera / ProjectorShadow.OnCreateCamera 等其余动态相机来源按需后置；
            // 当前仅监听 UI 内嵌 3D 模型相机（UIModelCam）。
            UIModelCam.OnCreateCamera += OnCreateCamera;
        }

        /// <summary>
        /// 监听动态创建相机：界面内相机补 <see cref="CameraCoverageChild"/>，场景相机补 <see cref="CameraCoverage"/>。
        /// </summary>
        private void OnCreateCamera(Camera cam)
        {
            var form = cam.GetComponentInParent<BaseForm>();
            if (form != null)
            {
                // 相机上层有 Form，则添加 CoverageChild 脚本
                if (cam.GetComponent<CoverageChild>() == null)
                    cam.gameObject.AddComponent<CameraCoverageChild>();
            }
            else
            {
                // 否则添加 SceneCoverage 脚本
                var coverage = cam.GetComponent<CameraCoverage>();
                if (coverage == null)
                {
                    coverage = cam.gameObject.AddComponent<CameraCoverage>();
                    coverage.shieldType = CameraCoverage.ShieldType.Enable;
                }
            }
        }

        private void OnDestroy()
        {
            // Mirror.OnCreateCamera -= OnCreateCamera;
            // ProjectorShadow.OnCreateCamera -= OnCreateCamera;
            UIModelCam.OnCreateCamera -= OnCreateCamera;
        }
    }
}
