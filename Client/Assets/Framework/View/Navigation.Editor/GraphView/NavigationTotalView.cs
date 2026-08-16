using System;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
namespace Framework.View.Navigation.Editor
{
    public class NavigationTotalView : GraphView
    {
        private NavigationContainerNodeView _rootNode;

        //选中节点回调
        public Action<NavigationNodeView> onNodeSelected;

        public NavigationTotalView()
        {
            //按照父级的宽高全屏填充
            this.StretchToParentSize();
            //滚轮缩放
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            //graphview窗口内容拖动
            this.AddManipulator(new ContentDragger());
            //添加横条
            // AddDraggableBar();
            //选中Node移动功能
            // this.AddManipulator(new SelectionDragger());
            //多个node框选功能
            // this.AddManipulator(new RectangleSelector());

            //菜单初始化
            // var menuProvider = ScriptableObject.CreateInstance<NavigationWindowMenuProvider>();
            // menuProvider.OnSelectEntryCb = OnSelectEntry;
            // nodeCreationRequest += context =>
            //     SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), menuProvider);

            //初始化布局方式
            graphViewChanged += OnGraphViewChanged;
            //初始化导航所有内容
            RefreshNavigation();
            //注册鼠标点击事件
            RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        // 鼠标点击事件的处理方法
        private void OnMouseDown(MouseDownEvent evt)
        {
            // 点击左键
            if (evt.button != 0) return;
            // 使用PickAll检查点击的位置是否在节点上
            var clickedElement = panel.Pick(evt.mousePosition);
            NavigationNodeView nodeView = FindNode(clickedElement);
            if (nodeView == null) return;
            nodeView.selected = true;
            onNodeSelected?.Invoke(nodeView);
        }

        //查找NavigationNodeView元素
        private NavigationNodeView FindNode(VisualElement element)
        {
            if (element is NavigationNodeView view)
                return view;
            VisualElement elementParent = element.parent;
            while (true)
            {
                if (elementParent == null)
                    break;
                if (elementParent is NavigationNodeView parentView)
                    return parentView;
                elementParent = elementParent.parent;
            }

            return null;
        }

        //监听Loader变化刷新
        private void OnLoaderChange(NavigationBehaviour behaviour, NavigationStateType stateType)
        {
            RefreshNavigation();
        }

        //监听导航组状态变化刷新
        private void OnGroupChange(NavigationBehaviour behaviour, NavigationStateType stateType)
        {
            NavigateContainer lastGroup = _rootNode.Container.GetLastContainer();
            NavigationViewKit.UpwardsTraverse(_rootNode, node =>
            {
                if (node is NavigationContainerNodeView groupView)
                    node.Blinking = groupView.Container == lastGroup;
            });
        }

        /// <summary>
        /// 刷新导航组
        /// </summary>
        public void RefreshNavigation()
        {
            var navigationSystem = Framework.View.Navigation.NavigationManager.Instance;
            if (navigationSystem == null) return;
            if (_rootNode != null)
            {
                DeleteElements(nodes);
                DeleteElements(edges);
                DeleteElements(graphElements);
                navigationSystem.AfterLoaderStateChange.RemoveAll(OnLoaderChange);
                navigationSystem.AfterContainerStateChange.RemoveAll(OnGroupChange);
            }

            // Fixme by fred 临时屏蔽，需要修复这里的事件监听逻辑
            navigationSystem.AfterLoaderStateChange.Add(OnLoaderChange);
            navigationSystem.AfterContainerStateChange.Add(OnGroupChange);
            _rootNode = new NavigationContainerNodeView(navigationSystem.Root, this, null, 1, null);
            if (_rootNode == null) return;

            IEnumerator UpdatePos()
            {
                yield return NavigationViewKit.DelayUpwardsTraverse(_rootNode, node => node.RefreshPosition(), 1);
                _rootNode.selected = true;
                //获取最后节点
                NavigateContainer lastNode = _rootNode.Container.GetLastContainer();
                GraphElement lastNodeView = FindNode(node =>
                    node is NavigationContainerNodeView groupNodeView && groupNodeView.Container == lastNode);
                FocusNode(lastNodeView as Node, new Vector3(0.9f, 0.9f, 0.9f));
                // contentContainer.transform.scale = new Vector3(1, 1, 1);
                // FrameSelection(); //自动调整可看到全部节点的视图
            }

            EditorCoroutineUtility.StartCoroutine(UpdatePos(), this);

            //刷新所有节点
            foreach (var node in nodes)
            {
                node.RefreshExpandedState();
                node.expanded = true;
                node.RefreshPorts();
            }

            //刷新整个面板显示
            MarkDirtyRepaint();
        }

        /// <summary>
        /// 查找节点
        /// </summary>
        /// <param name="findCb"></param>
        /// <returns></returns>
        public GraphElement FindNode(Func<GraphElement, bool> findCb)
        {
            //遍历查找所有节点
            foreach (var element in graphElements)
            {
                if (findCb(element))
                    return element;
            }

            return null;
        }

        /// <summary>
        /// 当选中一个节点时的回调
        /// </summary>
        /// <param name="searchTreeEntry"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            Type type = searchTreeEntry.userData.GetType();
            Node node = Activator.CreateInstance(type) as Node;
            this.AddElement(node);
            return true;
        }

        /// <summary>
        /// 端口连线
        /// </summary>
        /// <param name="startPort"></param>
        /// <param name="nodeAdapter"></param>
        /// <returns></returns>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            foreach (var port in ports.ToList())
            {
                if (startPort.node == port.node ||
                    startPort.direction == port.direction ||
                    startPort.portType != port.portType)
                    continue;
                compatiblePorts.Add(port);
            }

            return compatiblePorts;
        }

        //当布局发生变化时
        public GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            return change;
        }

        //对齐中心点坐标
        public void CenterPos(Vector2 centerPos)
        {
            //当前中心点坐标
            Vector2 centerPosition = new Vector2(contentRect.width * 0.5f, contentRect.height * 0.5f);
            // 计算偏移量
            Vector2 offset = centerPos - centerPosition;
            //调整位置
            contentViewContainer.style.left = offset.x;
            contentViewContainer.style.top = offset.y;
        }

        /// <summary>
        /// 聚焦到某个节点
        /// </summary>
        /// <param name="node"></param>
        /// <param name="setScale"></param>
        public void FocusNode(Node node, Vector3 setScale)
        {
            if (node == null) return;
            Vector3 viewportCenter = this.layout.center;
            Vector3 nodeCenterInView = contentViewContainer.WorldToLocal(node.GetGlobalCenter());
            Vector3 offset = viewportCenter - nodeCenterInView;
            contentContainer.transform.position = new Vector3(offset.x, offset.y, 0f);
            contentContainer.transform.scale = setScale; // 重置缩放
        }

        //添加横条
        private void AddDraggableBar()
        {
            // 创建一个可拖动的横条元素
            var draggableBar = new VisualElement();
            draggableBar.style.backgroundColor = new StyleColor(new Color(0.5f, 0.5f, 0.5f)); // 设置横条的背景颜色
            draggableBar.style.height = 5; // 设置横条的高度
            draggableBar.style.width = this.contentContainer.resolvedStyle.width; // 设置横条的宽度

            // 添加鼠标拖拽事件
            draggableBar.AddManipulator(new NavigationDraggableBarManipulator(this));

            // 添加横条到 GraphView
            this.contentContainer.Add(draggableBar);
        }
    }
}
