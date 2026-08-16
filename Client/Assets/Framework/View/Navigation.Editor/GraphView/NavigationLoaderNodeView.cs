using Cysharp.Threading.Tasks;
using Framework.View;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;
namespace Framework.View.Navigation.Editor
{
    public class NavigationLoaderNodeView : NavigationNodeView
    {
        public NavigationLoader Loader { get; private set; }

        public NavigationLoaderNodeView(NavigationLoader loader, GraphView graphView,
            NavigationNodeView root, int curLayer, NavigationNodeView parent)
        {
            Loader = loader;
            GraphView = graphView;
            Root = root;
            CurLayer = curLayer;
            Parent = parent;
            title = $"{NavigationViewKit.GetBehaviourDes(loader)}\n{loader.Name}";
            //添加到面板
            graphView.AddElement(this);

            //添加输入端口
            var inputPort = Port.Create<Edge>(Orientation.Vertical,
                Direction.Input, Port.Capacity.Single, typeof(NavigationContainerNodeView));
            inputPort.portName = "Group";
            inputPort.style.alignSelf = Align.Stretch;
            inputPort.style.backgroundColor = Color.gray;
            titleContainer.Add(inputPort);
            InputPort = inputPort;

            ShowContent();
        }

        private void ShowContent()
        {
            var cacheShow = new Label($"入口：{Loader.Entrance}");
            outputContainer.Add(cacheShow);
            var lockShow = new Label($"锁：{NavigationViewKit.GetLockDescribe(Loader.LockType)}");
            outputContainer.Add(lockShow);
            var transitionShow = new Label($"全屏：{Loader.IsFullScreen()}");
            outputContainer.Add(transitionShow);
            var logicalVisible = new Label($"逻辑可见：{Loader.LogicalVisible}");
            outputContainer.Add(logicalVisible);
            var rendering = new Label($"渲染：{Loader.Rendering}");
            outputContainer.Add(rendering);

            if (Loader is NavigationFormLoader formLoader)
            {
                var layerShow = new Label($"层级：{formLoader.Layer}");
                outputContainer.Add(layerShow);
            }
            else
            {
                var sceneLoader = Loader as NavigationSceneLoader;
                var loadedShow = new Label($"已加载：{sceneLoader?.Scene?.UnityScene.isLoaded}");
                outputContainer.Add(loadedShow);
                var active = new Label($"是否激活：{sceneLoader?.Scene?.IsActiveScene}");
                outputContainer.Add(active);
            }
        }

        //自定义菜单
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // 添加自定义菜单项
            evt.menu.AppendAction("关闭", _ => Loader.Close().Forget());
            evt.menu.AppendAction("显示", _ => Loader.SetLogicalVisible(true).Forget());
            evt.menu.AppendAction("隐藏", _ => Loader.SetLogicalVisible(false).Forget());
            evt.menu.AppendAction("清理", _ => Loader.Clear().Forget());
        }
    }
}
