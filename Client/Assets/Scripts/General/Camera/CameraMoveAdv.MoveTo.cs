using System;
using DG.Tweening;
using UnityEngine;

namespace General
{
    public partial class CameraMoveAdv
    {
        [Header("定位移动")]
        [SerializeField] private bool enableMoveTo = true;

        /// <summary>根据坐标移动相机（使目标点居中）。</summary>
        public void MoveCamByPos(Vector3 targetPos, Action<object> callBack = null, object userData = null, float moveTime = -1)
        {
            if (!enableMoveTo || cam == null) return;

            Vector3 endValue = TargetPosToCamPos(targetPos);
            float dis = (cam.transform.position - endValue).magnitude;
            float rt = moveTime >= 0 ? moveTime : dis / fixedMoveSpeed;
            if (rt <= 0)
            {
                cam.transform.position = endValue;
                callBack?.Invoke(userData);
                return;
            }

            IsMoving = true;
            var t = DOTween.To(() => cam.transform.position,
                x =>
                {
                    if (cam != null)
                    {
                        cam.transform.position = x;
                        TriggerMoveCb();
                    }
                }, endValue, rt);
            t.SetEase(Ease.Linear);
            t.OnComplete(() =>
            {
                IsMoving = false;
                callBack?.Invoke(userData);
                if (cam != null) LastLoc = cam.transform.position;
            }).target = cam.transform;
        }

        /// <summary>根据坐标环视 Y 轴旋转相机（使目标朝向居中）。</summary>
        public void RotateYCamByPos(Vector3 targetPos, Action<object> callBack = null, object userData = null, float rotateTime = -1)
        {
            if (!enableMoveTo || cam == null || lookCenter == null) return;

            float angle = RotateYCamAngle(targetPos);
            rotateTime = rotateTime >= 0 ? rotateTime : angle / fixedMoveSpeed;
            float lastAngle = 0;
            var t = DOTween.To(angleY =>
            {
                float roundAngle = angleY - lastAngle;
                if (cam != null)
                {
                    var trueDeltaX = FixDelta(offsetX, roundAngle, minOffsetX, maxOffsetX);
                    var position = lookCenter.transform.position;
                    cam.transform.RotateAround(position, Vector3.up, trueDeltaX);
                    cam.transform.LookAt(position);
                    offsetX += trueDeltaX;
                    lastAngle = angleY;
                    TriggerMoveCb();
                }
            }, 0, angle, rotateTime);
            t.SetEase(Ease.Linear);
            t.OnComplete(() => callBack?.Invoke(userData)).target = cam.transform;
        }

        /// <summary>根据坐标计算环视 Y 轴旋转角度。</summary>
        public float RotateYCamAngle(Vector3 targetPos)
        {
            Vector3 camPos = cam.transform.position;
            Vector2 camPos2 = new Vector2(camPos.x, camPos.z);
            Vector3 aroundPos = lookCenter.transform.position;
            Vector2 aroundPos2 = new Vector2(aroundPos.x, aroundPos.z);
            return Vector2.SignedAngle(new Vector2(targetPos.x, targetPos.z) - aroundPos2, aroundPos2 - camPos2);
        }

        /// <summary>通过场景物体坐标计算物体处于屏幕中心时摄像机坐标。</summary>
        public Vector3 TargetPosToCamPos(Vector3 targetPos)
        {
            var position = cam.transform.position;
            var pos = LineToPlane(position, cam.transform.forward, Vector3.up, targetPos);
            var dir = targetPos - pos;
            var camPos = position + dir;
            camPos = (camPos - targetPos).normalized * camDistance + targetPos;
            return camPos;
        }

        private static Vector3 LineToPlane(Vector3 linePoint, Vector3 lineDir, Vector3 planeNormal, Vector3 planePoint)
        {
            var d = Vector3.Dot(planePoint - linePoint, planeNormal) / Vector3.Dot(lineDir.normalized, planeNormal);
            return d * lineDir.normalized + linePoint;
        }
    }
}
