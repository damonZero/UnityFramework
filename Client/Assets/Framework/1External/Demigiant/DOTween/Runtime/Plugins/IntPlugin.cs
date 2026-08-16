using System;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;

namespace DG.Tweening.Plugins
{
	// Token: 0x02000024 RID: 36
	public class IntPlugin : ABSTweenPlugin<int, int, NoOptions>
	{
		// Token: 0x060001CB RID: 459 RVA: 0x0000890C File Offset: 0x00006B0C
		public override void Reset(TweenerCore<int, int, NoOptions> t)
		{
		}

		// Token: 0x060001CC RID: 460 RVA: 0x0000A174 File Offset: 0x00008374
		public override void SetFrom(TweenerCore<int, int, NoOptions> t, bool isRelative)
		{
			int endValue = t.endValue;
			t.endValue = t.getter();
			t.startValue = (isRelative ? (t.endValue + endValue) : endValue);
			t.setter(t.startValue);
		}

		// Token: 0x060001CD RID: 461 RVA: 0x0000A1C0 File Offset: 0x000083C0
		public override void SetFrom(TweenerCore<int, int, NoOptions> t, int fromValue, bool setImmediately, bool isRelative)
		{
			if (isRelative)
			{
				int num = t.getter();
				t.endValue += num;
				fromValue += num;
			}
			t.startValue = fromValue;
			if (setImmediately)
			{
				t.setter(fromValue);
			}
		}

		// Token: 0x060001CE RID: 462 RVA: 0x00008A83 File Offset: 0x00006C83
		public override int ConvertToStartValue(TweenerCore<int, int, NoOptions> t, int value)
		{
			return value;
		}

		// Token: 0x060001CF RID: 463 RVA: 0x0000A206 File Offset: 0x00008406
		public override void SetRelativeEndValue(TweenerCore<int, int, NoOptions> t)
		{
			t.endValue += t.startValue;
		}

		// Token: 0x060001D0 RID: 464 RVA: 0x0000A21B File Offset: 0x0000841B
		public override void SetChangeValue(TweenerCore<int, int, NoOptions> t)
		{
			t.changeValue = t.endValue - t.startValue;
		}

		// Token: 0x060001D1 RID: 465 RVA: 0x0000A230 File Offset: 0x00008430
		public override float GetSpeedBasedDuration(NoOptions options, float unitsXSecond, int changeValue)
		{
			float num = (float)changeValue / unitsXSecond;
			if (num < 0f)
			{
				num = -num;
			}
			return num;
		}

		// Token: 0x060001D2 RID: 466 RVA: 0x0000A250 File Offset: 0x00008450
		public override void EvaluateAndApply(NoOptions options, Tween t, bool isRelative, DOGetter<int> getter, DOSetter<int> setter, float elapsed, int startValue, int changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice)
		{
			if (t.loopType == LoopType.Incremental)
			{
				startValue += changeValue * (t.isComplete ? (t.completedLoops - 1) : t.completedLoops);
			}
			if (t.isSequenced && t.sequenceParent.loopType == LoopType.Incremental)
			{
				startValue += changeValue * ((t.loopType == LoopType.Incremental) ? t.loops : 1) * (t.sequenceParent.isComplete ? (t.sequenceParent.completedLoops - 1) : t.sequenceParent.completedLoops);
			}
			setter((int)Math.Round((double)((float)startValue + (float)changeValue * EaseManager.Evaluate(t.easeType, t.customEase, elapsed, duration, t.easeOvershootOrAmplitude, t.easePeriod))));
		}
	}
}
