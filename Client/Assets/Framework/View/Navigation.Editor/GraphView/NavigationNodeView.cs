using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
namespace Framework.View.Navigation.Editor
{
    public abstract class NavigationNodeView : Node
    {
        //绘制视图
        public GraphView GraphView { get; protected set; }

        //输入端口
        public Port InputPort { get; protected set; }

        //根节点
        public NavigationNodeView Root { get; protected set; }

        //父节点视图
        public NavigationNodeView Parent { get; protected set; }

        //子节点视图
        public List<NavigationNodeView> Child { get; protected set; } = new List<NavigationNodeView>();

        //位置缓存
        public Rect PositionCache { get; protected set; }

        //当前层级
        public int CurLayer { get; protected set; }

        public Vector2[] layerSpace = new[]
        {
            new Vector2(50, 50),
            new Vector2(50, 50),
            new Vector2(50, 50),
            new Vector2(50, 50)
        };

        /// <summary>
        /// 获取层级间距
        /// </summary>
        /// <param name="getLayer"></param>
        /// <returns></returns>
        public Vector2 GetLayerSpace(int getLayer)
        {
            if (getLayer < 0 || getLayer >= layerSpace.Length)
                return layerSpace[0];
            return layerSpace[getLayer];
        }

        /// <summary>
        /// 获取自身和子节点Rect
        /// </summary>
        /// <returns></returns>
        public Rect GetSelfAndChildRect()
        {
            List<Rect> rects = new List<Rect> { PositionCache };
            foreach (var child in Child)
            {
                rects.Add(child.GetSelfAndChildRect());
            }

            return GetMaxRect(rects);
        }

        /// <summary>
        /// 获取所有子节点Rect
        /// </summary>
        /// <returns></returns>
        public Rect GetChildRect()
        {
            List<Rect> rects = new List<Rect>();
            foreach (var child in Child)
            {
                rects.Add(child.GetSelfAndChildRect());
            }

            return GetMaxRect(rects);
        }

        /// <summary>
        /// 获取所有Rect的最大值
        /// </summary>
        /// <param name="rects"></param>
        /// <returns></returns>
        public Rect GetMaxRect(List<Rect> rects)
        {
            //计算所有Rect的最大值
            float leftX = rects[0].x;
            float leftY = rects[0].y;
            float rightX = rects[0].x + rects[0].width;
            float rightY = rects[0].y + rects[0].height;
            for (int i = 1; i < rects.Count; i++)
            {
                Rect rect = rects[i];
                if (rect.x < leftX) leftX = rect.x;
                if (rect.y < leftY) leftY = rect.y;
                if (rect.x + rect.width > rightX) rightX = rect.x + rect.width;
                if (rect.y + rect.height > rightY) rightY = rect.y + rect.height;
            }

            return new Rect(leftX, leftY, rightX - leftX, rightY - leftY);
        }

        /// <summary>
        /// 根据全局的Rect设置自身位置
        /// </summary>
        public void RefreshPosition()
        {
            Rect selfRect = GetPosition();
            if (Child.Count == 0)
            {
                //无子节点按全局排
                NavigationNodeView lastShowNode =
                    NavigationViewKit.GetLastGreaterLayerNode(Root, this);
                if (lastShowNode != null)
                {
                    Vector2 layerSpace = GetLayerSpace(CurLayer);
                    if (lastShowNode.Parent == Parent)
                        layerSpace.x = 0;
                    Rect lastRect = lastShowNode.PositionCache;
                    selfRect.position = lastRect.position + new Vector2(lastRect.width + layerSpace.x, 0);
                }
            }
            else
            {
                //有子节点按子节点排
                Rect childRect = GetChildRect();
                Vector2 layerSpace = GetLayerSpace(CurLayer);
                selfRect.position = new Vector2(childRect.position.x + childRect.width * 0.5f - selfRect.width * 0.5f,
                    childRect.position.y - layerSpace.y - selfRect.height);
            }

            SetPosition(selfRect);
        }

        public override void SetPosition(Rect newPos)
        {
            PositionCache = newPos;
            base.SetPosition(newPos);
        }

        /// <summary>
        /// 闪烁状态
        /// </summary>
        public bool Blinking { get; set; }

        protected NavigationNodeView()
        {
            // 在构造函数中注册Update回调
            EditorApplication.update += Update;
        }

        private void Update()
        {
            // 检查是否需要开始或停止闪烁
            if (Blinking)
            {
                // 开始闪烁
                StartBlink();
            }
            else
            {
                // 停止闪烁
                StopBlink();
            }
        }

        private void StartBlink()
        {
            // 设置边框颜色为红色
            style.borderBottomColor = Color.red;
            style.borderLeftColor = Color.red;
            style.borderRightColor = Color.red;
            style.borderTopColor = Color.red;

            // 刷新节点
            RefreshExpandedState();
            MarkDirtyRepaint();

            // 等待一段时间后停止闪烁
            EditorApplication.delayCall += StopBlink;
        }

        private void StopBlink()
        {
            // 恢复边框颜色
            style.borderBottomColor = Color.white;
            style.borderLeftColor = Color.white;
            style.borderRightColor = Color.white;
            style.borderTopColor = Color.white;

            // 刷新节点
            RefreshExpandedState();
            MarkDirtyRepaint();
        }

        public void ToggleBlinking()
        {
            // 切换闪烁状态
            Blinking = !Blinking;
        }
    }
}
