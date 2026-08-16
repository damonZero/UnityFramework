using UnityEngine;

namespace General
{
    public partial class CameraMoveAdv
    {
        [Header("环绕")]
        [SerializeField] private bool enableAround;

        partial void DoCamAround(float deltaX, float deltaY)
        {
            if (!enableAround) return;

            deltaX *= boostX;
            deltaY *= boostY;
            var trueDeltaX = FixDelta(offsetX, deltaX, minOffsetX, maxOffsetX);
            var trueDeltaY = FixDelta(offsetY, deltaY, minOffsetY, maxOffsetY);
            var position = lookCenter.transform.position;
            cam.transform.RotateAround(position, Vector3.up, trueDeltaX);
            cam.transform.RotateAround(position, cam.transform.right, trueDeltaY);
            offsetX += trueDeltaX;
            offsetY += trueDeltaY;
            TriggerMoveCb();
        }
    }
}
