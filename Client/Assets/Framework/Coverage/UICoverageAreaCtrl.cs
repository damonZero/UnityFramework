//**************************************************************************************
//Create By szx on 2020/12/3
//
//@Description Coverage UI区域外部控制器
//**************************************************************************************

using System;
using UnityEngine;

namespace Framework.Coverage
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UICoverageAreaCtrl : MonoBehaviour
    {
        [Header("显示区域是否初始可用")] public bool initShowAvailable = true;
        [Header("遮挡区域是否初始可用")] public bool initCovAvailable = true;

        public event Action<UICoverageArea.CoverageType, bool> OnAvailableChange;

        private bool _showAvailable = true; //作为展示区域是否可用
        private bool _covAvailable = true; //作为遮挡区域是否可用

        private void Awake()
        {
            _showAvailable = initShowAvailable;
            _covAvailable = initCovAvailable;
        }

        /// <summary>
        /// 设置是否可用
        /// </summary>
        /// <param name="available"></param>
        public void SetAvailable(UICoverageArea.CoverageType type, bool available)
        {
            var value = type == UICoverageArea.CoverageType.Show ? _showAvailable : _covAvailable;
            if (value == available)
                return;
            if (type == UICoverageArea.CoverageType.Show)
                _showAvailable = available;
            else
                _covAvailable = available;
            OnAvailableChange?.Invoke(type, available);
        }

        /// <summary>
        /// 设置是否可用，传枚举的int值，方便lua调用
        /// </summary>
        /// <param name="available"></param>
        /// <param name="type"></param>
        public void SetAvailable(int type, bool available)
        {
            SetAvailable((UICoverageArea.CoverageType) type, available);
        }

        /// <summary>
        /// 获取作为展示/遮挡区域时是否可用
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool GetAvailable(UICoverageArea.CoverageType type)
        {
            return type == UICoverageArea.CoverageType.Show ? _showAvailable : _covAvailable;
        }

        /// <summary>
        /// 获取作为展示/遮挡区域时是否可用，传枚举的int值，方便lua调用
        /// </summary>
        /// <param name="type"></param>
        public bool GetAvailable(int type)
        {
            return GetAvailable((UICoverageArea.CoverageType) type);
        }

    }
}
