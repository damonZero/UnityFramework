using UnityEngine;

namespace General
{
    public partial class CameraMoveAdv
    {
        [Header("平移")]
        [SerializeField] private bool enableMove = true;

        partial void DoCameraMove(float deltaX, float deltaY)
        {
            if (!enableMove) return;

            Vector3 prevPosition = cam.transform.position;
            Vector3 axisX = GetAxis(touchX);
            Vector3 axisY = GetAxis(touchY);
            deltaX *= boostX;
            deltaY *= boostY;

            float trueDeltaX = FixDelta(offsetX, deltaX, minOffsetX, maxOffsetX);
            float trueDeltaY = FixDelta(offsetY, deltaY, minOffsetY, maxOffsetY);
            cam.transform.position += axisX * trueDeltaX + axisY * trueDeltaY;
            offsetX += trueDeltaX;
            offsetY += trueDeltaY;

            velocity = cam.transform.position - prevPosition;
            TriggerMoveCb();
        }
    }
}
