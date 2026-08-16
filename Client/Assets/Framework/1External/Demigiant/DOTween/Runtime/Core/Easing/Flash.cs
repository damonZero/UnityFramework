using System;
using UnityEngine;

namespace DG.Tweening.Core.Easing
{
	// Token: 0x02000060 RID: 96
	public static class Flash
	{
		// Token: 0x06000301 RID: 769 RVA: 0x000123C4 File Offset: 0x000105C4
		public static float Ease(float time, float duration, float overshootOrAmplitude, float period)
		{
			int num = Mathf.CeilToInt(time / duration * overshootOrAmplitude);
			float num2 = duration / overshootOrAmplitude;
			time -= num2 * (float)(num - 1);
			float num3 = (float)((num % 2 != 0) ? 1 : -1);
			if (num3 < 0f)
			{
				time -= num2;
			}
			float res = time * num3 / num2;
			return Flash.WeightedEase(overshootOrAmplitude, period, num, num2, num3, res);
		}

		// Token: 0x06000302 RID: 770 RVA: 0x00012414 File Offset: 0x00010614
		public static float EaseIn(float time, float duration, float overshootOrAmplitude, float period)
		{
			int num = Mathf.CeilToInt(time / duration * overshootOrAmplitude);
			float num2 = duration / overshootOrAmplitude;
			time -= num2 * (float)(num - 1);
			float num3 = (float)((num % 2 != 0) ? 1 : -1);
			if (num3 < 0f)
			{
				time -= num2;
			}
			time *= num3;
			float res = (time /= num2) * time;
			return Flash.WeightedEase(overshootOrAmplitude, period, num, num2, num3, res);
		}

		// Token: 0x06000303 RID: 771 RVA: 0x0001246C File Offset: 0x0001066C
		public static float EaseOut(float time, float duration, float overshootOrAmplitude, float period)
		{
			int num = Mathf.CeilToInt(time / duration * overshootOrAmplitude);
			float num2 = duration / overshootOrAmplitude;
			time -= num2 * (float)(num - 1);
			float num3 = (float)((num % 2 != 0) ? 1 : -1);
			if (num3 < 0f)
			{
				time -= num2;
			}
			time *= num3;
			float res = -(time /= num2) * (time - 2f);
			return Flash.WeightedEase(overshootOrAmplitude, period, num, num2, num3, res);
		}

		// Token: 0x06000304 RID: 772 RVA: 0x000124CC File Offset: 0x000106CC
		public static float EaseInOut(float time, float duration, float overshootOrAmplitude, float period)
		{
			int num = Mathf.CeilToInt(time / duration * overshootOrAmplitude);
			float num2 = duration / overshootOrAmplitude;
			time -= num2 * (float)(num - 1);
			float num3 = (float)((num % 2 != 0) ? 1 : -1);
			if (num3 < 0f)
			{
				time -= num2;
			}
			time *= num3;
			float res = ((time /= num2 * 0.5f) < 1f) ? (0.5f * time * time) : (-0.5f * ((time -= 1f) * (time - 2f) - 1f));
			return Flash.WeightedEase(overshootOrAmplitude, period, num, num2, num3, res);
		}

		// Token: 0x06000305 RID: 773 RVA: 0x00012558 File Offset: 0x00010758
		private static float WeightedEase(float overshootOrAmplitude, float period, int stepIndex, float stepDuration, float dir, float res)
		{
			float num = 0f;
			float num2 = 0f;
			if (dir > 0f && (int)overshootOrAmplitude % 2 == 0)
			{
				stepIndex++;
			}
			else if (dir < 0f && (int)overshootOrAmplitude % 2 != 0)
			{
				stepIndex++;
			}
			if (period > 0f)
			{
				float num3 = (float)Math.Truncate((double)overshootOrAmplitude);
				num2 = overshootOrAmplitude - num3;
				if (num3 % 2f > 0f)
				{
					num2 = 1f - num2;
				}
				num2 = num2 * (float)stepIndex / overshootOrAmplitude;
				num = res * (overshootOrAmplitude - (float)stepIndex) / overshootOrAmplitude;
			}
			else if (period < 0f)
			{
				period = -period;
				num = res * (float)stepIndex / overshootOrAmplitude;
			}
			float num4 = num - res;
			res += num4 * period + num2;
			if (res > 1f)
			{
				res = 1f;
			}
			return res;
		}
	}
}
