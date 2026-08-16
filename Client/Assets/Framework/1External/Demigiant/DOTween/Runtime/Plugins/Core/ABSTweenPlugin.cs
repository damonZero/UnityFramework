using System;
using DG.Tweening.Core;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Options;

namespace DG.Tweening.Plugins.Core
{
	// Token: 0x0200003F RID: 63
	public abstract class ABSTweenPlugin<T1, T2, TPlugOptions> : ITweenPlugin where TPlugOptions : struct, IPlugOptions
	{
		// Token: 0x0600023F RID: 575
		public abstract void Reset(TweenerCore<T1, T2, TPlugOptions> t);

		// Token: 0x06000240 RID: 576
		public abstract void SetFrom(TweenerCore<T1, T2, TPlugOptions> t, bool isRelative);

		// Token: 0x06000241 RID: 577
		public abstract void SetFrom(TweenerCore<T1, T2, TPlugOptions> t, T2 fromValue, bool setImmediately, bool isRelative);

		// Token: 0x06000242 RID: 578
		public abstract T2 ConvertToStartValue(TweenerCore<T1, T2, TPlugOptions> t, T1 value);

		// Token: 0x06000243 RID: 579
		public abstract void SetRelativeEndValue(TweenerCore<T1, T2, TPlugOptions> t);

		// Token: 0x06000244 RID: 580
		public abstract void SetChangeValue(TweenerCore<T1, T2, TPlugOptions> t);

		// Token: 0x06000245 RID: 581
		public abstract float GetSpeedBasedDuration(TPlugOptions options, float unitsXSecond, T2 changeValue);

		// Token: 0x06000246 RID: 582
		public abstract void EvaluateAndApply(TPlugOptions options, Tween t, bool isRelative, DOGetter<T1> getter, DOSetter<T1> setter, float elapsed, T2 startValue, T2 changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice);
	}
}
