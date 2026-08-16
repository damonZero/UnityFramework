//**************************************************************************************
// Create By Copilot on 2026/03/05
// 转场效果简单工厂
//**************************************************************************************


using System;
using Framework.Pool;
namespace Framework.View.Navigation
{
    /// <summary>
    /// 导航转场预置与组合入口
    /// </summary>
    public static class TransitionFactory
    {
        /// <summary>
        /// 创建一个默认转场效果
        /// </summary>
        
        public static Func<ITransition> CreateDefault { get; set; }

        /// <summary>
        /// 没有转场
        /// </summary>
        public static readonly ITransition None = new TransitionNoOp();

        /// <summary>
        /// 创建转场效果实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static ITransition Create<T>() where T : class, ITransition, new()
        {
            return NavigationFactory.Instance.Get<T>();
        }

        public static ITransition Combine(ITransition first, ITransition second)
        {
            if (first == null || first.IsNoOp)
                return second ?? None;
            if (second == null || second.IsNoOp)
                return first;

            return new TransitionComposite(first, second);
        }

        public static ITransition Combine(params ITransition[] transitions)
        {
            if (transitions == null || transitions.Length == 0)
                return None;

            var composite = new TransitionComposite(transitions);
            return composite.IsNoOp ? None : composite;
        }
    }

}
