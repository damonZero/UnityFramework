using System;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace DG.Tweening
{
	// Token: 0x02000009 RID: 9
	public static class DOVirtual
	{
		// Token: 0x06000061 RID: 97 RVA: 0x00002DF0 File Offset: 0x00000FF0
		public static Tweener Float(float from, float to, float duration, TweenCallback<float> onVirtualUpdate)
		{
			return DOTween.To(() => from, delegate(float x)
			{
				from = x;
			}, to, duration).OnUpdate(delegate
			{
				onVirtualUpdate(from);
			});
		}

		// Token: 0x06000062 RID: 98 RVA: 0x00002E41 File Offset: 0x00001041
		public static float EasedValue(float from, float to, float lifetimePercentage, Ease easeType)
		{
			return from + (to - from) * EaseManager.Evaluate(easeType, null, lifetimePercentage, 1f, DOTween.defaultEaseOvershootOrAmplitude, DOTween.defaultEasePeriod);
		}

		// Token: 0x06000063 RID: 99 RVA: 0x00002E60 File Offset: 0x00001060
		public static float EasedValue(float from, float to, float lifetimePercentage, Ease easeType, float overshoot)
		{
			return from + (to - from) * EaseManager.Evaluate(easeType, null, lifetimePercentage, 1f, overshoot, DOTween.defaultEasePeriod);
		}

		// Token: 0x06000064 RID: 100 RVA: 0x00002E7C File Offset: 0x0000107C
		public static float EasedValue(float from, float to, float lifetimePercentage, Ease easeType, float amplitude, float period)
		{
			return from + (to - from) * EaseManager.Evaluate(easeType, null, lifetimePercentage, 1f, amplitude, period);
		}

		// Token: 0x06000065 RID: 101 RVA: 0x00002E95 File Offset: 0x00001095
		public static float EasedValue(float from, float to, float lifetimePercentage, AnimationCurve easeCurve)
		{
			return from + (to - from) * EaseManager.Evaluate(Ease.INTERNAL_Custom, new EaseFunction(new EaseCurve(easeCurve).Evaluate), lifetimePercentage, 1f, DOTween.defaultEaseOvershootOrAmplitude, DOTween.defaultEasePeriod);
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00002EC5 File Offset: 0x000010C5
		public static Tween DelayedCall(float delay, TweenCallback callback, bool ignoreTimeScale = true)
		{
			return DOTween.Sequence().AppendInterval(delay).OnStepComplete(callback).SetUpdate(UpdateType.Normal, ignoreTimeScale).SetAutoKill(true);
		}
	}
}
