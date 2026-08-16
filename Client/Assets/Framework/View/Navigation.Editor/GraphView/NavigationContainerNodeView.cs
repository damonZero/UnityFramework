using Cysharp.Threading.Tasks;
using Framework.View;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;
using Framework.View.Navigation;
namespace Framework.View.Navigation.Editor
{
    public class NavigationContainerNodeView : NavigationNodeView
    {
        //子节点输出端口
        public Port ChildPort { get; protected set; }

        //所有Loader输出端口
        public Port LoaderPort { get; protected set; }

        /// <summary>
        /// 绘制的导航容器对象
        /// </summary>
        public NavigateContainer Container { get; private set; }

        public NavigationContainerNodeView(NavigateContainer container, GraphView graphView,
            NavigationNodeView root, int curLayer, NavigationNodeView parent)
        {
            Container = container;
            GraphView = graphView;
            CurLayer = curLayer;
            Root = root ?? this;
            Parent = parent;
            title = $"导航容器\n{container.Name}";
            //添加到面板
            graphView.AddElement(this);

            //添加输入端口
            Port inputPort = Port.Create<Edge>(Orientation.Vertical,
                Direction.Input, Port.Capacity.Single, typeof(NavigationContainerNodeView));
            inputPort.style.alignSelf = Align.Stretch;
            inputPort.portName = "Parent";
            inputPort.style.backgroundColor = Color.gray;
            titleContainer.Add(inputPort);
            InputPort = inputPort;

            //添加子节点输出端口
            VisualElement outPutContainer = new VisualElement();
            outPutContainer.style.flexDirection = FlexDirection.Row;
            Port childGroupPort = Port.Create<Edge>(Orientation.Vertical,
                Direction.Output, Port.Capacity.Multi, typeof(NavigationContainerNodeView));
            childGroupPort.portName = "子导航容器";
            childGroupPort.style.alignSelf = Align.Center;
            outPutContainer.Add(childGroupPort);
            ChildPort = childGroupPort;

            //添加所有Loader输出端口
            Port loaderPort = Port.Create<Edge>(Orientation.Vertical,
                Direction.Output, Port.Capacity.Multi, typeof(NavigationContainerNodeView));
            loaderPort.portName = "所有加载器";
            loaderPort.style.alignSelf = Align.Center;
            outPutContainer.Add(loaderPort);
            LoaderPort = loaderPort;
            mainContainer.Add(outPutContainer);

            //显示所有内容
            ShowContent();
        }

        /// <summary>
        /// 显示导航容器内容
        /// </summary>
        public void ShowContent()
        {
            //当前状态
            Label stateShow = new Label($"当前状态：{NavigationViewKit.GetStateDescribe(Container.CurrentState)}");
            outputContainer.Add(stateShow);
            //当前锁状态
            Label lockShow = new Label($"锁类型：{NavigationViewKit.GetLockDescribe(Container.LockType)}");
            outputContainer.Add(lockShow);
            //当前缓存状态
            Label cacheShow = new Label($"缓存类型：{NavigationViewKit.GetCacheDescribe(Container.Cache.CurState)}");
            outputContainer.Add(cacheShow);
            //是否转场中
            Label transitionShow = new Label($"转场中：{Container.Transitioning}");
            outputContainer.Add(transitionShow);
            //显示所有loader
            ShowLoaders();
            //显示所有子导航容器
            ShowChildContainer();
        }

        //显示所有Loaders
        private void ShowLoaders()
        {
            //显示所有Loader
            foreach (var loader in Container.ForeachLoaders(TraversalOrder.Reverse))
            {
                string loaderType = loader is NavigationFormLoader ? "界面" : "场景";
                //显示名字
                Label loaderShow = new Label($"{loaderType}：{loader.Name}")
                {
                    style =
                    {
                        //未进入缓存的Loader为黄色
                        color = Color.yellow
                    }
                };
                extensionContainer.Insert(0, loaderShow);
                //绘制Loaders
                int loaderLayer = CurLayer + 1;
                NavigationLoaderNodeView childNode =
                    new NavigationLoaderNodeView(loader, GraphView, Root, loaderLayer, this);
                Child.Insert(0, childNode);
                //与父节点连线
                NavigationViewKit.LineNode(LoaderPort, childNode.InputPort, GraphView);
            }
        }

        //显示所有子节点
        private void ShowChildContainer()
        {
            //绘制子节点
            foreach (var childGroup in Container.ForeachContainers(TraversalOrder.Forward, includeSelf: false))
            {
                var loaderLayer = CurLayer + 1;
                var childNode =
                    new NavigationContainerNodeView(childGroup, GraphView, Root, loaderLayer, this);
                Child.Add(childNode);
                //与父节点连线
                NavigationViewKit.LineNode(ChildPort, childNode.InputPort, GraphView);
            }
        }

        //自定义菜单
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // 添加自定义菜单项
            evt.menu.AppendAction("关闭", _ => Container.Close().Forget());
            evt.menu.AppendAction("显示", _ => Container.SetLogicalVisible(true).Forget());
            evt.menu.AppendAction("隐藏", _ => Container.SetLogicalVisible(false).Forget());
            evt.menu.AppendAction("清理", _ => Container.Clear().Forget());
        }
    }
}
