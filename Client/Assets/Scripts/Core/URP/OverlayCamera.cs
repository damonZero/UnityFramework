using Framework.Coverage;
using UnityEngine;

namespace Core.URP
{
    /// <summary>
    /// URP Overlay 场景相机：继承 <see cref="CameraStackOverlay"/>，并把自身纳入 Coverage 遮挡屏蔽。
    /// 对应参考项目 Core/URP/OverlayCamera.cs。
    /// </summary>
    public class OverlayCamera : CameraStackOverlay
    {
        private void Awake()
        {
            var coverage = GetComponent<CameraCoverage>();
            if (coverage != null) coverage.AddCoverage(this);

            var cameraCoverage = GetComponent<CameraCoverageChild>();
            if (cameraCoverage != null) cameraCoverage.AddCoverage(this);
        }
    }
}
