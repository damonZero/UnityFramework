using System;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;

namespace DG.Tweening.Plugins
{
	// Token: 0x0200001F RID: 31
	public class LongPlugin : ABSTweenPlugin<long, long, NoOptions>
	{
		// Token: 0x0600019C RID: 412 RVA: 0x0000890C File Offset: 0x00006B0C
		public override void Reset(TweenerCore<long, long, NoOptions> t)
		{
		}

		// Token: 0x0600019D RID: 413 RVA: 0x00008E9C File Offset: 0x0000709C
		public override void SetFrom(TweenerCore<long, long, NoOptions> t, bool isRelative)
		{
			long endValue = t.endValue;
			t.endValue = t.getter();
			t.startValue = (isRelative ? (t.endValue + endValue) : endValue);
			t.setter(t.startValue);
		}

		// Token: 0x0600019E RID: 414 RVA: 0x00008EE8 File Offset: 0x000070E8
		public override void SetFrom(TweenerCore<long, long, NoOptions> t, long fromValue, bool setImmediately, bool isRelative)
		{
			if (isRelative)
			{
				long num = t.getter();
				t.endValue += num;
				fromValue += num;
			}
			t.startValue = fromValue;
			if (setImmediately)
			{
				t.setter(fromValue);
			}
		}

		// Token: 0x0600019F RID: 415 RVA: 0x00008A83 File Offset: 0x00006C83
		public override long ConvertToStartValue(TweenerCore<long, long, NoOptions> t, long value)
		{
			return value;
		}

		// Token: 0x060001A0 RID: 416 RVA: 0x00008F2E File Offset: 0x0000712E
		public override void SetRelativeEndValue(TweenerCore<long, long, NoOptions> t)
		{
			t.endValue += t.startValue;
		}

		// Token: 0x060001A1 RID: 417 RVA: 0x00008F43 File Offset: 0x00007143
		public override void SetChangeValue(TweenerCore<long, long, NoOptions> t)
		{
			t.changeValue = t.endValue - t.startValue;
		}

		// Token: 0x060001A2 RID: 418 RVA: 0x00008F58 File Offset: 0x00007158
		public override float GetSpeedBasedDuration(NoOptions options, float unitsXSecond, long changeValue)
		{
			float num = (float)changeValue / unitsXSecond;
			if (num < 0f)
			{
				num = -num;
			}
			return num;
		}

		// Token: 0x060001A3 RID: 419 RVA: 0x00008F78 File Offset: 0x00007178
		public override void EvaluateAndApply(NoOptions options, Tween t, bool isRelative, DOGetter<long> getter, DOSetter<long> setter, float elapsed, long startValue, long changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice)
		{
			if (t.loopType == LoopType.Incremental)
			{
				startValue += changeValue * (long)(t.isComplete ? (t.completedLoops - 1) : t.completedLoops);
			}
			if (t.isSequenced && t.sequenceParent.loopType == LoopType.Incremental)
			{
				startValue += changeValue * (long)((t.loopType == LoopType.Incremental) ? t.loops : 1) * (long)(t.sequenceParent.isComplete ? (t.sequenceParent.completedLoops - 1) : t.sequenceParent.completedLoops);
			}
			setter((long)Math.Round((double)((float)startValue + (float)changeValue * EaseManager.Evaluate(t.easeType, t.customEase, elapsed, duration, t.easeOvershootOrAmplitude, t.easePeriod))));
		}
	}
}
