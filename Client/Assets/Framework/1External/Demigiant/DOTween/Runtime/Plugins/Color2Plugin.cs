using System;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;

namespace DG.Tweening.Plugins
{
	// Token: 0x0200001D RID: 29
	internal class Color2Plugin : ABSTweenPlugin<Color2, Color2, ColorOptions>
	{
		// Token: 0x0600018A RID: 394 RVA: 0x0000890C File Offset: 0x00006B0C
		public override void Reset(TweenerCore<Color2, Color2, ColorOptions> t)
		{
		}

		// Token: 0x0600018B RID: 395 RVA: 0x00008910 File Offset: 0x00006B10
		public override void SetFrom(TweenerCore<Color2, Color2, ColorOptions> t, bool isRelative)
		{
			Color2 endValue = t.endValue;
			t.endValue = t.getter();
			if (isRelative)
			{
				t.startValue = new Color2(t.endValue.ca + endValue.ca, t.endValue.cb + endValue.cb);
			}
			else
			{
				t.startValue = new Color2(endValue.ca, endValue.cb);
			}
			Color2 pNewValue = t.endValue;
			if (!t.plugOptions.alphaOnly)
			{
				pNewValue = t.startValue;
			}
			else
			{
				pNewValue.ca.a = t.startValue.ca.a;
				pNewValue.cb.a = t.startValue.cb.a;
			}
			t.setter(pNewValue);
		}

		// Token: 0x0600018C RID: 396 RVA: 0x000089EC File Offset: 0x00006BEC
		public override void SetFrom(TweenerCore<Color2, Color2, ColorOptions> t, Color2 fromValue, bool setImmediately, bool isRelative)
		{
			if (isRelative)
			{
				Color2 c = t.getter();
				t.endValue += c;
				fromValue += c;
			}
			t.startValue = fromValue;
			if (setImmediately)
			{
				Color2 pNewValue = fromValue;
				if (t.plugOptions.alphaOnly)
				{
					pNewValue = t.getter();
					pNewValue.ca.a = fromValue.ca.a;
					pNewValue.cb.a = fromValue.cb.a;
				}
				t.setter(pNewValue);
			}
		}

		// Token: 0x0600018D RID: 397 RVA: 0x00008A83 File Offset: 0x00006C83
		public override Color2 ConvertToStartValue(TweenerCore<Color2, Color2, ColorOptions> t, Color2 value)
		{
			return value;
		}

		// Token: 0x0600018E RID: 398 RVA: 0x00008A86 File Offset: 0x00006C86
		public override void SetRelativeEndValue(TweenerCore<Color2, Color2, ColorOptions> t)
		{
			t.endValue += t.startValue;
		}

		// Token: 0x0600018F RID: 399 RVA: 0x00008A9F File Offset: 0x00006C9F
		public override void SetChangeValue(TweenerCore<Color2, Color2, ColorOptions> t)
		{
			t.changeValue = t.endValue - t.startValue;
		}

		// Token: 0x06000190 RID: 400 RVA: 0x00008AB8 File Offset: 0x00006CB8
		public override float GetSpeedBasedDuration(ColorOptions options, float unitsXSecond, Color2 changeValue)
		{
			return 1f / unitsXSecond;
		}

		// Token: 0x06000191 RID: 401 RVA: 0x00008AC4 File Offset: 0x00006CC4
		public override void EvaluateAndApply(ColorOptions options, Tween t, bool isRelative, DOGetter<Color2> getter, DOSetter<Color2> setter, float elapsed, Color2 startValue, Color2 changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice)
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
				startValue.ca.r = startValue.ca.r + changeValue.ca.r * num;
				startValue.ca.g = startValue.ca.g + changeValue.ca.g * num;
				startValue.ca.b = startValue.ca.b + changeValue.ca.b * num;
				startValue.ca.a = startValue.ca.a + changeValue.ca.a * num;
				startValue.cb.r = startValue.cb.r + changeValue.cb.r * num;
				startValue.cb.g = startValue.cb.g + changeValue.cb.g * num;
				startValue.cb.b = startValue.cb.b + changeValue.cb.b * num;
				startValue.cb.a = startValue.cb.a + changeValue.cb.a * num;
				setter(startValue);
				return;
			}
			Color2 pNewValue = getter();
			pNewValue.ca.a = startValue.ca.a + changeValue.ca.a * num;
			pNewValue.cb.a = startValue.cb.a + changeValue.cb.a * num;
			setter(pNewValue);
		}
	}
}
