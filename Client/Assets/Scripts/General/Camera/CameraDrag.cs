using UnityEngine;

namespace General
{
    /// <summary>相机拖动（鼠标），水平/垂直限位。对应参考项目 General/Camera/CameraDrag.cs。</summary>
    public class CameraDrag : MonoBehaviour
    {
        [Header("拖动速度")] public float dragSpeed = 1.0f;
        [Header("水平方向拖动限制")] public float horizontalLimit = 10f;
        [Header("竖直方向拖动限制")] public float verticalLimit = 10f;

        private Vector3 _forward;
        private Vector3 _right;
        private Vector3 _tempV3;
        private Vector3 _oldPos;
        private float _dragCoefficient = 0.01f;
        private float _blendCoefficient;

        private void Start()
        {
            Vector3 rotate = transform.rotation.eulerAngles;
            _oldPos = transform.position;
            _right = transform.right;
            _forward = transform.forward + Mathf.Tan(rotate.x / 180 * Mathf.PI) * transform.up;
            _forward = _forward.normalized;
            _blendCoefficient = _dragCoefficient * dragSpeed;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
                _tempV3 = Input.mousePosition;

            if (Input.GetMouseButton(0))
            {
                transform.position += MovePos(Input.mousePosition);
                Limit(transform);
                _tempV3 = Input.mousePosition;
            }
        }

        private Vector3 MovePos(Vector3 mousePos)
        {
            Vector3 delta = -(mousePos - _tempV3);
            return (delta.x * _right + delta.y * _forward) * _blendCoefficient;
        }

        private void Limit(Transform camera)
        {
            Vector3 delta = camera.position - _oldPos;

            float rightDot = Vector3.Dot(delta, _right);
            Vector3 rightTemp;
            if (Mathf.Abs(rightDot) >= horizontalLimit)
                rightTemp = rightDot > 0 ? horizontalLimit * _right : -horizontalLimit * _right;
            else
                rightTemp = rightDot * _right;

            float forwardDot = Vector3.Dot(delta, _forward);
            Vector3 forwardTemp;
            if (Mathf.Abs(forwardDot) >= verticalLimit)
                forwardTemp = forwardDot > 0 ? verticalLimit * _forward : -verticalLimit * _forward;
            else
                forwardTemp = forwardDot * _forward;

            camera.position = forwardTemp + rightTemp + _oldPos;
        }
    }
}
