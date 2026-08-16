using System;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;

namespace DG.Tweening.Plugins
{
	// Token: 0x0200002D RID: 45
	public class FloatPlugin : ABSTweenPlugin<float, float, FloatOptions>
	{
		// Token: 0x0600021A RID: 538 RVA: 0x0000890C File Offset: 0x00006B0C
		public override void Reset(TweenerCore<float, float, FloatOptions> t)
		{
		}

		// Token: 0x0600021B RID: 539 RVA: 0x0000C738 File Offset: 0x0000A938
		public override void SetFrom(TweenerCore<float, float, FloatOptions> t, bool isRelative)
		{
			float endValue = t.endValue;
			t.endValue = t.getter();
			t.startValue = (isRelative ? (t.endValue + endValue) : endValue);
			t.setter((!t.plugOptions.snapping) ? t.startValue : ((float)Math.Round((double)t.startValue)));
		}

		// Token: 0x0600021C RID: 540 RVA: 0x0000C7A0 File Offset: 0x0000A9A0
		public override void SetFrom(TweenerCore<float, float, FloatOptions> t, float fromValue, bool setImmediately, bool isRelative)
		{
			if (isRelative)
			{
				float num = t.getter();
				t.endValue += num;
				fromValue += num;
			}
			t.startValue = fromValue;
			if (setImmediately)
			{
				t.setter((!t.plugOptions.snapping) ? fromValue : ((float)Math.Round((double)fromValue)));
			}
		}

		// Token: 0x0600021D RID: 541 RVA: 0x00008A83 File Offset: 0x00006C83
		public override float ConvertToStartValue(TweenerCore<float, float, FloatOptions> t, float value)
		{
			return value;
		}

		// Token: 0x0600021E RID: 542 RVA: 0x0000C7FD File Offset: 0x0000A9FD
		public override void SetRelativeEndValue(TweenerCore<float, float, FloatOptions> t)
		{
			t.endValue += t.startValue;
		}

		// Token: 0x0600021F RID: 543 RVA: 0x0000C812 File Offset: 0x0000AA12
		public override void SetChangeValue(TweenerCore<float, float, FloatOptions> t)
		{
			t.changeValue = t.endValue - t.startValue;
		}

		// Token: 0x06000220 RID: 544 RVA: 0x0000C828 File Offset: 0x0000AA28
		public override float GetSpeedBasedDuration(FloatOptions options, float unitsXSecond, float changeValue)
		{
			float num = changeValue / unitsXSecond;
			if (num < 0f)
			{
				num = -num;
			}
			return num;
		}

		// Token: 0x06000221 RID: 545 RVA: 0x0000C848 File Offset: 0x0000AA48
		public override void EvaluateAndApply(FloatOptions options, Tween t, bool isRelative, DOGetter<float> getter, DOSetter<float> setter, float elapsed, float startValue, float changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice)
		{
			if (t.loopType == LoopType.Incremental)
			{
				startValue += changeValue * (float)(t.isComplete ? (t.completedLoops - 1) : t.completedLoops);
			}
			if (t.isSequenced && t.sequenceParent.loopType == LoopType.Incremental)
			{
				startValue += changeValue * (float)((t.loopType == LoopType.Incremental) ? t.loops : 1) * (float)(t.sequenceParent.isComplete ? (t.sequenceParent.completedLoops - 1) : t.sequenceParent.completedLoops);
			}
			setter((!options.snapping) ? (startValue + changeValue * EaseManager.Evaluate(t.easeType, t.customEase, elapsed, duration, t.easeOvershootOrAmplitude, t.easePeriod)) : ((float)Math.Round((double)(startValue + changeValue * EaseManager.Evaluate(t.easeType, t.customEase, elapsed, duration, t.easeOvershootOrAmplitude, t.easePeriod)))));
		}
	}
}
