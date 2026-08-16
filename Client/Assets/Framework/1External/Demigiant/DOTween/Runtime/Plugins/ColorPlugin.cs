using System;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace DG.Tweening.Plugins
{
	// Token: 0x02000023 RID: 35
	public class ColorPlugin : ABSTweenPlugin<Color, Color, ColorOptions>
	{
		// Token: 0x060001C2 RID: 450 RVA: 0x0000890C File Offset: 0x00006B0C
		public override void Reset(TweenerCore<Color, Color, ColorOptions> t)
		{
		}

		// Token: 0x060001C3 RID: 451 RVA: 0x00009EF8 File Offset: 0x000080F8
		public override void SetFrom(TweenerCore<Color, Color, ColorOptions> t, bool isRelative)
		{
			Color endValue = t.endValue;
			t.endValue = t.getter();
			t.startValue = (isRelative ? (t.endValue + endValue) : endValue);
			Color pNewValue = t.endValue;
			if (!t.plugOptions.alphaOnly)
			{
				pNewValue = t.startValue;
			}
			else
			{
				pNewValue.a = t.startValue.a;
			}
			t.setter(pNewValue);
		}

		// Token: 0x060001C4 RID: 452 RVA: 0x00009F70 File Offset: 0x00008170
		public override void SetFrom(TweenerCore<Color, Color, ColorOptions> t, Color fromValue, bool setImmediately, bool isRelative)
		{
			if (isRelative)
			{
				Color color = t.getter();
				t.endValue += color;
				fromValue += color;
			}
			t.startValue = fromValue;
			if (setImmediately)
			{
				Color pNewValue = fromValue;
				if (t.plugOptions.alphaOnly)
				{
					pNewValue = t.getter();
					pNewValue.a = fromValue.a;
				}
				t.setter(pNewValue);
			}
		}

		// Token: 0x060001C5 RID: 453 RVA: 0x00008A83 File Offset: 0x00006C83
		public override Color ConvertToStartValue(TweenerCore<Color, Color, ColorOptions> t, Color value)
		{
			return value;
		}

		// Token: 0x060001C6 RID: 454 RVA: 0x00009FE6 File Offset: 0x000081E6
		public override void SetRelativeEndValue(TweenerCore<Color, Color, ColorOptions> t)
		{
			t.endValue += t.startValue;
		}

		// Token: 0x060001C7 RID: 455 RVA: 0x00009FFF File Offset: 0x000081FF
		public override void SetChangeValue(TweenerCore<Color, Color, ColorOptions> t)
		{
			t.changeValue = t.endValue - t.startValue;
		}

		// Token: 0x060001C8 RID: 456 RVA: 0x00008AB8 File Offset: 0x00006CB8
		public override float GetSpeedBasedDuration(ColorOptions options, float unitsXSecond, Color changeValue)
		{
			return 1f / unitsXSecond;
		}

		// Token: 0x060001C9 RID: 457 RVA: 0x0000A018 File Offset: 0x00008218
		public override void EvaluateAndApply(ColorOptions options, Tween t, bool isRelative, DOGetter<Color> getter, DOSetter<Color> setter, float elapsed, Color startValue, Color changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice)
		{
			if (t.loopType == LoopType.Incremental)
			{
				startValue += changeValue * (float)(t.isComplete ? (t.completedLoops - 1) : t.completedLoops);
			}
			if (t.isSequenced && t.sequenceParent.loopType == LoopType.Incremental)
			{
				startValue += changeValue * (float)((t.loopType == LoopType.Incremental) ? t.loops : 1) * (float)(t.sequenceParent.isComplete ? (t.sequenceParent.completedLoops - 1) : t.sequenceParent.completedLoops);
			}
			float num = EaseManager.Evaluate(t.easeType, t.customEase, elapsed, duration, t.easeOvershootOrAmplitude, t.easePeriod);
			if (!options.alphaOnly)
			{
				startValue.r += changeValue.r * num;
				startValue.g += changeValue.g * num;
				startValue.b += changeValue.b * num;
				startValue.a += changeValue.a * num;
				setter(startValue);
				return;
			}
			Color pNewValue = getter();
			pNewValue.a = startValue.a + changeValue.a * num;
			setter(pNewValue);
		}
	}
}
