using Framework.Event;
using Framework.View;

namespace Core.ViewSystem
{
    /// <summary>
    /// Form 生命周期阶段，与 FormManager 暴露的 14 个 C# 事件一一对应。
    /// </summary>
    public enum FormLifecyclePhase
    {
        PreAwake,
        PostAwake,
        PreDestroy,
        PostDestroy,
        PreOpen,
        PostOpen,
        PreShow,
        PostShow,
        PreHide,
        PostHide,
        PreClose,
        PostClose,
        LayerChanged,
        RenderingChanged
    }

    /// <summary>
    /// Form 生命周期全局事件。
    /// 业务层通过 MessagePipe 的 ISubscriber&lt;FormLifecycleEvent&gt; 订阅，
    /// 按 <see cref="Phase"/> 区分阶段（对应参考项目 FormSubSystem 的 EventKey 转发）。
    /// </summary>
    [GameEvent]
    public readonly struct FormLifecycleEvent
    {
        public readonly FormLifecyclePhase Phase;
        public readonly BaseForm Form;

        /// <summary>仅 <see cref="FormLifecyclePhase.LayerChanged"/> 时有效。</summary>
        public readonly int OldLayer;

        /// <summary>仅 <see cref="FormLifecyclePhase.LayerChanged"/> 时有效。</summary>
        public readonly int NewLayer;

        public FormLifecycleEvent(FormLifecyclePhase phase, BaseForm form)
        {
            Phase = phase;
            Form = form;
            OldLayer = 0;
            NewLayer = 0;
        }

        public FormLifecycleEvent(BaseForm form, int oldLayer, int newLayer)
        {
            Phase = FormLifecyclePhase.LayerChanged;
            Form = form;
            OldLayer = oldLayer;
            NewLayer = newLayer;
        }
    }
}
