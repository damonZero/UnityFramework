using System;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;

namespace DG.Tweening.Plugins
{
	// Token: 0x02000028 RID: 40
	public class UintPlugin : ABSTweenPlugin<uint, uint, UintOptions>
	{
		// Token: 0x060001F0 RID: 496 RVA: 0x0000890C File Offset: 0x00006B0C
		public override void Reset(TweenerCore<uint, uint, UintOptions> t)
		{
		}

		// Token: 0x060001F1 RID: 497 RVA: 0x0000B330 File Offset: 0x00009530
		public override void SetFrom(TweenerCore<uint, uint, UintOptions> t, bool isRelative)
		{
			uint endValue = t.endValue;
			t.endValue = t.getter();
			t.startValue = (isRelative ? (t.endValue + endValue) : endValue);
			t.setter(t.startValue);
		}

		// Token: 0x060001F2 RID: 498 RVA: 0x0000B37C File Offset: 0x0000957C
		public override void SetFrom(TweenerCore<uint, uint, UintOptions> t, uint fromValue, bool setImmediately, bool isRelative)
		{
			if (isRelative)
			{
				uint num = t.getter();
				t.endValue += num;
				fromValue += num;
			}
			t.startValue = fromValue;
			if (setImmediately)
			{
				t.setter(fromValue);
			}
		}

		// Token: 0x060001F3 RID: 499 RVA: 0x00008A83 File Offset: 0x00006C83
		public override uint ConvertToStartValue(TweenerCore<uint, uint, UintOptions> t, uint value)
		{
			return value;
		}

		// Token: 0x060001F4 RID: 500 RVA: 0x0000B3C2 File Offset: 0x000095C2
		public override void SetRelativeEndValue(TweenerCore<uint, uint, UintOptions> t)
		{
			t.endValue += t.startValue;
		}

		// Token: 0x060001F5 RID: 501 RVA: 0x0000B3D8 File Offset: 0x000095D8
		public override void SetChangeValue(TweenerCore<uint, uint, UintOptions> t)
		{
			t.plugOptions.isNegativeChangeValue = (t.endValue < t.startValue);
			t.changeValue = (t.plugOptions.isNegativeChangeValue ? (t.startValue - t.endValue) : (t.endValue - t.startValue));
		}

		// Token: 0x060001F6 RID: 502 RVA: 0x0000B430 File Offset: 0x00009630
		public override float GetSpeedBasedDuration(UintOptions options, float unitsXSecond, uint changeValue)
		{
			float num = changeValue / unitsXSecond;
			if (num < 0f)
			{
				num = -num;
			}
			return num;
		}

		// Token: 0x060001F7 RID: 503 RVA: 0x0000B450 File Offset: 0x00009650
		public override void EvaluateAndApply(UintOptions options, Tween t, bool isRelative, DOGetter<uint> getter, DOSetter<uint> setter, float elapsed, uint startValue, uint changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice)
		{
			uint num;
			if (t.loopType == LoopType.Incremental)
			{
				num = (uint)((ulong)changeValue * (ulong)((long)(t.isComplete ? (t.completedLoops - 1) : t.completedLoops)));
				if (options.isNegativeChangeValue)
				{
					startValue -= num;
				}
				else
				{
					startValue += num;
				}
			}
			if (t.isSequenced && t.sequenceParent.loopType == LoopType.Incremental)
			{
				num = (uint)((ulong)changeValue * (ulong)((long)((t.loopType == LoopType.Incremental) ? t.loops : 1)) * (ulong)((long)(t.sequenceParent.isComplete ? (t.sequenceParent.completedLoops - 1) : t.sequenceParent.completedLoops)));
				if (options.isNegativeChangeValue)
				{
					startValue -= num;
				}
				else
				{
					startValue += num;
				}
			}
			num = (uint)Math.Round((double)(changeValue * EaseManager.Evaluate(t.easeType, t.customEase, elapsed, duration, t.easeOvershootOrAmplitude, t.easePeriod)));
			if (options.isNegativeChangeValue)
			{
				setter(startValue - num);
				return;
			}
			setter(startValue + num);
		}
	}
}
