using System;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace DG.Tweening.Plugins
{
	// Token: 0x02000029 RID: 41
	public class Vector2Plugin : ABSTweenPlugin<Vector2, Vector2, VectorOptions>
	{
		// Token: 0x060001F9 RID: 505 RVA: 0x0000890C File Offset: 0x00006B0C
		public override void Reset(TweenerCore<Vector2, Vector2, VectorOptions> t)
		{
		}

		// Token: 0x060001FA RID: 506 RVA: 0x0000B560 File Offset: 0x00009760
		public override void SetFrom(TweenerCore<Vector2, Vector2, VectorOptions> t, bool isRelative)
		{
			Vector2 endValue = t.endValue;
			t.endValue = t.getter();
			t.startValue = (isRelative ? (t.endValue + endValue) : endValue);
			Vector2 vector = t.endValue;
			AxisConstraint axisConstraint = t.plugOptions.axisConstraint;
			if (axisConstraint != AxisConstraint.X)
			{
				if (axisConstraint != AxisConstraint.Y)
				{
					vector = t.startValue;
				}
				else
				{
					vector.y = t.startValue.y;
				}
			}
			else
			{
				vector.x = t.startValue.x;
			}
			if (t.plugOptions.snapping)
			{
				vector.x = (float)Math.Round((double)vector.x);
				vector.y = (float)Math.Round((double)vector.y);
			}
			t.setter(vector);
		}

		// Token: 0x060001FB RID: 507 RVA: 0x0000B62C File Offset: 0x0000982C
		public override void SetFrom(TweenerCore<Vector2, Vector2, VectorOptions> t, Vector2 fromValue, bool setImmediately, bool isRelative)
		{
			if (isRelative)
			{
				Vector2 vector = t.getter();
				t.endValue += vector;
				fromValue += vector;
			}
			t.startValue = fromValue;
			if (setImmediately)
			{
				AxisConstraint axisConstraint = t.plugOptions.axisConstraint;
				Vector2 vector2;
				if (axisConstraint != AxisConstraint.X)
				{
					if (axisConstraint != AxisConstraint.Y)
					{
						vector2 = fromValue;
					}
					else
					{
						vector2 = t.getter();
						vector2.y = fromValue.y;
					}
				}
				else
				{
					vector2 = t.getter();
					vector2.x = fromValue.x;
				}
				if (t.plugOptions.snapping)
				{
					vector2.x = (float)Math.Round((double)vector2.x);
					vector2.y = (float)Math.Round((double)vector2.y);
				}
				t.setter(vector2);
			}
		}

		// Token: 0x060001FC RID: 508 RVA: 0x00008A83 File Offset: 0x00006C83
		public override Vector2 ConvertToStartValue(TweenerCore<Vector2, Vector2, VectorOptions> t, Vector2 value)
		{
			return value;
		}

		// Token: 0x060001FD RID: 509 RVA: 0x0000B700 File Offset: 0x00009900
		public override void SetRelativeEndValue(TweenerCore<Vector2, Vector2, VectorOptions> t)
		{
			t.endValue += t.startValue;
		}

		// Token: 0x060001FE RID: 510 RVA: 0x0000B71C File Offset: 0x0000991C
		public override void SetChangeValue(TweenerCore<Vector2, Vector2, VectorOptions> t)
		{
			AxisConstraint axisConstraint = t.plugOptions.axisConstraint;
			if (axisConstraint == AxisConstraint.X)
			{
				t.changeValue = new Vector2(t.endValue.x - t.startValue.x, 0f);
				return;
			}
			if (axisConstraint != AxisConstraint.Y)
			{
				t.changeValue = t.endValue - t.startValue;
				return;
			}
			t.changeValue = new Vector2(0f, t.endValue.y - t.startValue.y);
		}

		// Token: 0x060001FF RID: 511 RVA: 0x0000B7A6 File Offset: 0x000099A6
		public override float GetSpeedBasedDuration(VectorOptions options, float unitsXSecond, Vector2 changeValue)
		{
			return changeValue.magnitude / unitsXSecond;
		}

		// Token: 0x06000200 RID: 512 RVA: 0x0000B7B4 File Offset: 0x000099B4
		public override void EvaluateAndApply(VectorOptions options, Tween t, bool isRelative, DOGetter<Vector2> getter, DOSetter<Vector2> setter, float elapsed, Vector2 startValue, Vector2 changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice)
		{
			if (t.loopType == LoopType.Incremental)
			{
				startValue += changeValue * (float)(t.isComplete ? (t.completedLoops - 1) : t.completedLoops);
			}
			if (t.isSequenced && t.sequenceParent.loopType == LoopType.Incremental)
			{
				startValue += changeValue * (float)((t.loopType == LoopType.Incremental) ? t.loops : 1) * (float)(t.sequenceParent.isComplete ? (t.sequenceParent.completedLoops - 1) : t.sequenceParent.completedLoops);
			}
			float num = EaseManager.Evaluate(t.easeType, t.customEase, elapsed, duration, t.easeOvershootOrAmplitude, t.easePeriod);
			AxisConstraint axisConstraint = options.axisConstraint;
			if (axisConstraint == AxisConstraint.X)
			{
				Vector2 vector = getter();
				vector.x = startValue.x + changeValue.x * num;
				if (options.snapping)
				{
					vector.x = (float)Math.Round((double)vector.x);
				}
				setter(vector);
				return;
			}
			if (axisConstraint != AxisConstraint.Y)
			{
				startValue.x += changeValue.x * num;
				startValue.y += changeValue.y * num;
				if (options.snapping)
				{
					startValue.x = (float)Math.Round((double)startValue.x);
					startValue.y = (float)Math.Round((double)startValue.y);
				}
				setter(startValue);
				return;
			}
			Vector2 vector2 = getter();
			vector2.y = startValue.y + changeValue.y * num;
			if (options.snapping)
			{
				vector2.y = (float)Math.Round((double)vector2.y);
			}
			setter(vector2);
		}
	}
}
