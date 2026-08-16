//**************************************************************************************
//Create By szx on 2019/11/6
//
//@Description 场景显示对象
//**************************************************************************************

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Framework.Coverage
{
    /// <summary>
    /// 场景相机显示对象  该脚本挂场景摄像机上
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraCoverage : BaseCoverage
    {
        public enum ShieldType
        {
            Enable = 1, //通过改变Enable来屏蔽
            ClipPlane = 2 //通过改变裁剪平面距离来屏蔽
        }

        //缓存的主相机(摄像机被禁用后，可以通过这个缓存尝试获取)
        public static Camera mainCamera;

        private IntRect[] _showRects;
        private IntRect[] _coverRects;
        private RectSide[] _verticalSides;
        private Camera _camera;
        private PhysicsRaycaster _raycaster;

        [Header("正常视距")] public float normalFarClipPlane = 1000f;

        [Header("屏蔽相机视距")] public float hideFarClipPlane = 1f;

        [Header("屏蔽类型")] public ShieldType shieldType = ShieldType.Enable;
        //外部注入的需要屏蔽的组件
        private readonly List<MonoBehaviour> _coverageList = new List<MonoBehaviour>();

        public override IEnumerable<IntRect> ShowRectList => _showRects;

        public override IEnumerable<IntRect> CoverRectList => _coverRects;

        public override IList<RectSide> HorizontalSideList => throw new Exception("暂时只用竖直方向的边");


        protected override bool RegisterOnStart => true;


        protected override bool Init()
        {
            //场景相机有且只有一个显示区域，即为UI设计尺寸
            //没有遮挡区域
            _showRects = new[] {Holder.Range};
            _coverRects = Array.Empty<IntRect>();
            _verticalSides = Array.Empty<RectSide>();
            return true;
        }

        public override string DebugInfo()
        {
            return $"场景[{gameObject.scene.name}]->  是否可见:{CoverageVisible}";
        }

        public override IList<RectSide> VerticalSideList => _verticalSides;

        public override int CoverageIdx => -1;

        protected override bool ActualRendering
        {
            get
            {
                if (shieldType == ShieldType.Enable)
                    return _camera.enabled;
                if (shieldType == ShieldType.ClipPlane)
                    return Mathf.Abs(_camera.farClipPlane - normalFarClipPlane) > 0.0001f;
                return true;
            }
        }

        protected override void DoSetVisible(bool visible)
        {
            if (shieldType == ShieldType.Enable)
                _camera.enabled = visible;
            else if (shieldType == ShieldType.ClipPlane)
                _camera.farClipPlane = visible ? normalFarClipPlane : hideFarClipPlane;
            if (_raycaster != null)
                _raycaster.enabled = visible;

            foreach (var behaviour in _coverageList)
            {
                if(behaviour)behaviour.enabled = visible;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _camera = GetComponent<Camera>();
            _raycaster = GetComponent<PhysicsRaycaster>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            //缓存主相机（放到OnEnable是因为场景可能缓存）
            if (_camera != null && "MainCamera".Equals(_camera.tag))
            {
                mainCamera = _camera;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            //去掉主相机缓存
            if (mainCamera == _camera)
            {
                mainCamera = null;
            }
        }

        //添加需要参与屏蔽的脚本
        public void AddCoverage(MonoBehaviour behaviour)
        {
            _coverageList.Add(behaviour);
        }
    }
}
