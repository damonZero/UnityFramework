//**************************************************************************************
//Create By fred on 2019/01/03
//
//@Description 触摸事件管理模块
//**************************************************************************************

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Framework.Touch
{
    public class TouchModule
    {
        // 返回键响应代理
        public static Action BackHandler { get; set; }

        // 触摸开始处理
        public static Action<Vector3> TouchBegin { get; set; }
        // 触摸中处理
        public static Action<Vector3> Touching { get; set; }
        // 触摸结束处理
        public static Action<Vector3> TouchEnd { get; set; }

        // 拖拽阈值与设备 dpi 的比率（默认 5px / 约 160dpi ≈ 0.032，具体取值由需求决定）
        protected const float DPI_THRESHOLD_RATE = 0.032f;

        // dpi 获取失败（部分 Android 设备/编辑器返回 0）时的回退值
        protected const float FALLBACK_DPI = 160f;

        public void Init()
        {
            // 根据屏幕 DPI 设置拖动阈值，避免高分辨率下滚动列表中的按钮难以点击的问题
            var dpi = Screen.dpi > 0 ? Screen.dpi : FALLBACK_DPI;
            var threshold = Mathf.Max(1, (int)(dpi * DPI_THRESHOLD_RATE));
            if (EventSystem.current != null)
                EventSystem.current.pixelDragThreshold = threshold;
            // 控制是否处理鼠标右键、中键
            StandaloneAdvInputModule.isHandleRightAndMiddleButton = false;
        }

        public void Shutdown()
        {
            BackHandler = null;
            TouchBegin = null;
            Touching = null;
            TouchEnd = null;

            StandaloneAdvInputModule.ShutDown();
        }

        public void Update(float elapsed)
        {
            // 返回键检测
            if (Input.GetKeyDown(KeyCode.Escape))
                BackHandler?.Invoke();

#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
                TouchBegin?.Invoke(Input.mousePosition);
            if (Input.GetMouseButton(0))
                Touching?.Invoke(Input.mousePosition);
            if (Input.GetMouseButtonUp(0))
                TouchEnd?.Invoke(Input.mousePosition);
#else
            // Touch处理
            for (var i = 0; i < Input.touchCount; ++i)
            {
                var touch = Input.GetTouch(i);
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        TouchBegin?.Invoke(touch.position);
                        break;
                    case TouchPhase.Moved:
                        Touching?.Invoke(touch.position);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        TouchEnd?.Invoke(touch.position);
                        break;
                }
            }
#endif
        }
    }
}
