using System;
using DG.Tweening.Core.Easing;
using UnityEngine;

namespace DG.Tweening
{
	// Token: 0x0200000B RID: 11
	public class EaseFactory
	{
		// Token: 0x06000067 RID: 103 RVA: 0x00002EE8 File Offset: 0x000010E8
		public static EaseFunction StopMotion(int motionFps, Ease? ease = null)
		{
			EaseFunction customEase = EaseManager.ToEaseFunction((ease == null) ? DOTween.defaultEaseType : ease.Value);
			return EaseFactory.StopMotion(motionFps, customEase);
		}

		// Token: 0x06000068 RID: 104 RVA: 0x00002F19 File Offset: 0x00001119
		public static EaseFunction StopMotion(int motionFps, AnimationCurve animCurve)
		{
			return EaseFactory.StopMotion(motionFps, new EaseFunction(new EaseCurve(animCurve).Evaluate));
		}

		// Token: 0x06000069 RID: 105 RVA: 0x00002F32 File Offset: 0x00001132
		public static EaseFunction StopMotion(int motionFps, EaseFunction customEase)
		{
			float motionDelay = 1f / (float)motionFps;
			return delegate(float time, float duration, float overshootOrAmplitude, float period)
			{
				float time2 = (time < duration) ? (time - time % motionDelay) : time;
				return customEase(time2, duration, overshootOrAmplitude, period);
			};
		}
	}
}
