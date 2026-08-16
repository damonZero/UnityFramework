using System;

namespace DG.Tweening.Core.Easing
{
	// Token: 0x0200005D RID: 93
	public static class Bounce
	{
		// Token: 0x060002F8 RID: 760 RVA: 0x00011604 File Offset: 0x0000F804
		public static float EaseIn(float time, float duration, float unusedOvershootOrAmplitude, float unusedPeriod)
		{
			return 1f - Bounce.EaseOut(duration - time, duration, -1f, -1f);
		}

		// Token: 0x060002F9 RID: 761 RVA: 0x00011620 File Offset: 0x0000F820
		public static float EaseOut(float time, float duration, float unusedOvershootOrAmplitude, float unusedPeriod)
		{
			if ((time /= duration) < 0.363636374f)
			{
				return 7.5625f * time * time;
			}
			if (time < 0.727272749f)
			{
				return 7.5625f * (time -= 0.545454562f) * time + 0.75f;
			}
			if (time < 0.909090936f)
			{
				return 7.5625f * (time -= 0.8181818f) * time + 0.9375f;
			}
			return 7.5625f * (time -= 0.954545438f) * time + 0.984375f;
		}

		// Token: 0x060002FA RID: 762 RVA: 0x000116A0 File Offset: 0x0000F8A0
		public static float EaseInOut(float time, float duration, float unusedOvershootOrAmplitude, float unusedPeriod)
		{
			if (time < duration * 0.5f)
			{
				return Bounce.EaseIn(time * 2f, duration, -1f, -1f) * 0.5f;
			}
			return Bounce.EaseOut(time * 2f - duration, duration, -1f, -1f) * 0.5f + 0.5f;
		}
	}
}
