// **************************************************************************************
//   作者：WangXing-汪兴
//   创建时间：2026-04-22
// **************************************************************************************

namespace Framework.View
{
    public sealed class VisibleController
    {
        /// <summary>
        /// 控制器名字
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 控制器改变可见性的策略
        /// </summary>
        public IVisibleStrategy Strategy { get; private set; }

        public VisibleController(string name, IVisibleStrategy strategy)
        {
            Name = name;
            Strategy = strategy;
        }

        public void Reset()
        {
            Name = null;
            Strategy = null;
        }

        public override string ToString()
        {
            return $"{Name} ({Strategy})";
        }
    }

    public struct VisibleControllerState
    {
        /// <summary>
        /// 默认状态：期望显示，且当前也显示
        /// </summary>
        public static VisibleControllerState Default { get; } = new(true, true);

        public VisibleControllerState(bool expectedState, bool currentState)
        {
            ExpectedState = expectedState;
            CurrentState = currentState;
        }

        /// <summary>
        /// 期望的可见性状态 (true=显示，false=隐藏）
        ///
        /// 为什么要有两个状态 ExpectedState与CurrentState：
        ///     当我们由多个控制器时，只要任何一个控制器要求'隐藏'，那么View就要隐藏
        ///     假设我们现在有三个控制器，三个控制器都设置为了'隐藏'，此时View隐藏不可见
        ///     此时其中一个控制器A说我希望改变为'显示'，而另外两个控制器还是'隐藏'，所以View仍然需要保持隐藏不可见
        ///     因此控制器A需要仍然保持'隐藏'状态，不能把View显示出来，所以控制器A同时拥有了两个状态：
        ///         1. ExpectedState，期望状态：'显示'
        ///         2. CurrentState，当前实际状态：'隐藏'
        ///
        /// set为internal，应用层不允许直接修改，必须通过View.SetVisibleState来修改
        /// </summary>
        public bool ExpectedState { get; internal set; }

        /// <summary>
        /// 当前已经设置的可见性状态 (true=显示，false=隐藏）
        ///
        /// set为internal，应用层不允许直接修改，只能由程序集内部逻辑修改
        /// </summary>
        internal bool CurrentState { get; set; }
    }
}
