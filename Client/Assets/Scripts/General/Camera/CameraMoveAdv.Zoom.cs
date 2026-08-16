using UnityEngine;

namespace General
{
    public partial class CameraMoveAdv
    {
        [Header("缩放")]
        [SerializeField] private bool enableZoom = true;

        partial void DoCameraZoom(float deltaZoom)
        {
            if (!enableZoom) return;

            var axisZoom = cam.transform.forward;
            deltaZoom *= boostZoom;
            var trueDeltaZoom = FixDelta(offsetZoom, deltaZoom, minZoom, maxZoom);
            cam.transform.position += axisZoom * trueDeltaZoom;
            offsetZoom += trueDeltaZoom;
            TriggerMoveCb();
            TriggerZoomCb();
        }
    }
}
