using UnityEngine;

namespace General
{
    public partial class CameraMoveAdv
    {
        [Header("旋转")]
        [SerializeField] private bool enableRotate;

        partial void DoCamRotate(float deltaX, float deltaY)
        {
            if (!enableRotate) return;

            deltaX *= boostX;
            deltaY *= -boostY;
            var trueDeltaX = FixDelta(offsetX, deltaX, minOffsetX, maxOffsetX);
            var trueDeltaY = FixDelta(offsetY, deltaY, minOffsetY, maxOffsetY);
            cam.transform.Rotate(Vector3.up, trueDeltaX, Space.World);
            cam.transform.Rotate(Vector3.right, trueDeltaY, Space.Self);
            offsetX += trueDeltaX;
            offsetY += trueDeltaY;
            TriggerMoveCb();
        }
    }
}
