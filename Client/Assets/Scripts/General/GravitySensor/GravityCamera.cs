using System;
using System.Collections.Generic;
using UnityEngine;

namespace General
{
    /// <summary>重力感应旋转镜头（Input.acceleration）。对应参考项目 General/GravitySensor/GravityCamera.cs。</summary>
    public class GravityCamera : MonoBehaviour
    {
        [Header("旋转中心点")] public Vector3 centerOffset;
        [Header("灵敏度（镜头转动速度）")] public float sensitivity = 1;
        [Header("X轴方向最大范围")] public float horizontalRange = 1.5f;
        [Header("Y轴方向最大范围")] public float verticalRange = 0.6f;
        [Header("柔和度")] public int filterWindowSize = 5;
        [Header("启用脚本时是否重置")] public bool enableReset = false;
        [Header("禁用脚本时是否恢复")] public bool disableRecover = true;

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Queue<Vector3> _filter;

        private void Start()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _filter = new Queue<Vector3>();
        }

        private void Update()
        {
            if (Math.Abs(Input.acceleration.x) <= 0.001 && Math.Abs(Input.acceleration.y) <= 0.001)
                return;

            transform.rotation = _initialRotation;

            _filter.Enqueue(Input.acceleration);
            if (_filter.Count > filterWindowSize)
                _filter.Dequeue();

            float totalX = 0, totalY = 0;
            foreach (Vector3 acc in _filter)
            {
                totalX += acc.x;
                totalY += acc.y;
            }

            float filteredX = totalX / _filter.Count;
            float filteredY = totalY / _filter.Count;

            float xc = -filteredX * horizontalRange;
            float yc = (0.5f + filteredY) * 2 * verticalRange;

            xc = Clamp(xc * sensitivity, -horizontalRange, horizontalRange);
            yc = Clamp(yc * sensitivity, -verticalRange, verticalRange);

            transform.RotateAround(transform.position + centerOffset, Vector3.up, xc);
            transform.RotateAround(transform.position + centerOffset, Vector3.right, yc);
        }

        private static T Clamp<T>(T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            if (val.CompareTo(max) > 0) return max;
            return val;
        }

        private void OnEnable()
        {
            if (enableReset)
            {
                _initialPosition = transform.position;
                _initialRotation = transform.rotation;
            }
        }

        private void OnDisable()
        {
            if (disableRecover)
            {
                transform.position = _initialPosition;
                transform.rotation = _initialRotation;
            }
        }
    }
}
