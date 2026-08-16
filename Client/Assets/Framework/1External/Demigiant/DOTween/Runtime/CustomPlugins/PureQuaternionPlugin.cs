using System;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace DG.Tweening.CustomPlugins
{
	// Token: 0x02000047 RID: 71
	public class PureQuaternionPlugin : ABSTweenPlugin<Quaternion, Quaternion, NoOptions>
	{
		// Token: 0x06000270 RID: 624 RVA: 0x0000E7F1 File Offset: 0x0000C9F1
		public static PureQuaternionPlugin Plug()
		{
			if (PureQuaternionPlugin._plug == null)
			{
				PureQuaternionPlugin._plug = new PureQuaternionPlugin();
			}
			return PureQuaternionPlugin._plug;
		}

		// Token: 0x06000271 RID: 625 RVA: 0x0000890C File Offset: 0x00006B0C
		public override void Reset(TweenerCore<Quaternion, Quaternion, NoOptions> t)
		{
		}

		// Token: 0x06000272 RID: 626 RVA: 0x0000E80C File Offset: 0x0000CA0C
		public override void SetFrom(TweenerCore<Quaternion, Quaternion, NoOptions> t, bool isRelative)
		{
			Quaternion endValue = t.endValue;
			t.endValue = t.getter();
			t.startValue = (isRelative ? (t.endValue * endValue) : endValue);
			t.setter(t.startValue);
		}

		// Token: 0x06000273 RID: 627 RVA: 0x0000E85C File Offset: 0x0000CA5C
		public override void SetFrom(TweenerCore<Quaternion, Quaternion, NoOptions> t, Quaternion fromValue, bool setImmediately, bool isRelative)
		{
			if (isRelative)
			{
				Quaternion quaternion = t.getter();
				t.endValue = quaternion * t.endValue;
				fromValue = quaternion * fromValue;
			}
			t.startValue = fromValue;
			if (setImmediately)
			{
				t.setter(fromValue);
			}
		}

		// Token: 0x06000274 RID: 628 RVA: 0x00008A83 File Offset: 0x00006C83
		public override Quaternion ConvertToStartValue(TweenerCore<Quaternion, Quaternion, NoOptions> t, Quaternion value)
		{
			return value;
		}

		// Token: 0x06000275 RID: 629 RVA: 0x0000E8AA File Offset: 0x0000CAAA
		public override void SetRelativeEndValue(TweenerCore<Quaternion, Quaternion, NoOptions> t)
		{
			t.endValue *= t.startValue;
		}

		// Token: 0x06000276 RID: 630 RVA: 0x0000E8C3 File Offset: 0x0000CAC3
		public override void SetChangeValue(TweenerCore<Quaternion, Quaternion, NoOptions> t)
		{
			t.changeValue = t.endValue;
		}

		// Token: 0x06000277 RID: 631 RVA: 0x0000E8D4 File Offset: 0x0000CAD4
		public override float GetSpeedBasedDuration(NoOptions options, float unitsXSecond, Quaternion changeValue)
		{
			return changeValue.eulerAngles.magnitude / unitsXSecond;
		}

		// Token: 0x06000278 RID: 632 RVA: 0x0000E8F4 File Offset: 0x0000CAF4
		public override void EvaluateAndApply(NoOptions options, Tween t, bool isRelative, DOGetter<Quaternion> getter, DOSetter<Quaternion> setter, float elapsed, Quaternion startValue, Quaternion changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice)
		{
			float num = EaseManager.Evaluate(t.easeType, t.customEase, elapsed, duration, t.easeOvershootOrAmplitude, t.easePeriod);
			setter(Quaternion.Slerp(startValue, changeValue, num));
		}

		// Token: 0x04000134 RID: 308
		private static PureQuaternionPlugin _plug;
	}
}
