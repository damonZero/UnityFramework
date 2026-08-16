using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
namespace Framework.View.Navigation.Editor
{
    public class NavigationDraggableBarManipulator:MouseManipulator
    {
        private bool _isDragging;
        private GraphView _graphView;

        public NavigationDraggableBarManipulator(GraphView graphView)
        {
            this._graphView = graphView;
            // 注册鼠标按下、抬起、移动事件回调
            RegisterCallbacksOnTarget();
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            _isDragging = true;
            target.CaptureMouse();
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            _isDragging = false;
            target.ReleaseMouse();
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (_isDragging)
            {
                // 计算横条的新高度
                // target.style.height = Mathf.Max(1, target.resolvedStyle.height + evt.localDelta.y);

                // 阻止事件继续传递，以防止影响到其他控件
                evt.StopPropagation();
            }
        }
    }
}