using System;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;

namespace DG.Tweening.Plugins
{
	// Token: 0x0200001E RID: 30
	public class DoublePlugin : ABSTweenPlugin<double, double, NoOptions>
	{
		// Token: 0x06000193 RID: 403 RVA: 0x0000890C File Offset: 0x00006B0C
		public override void Reset(TweenerCore<double, double, NoOptions> t)
		{
		}

		// Token: 0x06000194 RID: 404 RVA: 0x00008CF8 File Offset: 0x00006EF8
		public override void SetFrom(TweenerCore<double, double, NoOptions> t, bool isRelative)
		{
			double endValue = t.endValue;
			t.endValue = t.getter();
			t.startValue = (isRelative ? (t.endValue + endValue) : endValue);
			t.setter(t.startValue);
		}

		// Token: 0x06000195 RID: 405 RVA: 0x00008D44 File Offset: 0x00006F44
		public override void SetFrom(TweenerCore<double, double, NoOptions> t, double fromValue, bool setImmediately, bool isRelative)
		{
			if (isRelative)
			{
				double num = t.getter();
				t.endValue += num;
				fromValue += num;
			}
			t.startValue = fromValue;
			if (setImmediately)
			{
				t.setter(fromValue);
			}
		}

		// Token: 0x06000196 RID: 406 RVA: 0x00008A83 File Offset: 0x00006C83
		public override double ConvertToStartValue(TweenerCore<double, double, NoOptions> t, double value)
		{
			return value;
		}

		// Token: 0x06000197 RID: 407 RVA: 0x00008D8A File Offset: 0x00006F8A
		public override void SetRelativeEndValue(TweenerCore<double, double, NoOptions> t)
		{
			t.endValue += t.startValue;
		}

		// Token: 0x06000198 RID: 408 RVA: 0x00008D9F File Offset: 0x00006F9F
		public override void SetChangeValue(TweenerCore<double, double, NoOptions> t)
		{
			t.changeValue = t.endValue - t.startValue;
		}

		// Token: 0x06000199 RID: 409 RVA: 0x00008DB4 File Offset: 0x00006FB4
		public override float GetSpeedBasedDuration(NoOptions options, float unitsXSecond, double changeValue)
		{
			float num = (float)changeValue / unitsXSecond;
			if (num < 0f)
			{
				num = -num;
			}
			return num;
		}

		// Token: 0x0600019A RID: 410 RVA: 0x00008DD4 File Offset: 0x00006FD4
		public override void EvaluateAndApply(NoOptions options, Tween t, bool isRelative, DOGetter<double> getter, DOSetter<double> setter, float elapsed, double startValue, double changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice)
		{
			if (t.loopType == LoopType.Incremental)
			{
				startValue += changeValue * (double)(t.isComplete ? (t.completedLoops - 1) : t.completedLoops);
			}
			if (t.isSequenced && t.sequenceParent.loopType == LoopType.Incremental)
			{
				startValue += changeValue * (double)((t.loopType == LoopType.Incremental) ? t.loops : 1) * (double)(t.sequenceParent.isComplete ? (t.sequenceParent.completedLoops - 1) : t.sequenceParent.completedLoops);
			}
			setter(startValue + changeValue * (double)EaseManager.Evaluate(t.easeType, t.customEase, elapsed, duration, t.easeOvershootOrAmplitude, t.easePeriod));
		}
	}
}
