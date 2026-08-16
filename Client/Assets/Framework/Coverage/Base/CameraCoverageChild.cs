//**************************************************************************************
//Create By szx on 2020/12/1
//
//@Description Coverage 摄像机子节点
//**************************************************************************************

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Framework.Coverage
{
    [RequireComponent(typeof(Camera))]
    public class CameraCoverageChild : CoverageChild
    {
        private Camera _camera;
        private PhysicsRaycaster _raycaster;

        //外部注入的需要屏蔽的组件
        private readonly List<MonoBehaviour> _coverageList = new List<MonoBehaviour>();

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _raycaster = GetComponent<PhysicsRaycaster>();
        }

        public override void OnShow()
        {
            _camera.enabled = true;
            if (_raycaster != null)
                _raycaster.enabled = true;
            foreach (var behaviour in _coverageList)
            {
                if(behaviour)behaviour.enabled = true;
            }
        }

        public override void OnHide()
        {
            _camera.enabled = false;
            if (_raycaster != null)
                _raycaster.enabled = false;
            foreach (var behaviour in _coverageList)
            {
                if(behaviour)behaviour.enabled = false;
            }
        }

        //添加需要参与屏蔽的脚本
        public void AddCoverage(MonoBehaviour behaviour)
        {
            _coverageList.Add(behaviour);
        }
    }
}
