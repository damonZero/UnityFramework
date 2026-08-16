//*****************************************************************************
//Created By Liangc on 2019年5月22日.
//
//@Description 背景图适配组件(去黑边,防畸变)
//*****************************************************************************

using System;
using UnityEngine;

namespace Package.PSD2UGUI
{
    [RequireComponent(typeof(RectTransform))]
    public class BgScaler : MonoBehaviour
    {
        private void Awake()
        {
            AdapterBg();
        }

        private void Update()
        {
            AdapterBg();
        }

        /**
         * 适配背景
         */
        void AdapterBg()
        {
            var image = GetComponent<UnityEngine.UI.Image>();
            var f1 = Screen.width / (float)Screen.height;
            var f2 = 1600 / 1800.0f;
            if (f1 > f2)
            {
                // 遇到最宽的屏幕，以高度为基准
                var f = f1 * 1624;
                image.rectTransform.sizeDelta = new Vector2(f, 1800 * f / 1600);
                return;
            }

            var f3 = 750 / 1800.0f;
            if (f1 < f3)
            {
                // 遇到最窄的屏幕，以宽度为基准
                var f = 750 / f1;
                image.rectTransform.sizeDelta = new Vector2(1600 * f / 1800, f);
                return;
            }
            
            // 适配范围内的屏幕，保持原来大小
            image.rectTransform.sizeDelta = new Vector2(1600, 1800);
        }
    }
}

