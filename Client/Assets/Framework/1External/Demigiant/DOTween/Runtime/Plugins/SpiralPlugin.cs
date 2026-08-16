using System;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Core;
using UnityEngine;

namespace DG.Tweening.Plugins
{
	/// <summary>
	/// Tweens a Vector3 along a spiral.
	/// EndValue represents the direction of the spiral
	/// </summary>
	// Token: 0x0200000A RID: 10
	public class SpiralPlugin : ABSTweenPlugin<Vector3, Vector3, SpiralOptions>
	{
		// Token: 0x06000021 RID: 33 RVA: 0x00002C34 File Offset: 0x00000E34
		public override void Reset(TweenerCore<Vector3, Vector3, SpiralOptions> t)
		{
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002C34 File Offset: 0x00000E34
		public override void SetFrom(TweenerCore<Vector3, Vector3, SpiralOptions> t, bool isRelative)
		{
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002C34 File Offset: 0x00000E34
		public override void SetFrom(TweenerCore<Vector3, Vector3, SpiralOptions> t, Vector3 fromValue, bool setImmediately, bool isRelative)
		{
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002C36 File Offset: 0x00000E36
		public static ABSTweenPlugin<Vector3, Vector3, SpiralOptions> Get()
		{
			return PluginsManager.GetCustomPlugin<SpiralPlugin, Vector3, Vector3, SpiralOptions>();
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002C3D File Offset: 0x00000E3D
		public override Vector3 ConvertToStartValue(TweenerCore<Vector3, Vector3, SpiralOptions> t, Vector3 value)
		{
			return value;
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002C34 File Offset: 0x00000E34
		public override void SetRelativeEndValue(TweenerCore<Vector3, Vector3, SpiralOptions> t)
		{
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00002C40 File Offset: 0x00000E40
		public override void SetChangeValue(TweenerCore<Vector3, Vector3, SpiralOptions> t)
		{
			t.plugOptions.speed = t.plugOptions.speed * (10f / t.plugOptions.frequency);
			t.plugOptions.axisQ = Quaternion.LookRotation(t.endValue, Vector3.up);
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00002C3D File Offset: 0x00000E3D
		public override float GetSpeedBasedDuration(SpiralOptions options, float unitsXSecond, Vector3 changeValue)
		{
			return unitsXSecond;
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00002C80 File Offset: 0x00000E80
		public override void EvaluateAndApply(SpiralOptions options, Tween t, bool isRelative, DOGetter<Vector3> getter, DOSetter<Vector3> setter, float elapsed, Vector3 startValue, Vector3 changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice)
		{
			float num = options.frequency;
			float num2 = options.depth;
			float num3 = options.speed;
			float num4 = EaseManager.Evaluate(t, elapsed, duration, t.easeOvershootOrAmplitude, t.easePeriod);
			bool flag = options.mode == SpiralMode.ExpandThenContract;
			float num5 = (flag && num4 > 0.5f) ? (0.5f - (num4 - 0.5f)) : num4;
			if (t.loopType == LoopType.Incremental)
			{
				if (flag)
				{
					int num6 = t.isComplete ? (t.completedLoops - 1) : (t.completedLoops + 1);
					num /= (float)num6;
					num2 *= (float)num6;
					num3 = num3 / (10f / options.frequency) * (10f / num);
				}
				else
				{
					int num7 = t.isComplete ? (t.completedLoops - 1) : t.completedLoops;
					num4 += (float)num7;
					num5 = num4;
				}
			}
			float num8 = duration * num3 * num4;
			options.unit = duration * num3 * num5;
			Vector3 vector;
			vector= new Vector3(options.unit * Mathf.Cos(num8 * num), options.unit * Mathf.Sin(num8 * num), num2 * num4);
			vector = options.axisQ * vector + startValue;
			if (options.snapping)
			{
				vector.x = (float)Math.Round((double)vector.x);
				vector.y = (float)Math.Round((double)vector.y);
				vector.z = (float)Math.Round((double)vector.z);
			}
			setter(vector);
		}

		// Token: 0x04000045 RID: 69
		public static readonly Vector3 DefaultDirection = Vector3.forward;
	}
}
