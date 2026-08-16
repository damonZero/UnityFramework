using System;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace DG.Tweening.Plugins
{
	// Token: 0x02000025 RID: 37
	public class QuaternionPlugin : ABSTweenPlugin<Quaternion, Vector3, QuaternionOptions>
	{
		// Token: 0x060001D4 RID: 468 RVA: 0x0000890C File Offset: 0x00006B0C
		public override void Reset(TweenerCore<Quaternion, Vector3, QuaternionOptions> t)
		{
		}

		// Token: 0x060001D5 RID: 469 RVA: 0x0000A320 File Offset: 0x00008520
		public override void SetFrom(TweenerCore<Quaternion, Vector3, QuaternionOptions> t, bool isRelative)
		{
			Vector3 endValue = t.endValue;
			t.endValue = t.getter().eulerAngles;
			if (t.plugOptions.rotateMode == RotateMode.Fast && !t.isRelative)
			{
				t.startValue = endValue;
			}
			else if (t.plugOptions.rotateMode == RotateMode.FastBeyond360)
			{
				t.startValue = t.endValue + endValue;
			}
			else
			{
				Quaternion quaternion = t.getter();
				if (t.plugOptions.rotateMode == RotateMode.WorldAxisAdd)
				{
					t.startValue = (quaternion * Quaternion.Inverse(quaternion) * Quaternion.Euler(endValue) * quaternion).eulerAngles;
				}
				else
				{
					t.startValue = (quaternion * Quaternion.Euler(endValue)).eulerAngles;
				}
				t.endValue = -endValue;
			}
			t.setter(Quaternion.Euler(t.startValue));
		}

		// Token: 0x060001D6 RID: 470 RVA: 0x0000A414 File Offset: 0x00008614
		public override void SetFrom(TweenerCore<Quaternion, Vector3, QuaternionOptions> t, Vector3 fromValue, bool setImmediately, bool isRelative)
		{
			if (isRelative)
			{
				Vector3 eulerAngles = t.getter().eulerAngles;
				t.endValue += eulerAngles;
				fromValue += eulerAngles;
			}
			t.startValue = fromValue;
			if (setImmediately)
			{
				t.setter(Quaternion.Euler(fromValue));
			}
		}

		// Token: 0x060001D7 RID: 471 RVA: 0x0000A46F File Offset: 0x0000866F
		public override Vector3 ConvertToStartValue(TweenerCore<Quaternion, Vector3, QuaternionOptions> t, Quaternion value)
		{
			return value.eulerAngles;
		}

		// Token: 0x060001D8 RID: 472 RVA: 0x0000A478 File Offset: 0x00008678
		public override void SetRelativeEndValue(TweenerCore<Quaternion, Vector3, QuaternionOptions> t)
		{
			t.endValue += t.startValue;
		}

		// Token: 0x060001D9 RID: 473 RVA: 0x0000A494 File Offset: 0x00008694
		public override void SetChangeValue(TweenerCore<Quaternion, Vector3, QuaternionOptions> t)
		{
			if (t.plugOptions.rotateMode == RotateMode.Fast && !t.isRelative)
			{
				Vector3 endValue = t.endValue;
				if (endValue.x > 360f)
				{
					endValue.x %= 360f;
				}
				if (endValue.y > 360f)
				{
					endValue.y %= 360f;
				}
				if (endValue.z > 360f)
				{
					endValue.z %= 360f;
				}
				Vector3 vector = endValue - t.startValue;
				float num = (vector.x > 0f) ? vector.x : (-vector.x);
				if (num > 180f)
				{
					vector.x = ((vector.x > 0f) ? (-(360f - num)) : (360f - num));
				}
				num = ((vector.y > 0f) ? vector.y : (-vector.y));
				if (num > 180f)
				{
					vector.y = ((vector.y > 0f) ? (-(360f - num)) : (360f - num));
				}
				num = ((vector.z > 0f) ? vector.z : (-vector.z));
				if (num > 180f)
				{
					vector.z = ((vector.z > 0f) ? (-(360f - num)) : (360f - num));
				}
				t.changeValue = vector;
				return;
			}
			if (t.plugOptions.rotateMode == RotateMode.FastBeyond360 || t.isRelative)
			{
				t.changeValue = t.endValue - t.startValue;
				return;
			}
			t.changeValue = t.endValue;
		}

		// Token: 0x060001DA RID: 474 RVA: 0x0000A650 File Offset: 0x00008850
		public override float GetSpeedBasedDuration(QuaternionOptions options, float unitsXSecond, Vector3 changeValue)
		{
			return changeValue.magnitude / unitsXSecond;
		}

		// Token: 0x060001DB RID: 475 RVA: 0x0000A65C File Offset: 0x0000885C
		public override void EvaluateAndApply(QuaternionOptions options, Tween t, bool isRelative, DOGetter<Quaternion> getter, DOSetter<Quaternion> setter, float elapsed, Vector3 startValue, Vector3 changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice)
		{
			Vector3 vector = startValue;
			if (t.loopType == LoopType.Incremental)
			{
				vector += changeValue * (float)(t.isComplete ? (t.completedLoops - 1) : t.completedLoops);
			}
			if (t.isSequenced && t.sequenceParent.loopType == LoopType.Incremental)
			{
				vector += changeValue * (float)((t.loopType == LoopType.Incremental) ? t.loops : 1) * (float)(t.sequenceParent.isComplete ? (t.sequenceParent.completedLoops - 1) : t.sequenceParent.completedLoops);
			}
			float num = EaseManager.Evaluate(t.easeType, t.customEase, elapsed, duration, t.easeOvershootOrAmplitude, t.easePeriod);
			RotateMode rotateMode = options.rotateMode;
			if (rotateMode - RotateMode.WorldAxisAdd > 1)
			{
				vector.x += changeValue.x * num;
				vector.y += changeValue.y * num;
				vector.z += changeValue.z * num;
				setter(Quaternion.Euler(vector));
				return;
			}
			Quaternion quaternion = Quaternion.Euler(startValue);
			vector.x = changeValue.x * num;
			vector.y = changeValue.y * num;
			vector.z = changeValue.z * num;
			if (options.rotateMode == RotateMode.WorldAxisAdd)
			{
				setter(quaternion * Quaternion.Inverse(quaternion) * Quaternion.Euler(vector) * quaternion);
				return;
			}
			setter(quaternion * Quaternion.Euler(vector));
		}
	}
}
