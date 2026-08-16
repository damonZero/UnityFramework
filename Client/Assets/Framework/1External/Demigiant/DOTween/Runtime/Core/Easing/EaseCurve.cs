using System;
using UnityEngine;

namespace DG.Tweening.Core.Easing
{
	// Token: 0x0200005F RID: 95
	public class EaseCurve
	{
		// Token: 0x060002FF RID: 767 RVA: 0x00012375 File Offset: 0x00010575
		public EaseCurve(AnimationCurve animCurve)
		{
			this._animCurve = animCurve;
		}

		// Token: 0x06000300 RID: 768 RVA: 0x00012384 File Offset: 0x00010584
		public float Evaluate(float time, float duration, float unusedOvershoot, float unusedPeriod)
		{
			float time2 = this._animCurve[this._animCurve.length - 1].time;
			float num = time / duration;
			return this._animCurve.Evaluate(num * time2);
		}

		// Token: 0x040001C0 RID: 448
		private readonly AnimationCurve _animCurve;
	}
}
