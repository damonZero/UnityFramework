using System;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace DG.Tweening.Plugins
{
	// Token: 0x02000027 RID: 39
	public class RectPlugin : ABSTweenPlugin<Rect, Rect, RectOptions>
	{
		// Token: 0x060001E7 RID: 487 RVA: 0x0000890C File Offset: 0x00006B0C
		public override void Reset(TweenerCore<Rect, Rect, RectOptions> t)
		{
		}

		// Token: 0x060001E8 RID: 488 RVA: 0x0000AD6C File Offset: 0x00008F6C
		public override void SetFrom(TweenerCore<Rect, Rect, RectOptions> t, bool isRelative)
		{
			Rect endValue = t.endValue;
			t.endValue = t.getter();
			t.startValue = endValue;
			if (isRelative)
			{
				t.startValue.x = t.startValue.x + t.endValue.x;
				t.startValue.y = t.startValue.y + t.endValue.y;
				t.startValue.width = t.startValue.width + t.endValue.width;
				t.startValue.height = t.startValue.height + t.endValue.height;
			}
			Rect startValue = t.startValue;
			if (t.plugOptions.snapping)
			{
				startValue.x = (float)Math.Round((double)startValue.x);
				startValue.y = (float)Math.Round((double)startValue.y);
				startValue.width = (float)Math.Round((double)startValue.width);
				startValue.height = (float)Math.Round((double)startValue.height);
			}
			t.setter(startValue);
		}

		// Token: 0x060001E9 RID: 489 RVA: 0x0000AE84 File Offset: 0x00009084
		public override void SetFrom(TweenerCore<Rect, Rect, RectOptions> t, Rect fromValue, bool setImmediately, bool isRelative)
		{
			if (isRelative)
			{
				Rect rect = t.getter();
				t.endValue.x = t.endValue.x + rect.x;
				t.endValue.y = t.endValue.y + rect.y;
				t.endValue.width = t.endValue.width + rect.width;
				t.endValue.height = t.endValue.height + rect.height;
				fromValue.x += rect.x;
				fromValue.y += rect.y;
				fromValue.width += rect.width;
				fromValue.height += rect.height;
			}
			t.startValue = fromValue;
			if (setImmediately)
			{
				Rect pNewValue = fromValue;
				if (t.plugOptions.snapping)
				{
					pNewValue.x = (float)Math.Round((double)pNewValue.x);
					pNewValue.y = (float)Math.Round((double)pNewValue.y);
					pNewValue.width = (float)Math.Round((double)pNewValue.width);
					pNewValue.height = (float)Math.Round((double)pNewValue.height);
				}
				t.setter(pNewValue);
			}
		}

		// Token: 0x060001EA RID: 490 RVA: 0x00008A83 File Offset: 0x00006C83
		public override Rect ConvertToStartValue(TweenerCore<Rect, Rect, RectOptions> t, Rect value)
		{
			return value;
		}

		// Token: 0x060001EB RID: 491 RVA: 0x0000AFD8 File Offset: 0x000091D8
		public override void SetRelativeEndValue(TweenerCore<Rect, Rect, RectOptions> t)
		{
			t.endValue.x = t.endValue.x + t.startValue.x;
			t.endValue.y = t.endValue.y + t.startValue.y;
			t.endValue.width = t.endValue.width + t.startValue.width;
			t.endValue.height = t.endValue.height + t.startValue.height;
		}

		// Token: 0x060001EC RID: 492 RVA: 0x0000B05C File Offset: 0x0000925C
		public override void SetChangeValue(TweenerCore<Rect, Rect, RectOptions> t)
		{
			t.changeValue = new Rect(t.endValue.x - t.startValue.x, t.endValue.y - t.startValue.y, t.endValue.width - t.startValue.width, t.endValue.height - t.startValue.height);
		}

		// Token: 0x060001ED RID: 493 RVA: 0x0000B0D0 File Offset: 0x000092D0
		public override float GetSpeedBasedDuration(RectOptions options, float unitsXSecond, Rect changeValue)
		{
			float width = changeValue.width;
			float height = changeValue.height;
			return (float)Math.Sqrt((double)(width * width + height * height)) / unitsXSecond;
		}

		// Token: 0x060001EE RID: 494 RVA: 0x0000B0FC File Offset: 0x000092FC
		public override void EvaluateAndApply(RectOptions options, Tween t, bool isRelative, DOGetter<Rect> getter, DOSetter<Rect> setter, float elapsed, Rect startValue, Rect changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice)
		{
			if (t.loopType == LoopType.Incremental)
			{
				int num = t.isComplete ? (t.completedLoops - 1) : t.completedLoops;
				startValue.x += changeValue.x * (float)num;
				startValue.y += changeValue.y * (float)num;
				startValue.width += changeValue.width * (float)num;
				startValue.height += changeValue.height * (float)num;
			}
			if (t.isSequenced && t.sequenceParent.loopType == LoopType.Incremental)
			{
				int num2 = ((t.loopType == LoopType.Incremental) ? t.loops : 1) * (t.sequenceParent.isComplete ? (t.sequenceParent.completedLoops - 1) : t.sequenceParent.completedLoops);
				startValue.x += changeValue.x * (float)num2;
				startValue.y += changeValue.y * (float)num2;
				startValue.width += changeValue.width * (float)num2;
				startValue.height += changeValue.height * (float)num2;
			}
			float num3 = EaseManager.Evaluate(t.easeType, t.customEase, elapsed, duration, t.easeOvershootOrAmplitude, t.easePeriod);
			startValue.x += changeValue.x * num3;
			startValue.y += changeValue.y * num3;
			startValue.width += changeValue.width * num3;
			startValue.height += changeValue.height * num3;
			if (options.snapping)
			{
				startValue.x = (float)Math.Round((double)startValue.x);
				startValue.y = (float)Math.Round((double)startValue.y);
				startValue.width = (float)Math.Round((double)startValue.width);
				startValue.height = (float)Math.Round((double)startValue.height);
			}
			setter(startValue);
		}
	}
}
