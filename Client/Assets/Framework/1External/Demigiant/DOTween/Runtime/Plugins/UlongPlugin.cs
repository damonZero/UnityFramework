using System;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;

namespace DG.Tweening.Plugins
{
	// Token: 0x02000020 RID: 32
	public class UlongPlugin : ABSTweenPlugin<ulong, ulong, NoOptions>
	{
		// Token: 0x060001A5 RID: 421 RVA: 0x0000890C File Offset: 0x00006B0C
		public override void Reset(TweenerCore<ulong, ulong, NoOptions> t)
		{
		}

		// Token: 0x060001A6 RID: 422 RVA: 0x00009048 File Offset: 0x00007248
		public override void SetFrom(TweenerCore<ulong, ulong, NoOptions> t, bool isRelative)
		{
			ulong endValue = t.endValue;
			t.endValue = t.getter();
			t.startValue = (isRelative ? (t.endValue + endValue) : endValue);
			t.setter(t.startValue);
		}

		// Token: 0x060001A7 RID: 423 RVA: 0x00009094 File Offset: 0x00007294
		public override void SetFrom(TweenerCore<ulong, ulong, NoOptions> t, ulong fromValue, bool setImmediately, bool isRelative)
		{
			if (isRelative)
			{
				ulong num = t.getter();
				t.endValue += num;
				fromValue += num;
			}
			t.startValue = fromValue;
			if (setImmediately)
			{
				t.setter(fromValue);
			}
		}

		// Token: 0x060001A8 RID: 424 RVA: 0x00008A83 File Offset: 0x00006C83
		public override ulong ConvertToStartValue(TweenerCore<ulong, ulong, NoOptions> t, ulong value)
		{
			return value;
		}

		// Token: 0x060001A9 RID: 425 RVA: 0x000090DA File Offset: 0x000072DA
		public override void SetRelativeEndValue(TweenerCore<ulong, ulong, NoOptions> t)
		{
			t.endValue += t.startValue;
		}

		// Token: 0x060001AA RID: 426 RVA: 0x000090EF File Offset: 0x000072EF
		public override void SetChangeValue(TweenerCore<ulong, ulong, NoOptions> t)
		{
			t.changeValue = t.endValue - t.startValue;
		}

		// Token: 0x060001AB RID: 427 RVA: 0x00009104 File Offset: 0x00007304
		public override float GetSpeedBasedDuration(NoOptions options, float unitsXSecond, ulong changeValue)
		{
			float num = changeValue / unitsXSecond;
			if (num < 0f)
			{
				num = -num;
			}
			return num;
		}

		// Token: 0x060001AC RID: 428 RVA: 0x00009124 File Offset: 0x00007324
		public override void EvaluateAndApply(NoOptions options, Tween t, bool isRelative, DOGetter<ulong> getter, DOSetter<ulong> setter, float elapsed, ulong startValue, ulong changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice)
		{
			if (t.loopType == LoopType.Incremental)
			{
				startValue += changeValue * (ulong)(t.isComplete ? (t.completedLoops - 1) : t.completedLoops);
			}
			if (t.isSequenced && t.sequenceParent.loopType == LoopType.Incremental)
			{
				startValue += changeValue * (ulong)((t.loopType == LoopType.Incremental) ? t.loops : 1) * (ulong)(t.sequenceParent.isComplete ? (t.sequenceParent.completedLoops - 1) : t.sequenceParent.completedLoops);
			}
			setter((ulong)(startValue + changeValue * (decimal)EaseManager.Evaluate(t.easeType, t.customEase, elapsed, duration, t.easeOvershootOrAmplitude, t.easePeriod)));
		}
	}
}
