using System;
using System.Collections.Generic;
namespace Framework.View.Navigation
{
    /// <summary>
    /// 组合转场：Begin顺序执行，End逆序执行
    /// </summary>
    public sealed class TransitionComposite : TransitionBase
    {
        private readonly ITransition[] _transitions;

        public TransitionComposite(params ITransition[] transitions)
        {
            if (transitions == null || transitions.Length == 0)
            {
                _transitions = Array.Empty<ITransition>();
                return;
            }

            var normalized = new List<ITransition>(transitions.Length);
            foreach (var transition in transitions)
            {
                if (transition == null || transition.IsNoOp)
                    continue;

                if (transition is TransitionComposite composite)
                {
                    normalized.AddRange(composite._transitions);
                }
                else
                {
                    normalized.Add(transition);
                }
            }

            _transitions = normalized.Count == 0 ? Array.Empty<ITransition>() : normalized.ToArray();
        }

        public override bool IsNoOp => _transitions.Length == 0;

        public override bool IsEffectRunning
        {
            get
            {
                foreach (var transition in _transitions)
                {
                    if (transition is { IsEffectRunning: true }) return true;
                }

                return false;
            }
        }

        public override void Start()
        {
            base.Start();

            foreach (var transition in _transitions)
            {
                transition.Start();
            }
        }

        public override void Stop()
        {
            for (var i = _transitions.Length - 1; i >= 0; i--)
            {
                _transitions[i].Stop();
            }

            base.Stop();
        }
    }
}
