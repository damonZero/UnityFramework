using UnityEngine;

namespace General
{
    public partial class CameraMoveAdv
    {
        [Header("环视")]
        [SerializeField] private bool enableLookAround;
        [SerializeField] private bool rotateLimit = true;

        partial void DoCamLookAround(float deltaX, float deltaY)
        {
            if (!enableLookAround) return;

            deltaX *= boostX;
            deltaY *= boostY;
            var trueDeltaX = !rotateLimit ? deltaX : FixDelta(offsetX, deltaX, minOffsetX, maxOffsetX);
            var trueDeltaY = !rotateLimit ? deltaY : FixDelta(offsetY, deltaY, minOffsetY, maxOffsetY);
            var position = lookCenter.transform.position;
            cam.transform.RotateAround(position, Vector3.up, trueDeltaX);
            cam.transform.RotateAround(position, cam.transform.right, trueDeltaY);
            cam.transform.LookAt(position);
            offsetX += trueDeltaX;
            offsetY += trueDeltaY;
            TriggerMoveCb();
        }
    }
}
