// ********************************************************************
//   作者：WangXing-汪兴
//   创建时间：2026-04-22
// ********************************************************************

namespace Framework.View
{
    /// <summary>
    /// 场景显示/隐藏处理策略：enable/disable场景中所有根节点(Root GameObjects)
    /// </summary>
    public class SceneVisibleStrategyByRootGameObjects : IVisibleStrategy
    {
        public static SceneVisibleStrategyByRootGameObjects Shared { get; } = new ();

        /// <summary>
        /// 构造函数设置为private，不允许new，直接使用Shared实例即可
        /// </summary>
        private SceneVisibleStrategyByRootGameObjects()
        {
        }

        public void SetVisible(ViewBase view, bool visible)
        {
            if (view is BaseScene scene)
            {
                if (visible)
                {
                    scene.EnableRootGameObjects();
                }
                else
                {
                    scene.DisableRootGameObjects();
                }
            }
            else
            {
                Log.Error($"{view} is not a {nameof(BaseScene)}");
            }
        }

    }
}
