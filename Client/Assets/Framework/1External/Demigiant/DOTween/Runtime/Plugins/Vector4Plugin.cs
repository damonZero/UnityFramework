using System;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace DG.Tweening.Plugins
{
	// Token: 0x0200002A RID: 42
	public class Vector4Plugin : ABSTweenPlugin<Vector4, Vector4, VectorOptions>
	{
		// Token: 0x06000202 RID: 514 RVA: 0x0000890C File Offset: 0x00006B0C
		public override void Reset(TweenerCore<Vector4, Vector4, VectorOptions> t)
		{
		}

		// Token: 0x06000203 RID: 515 RVA: 0x0000B984 File Offset: 0x00009B84
		public override void SetFrom(TweenerCore<Vector4, Vector4, VectorOptions> t, bool isRelative)
		{
			Vector4 endValue = t.endValue;
			t.endValue = t.getter();
			t.startValue = (isRelative ? (t.endValue + endValue) : endValue);
			Vector4 vector = t.endValue;
			AxisConstraint axisConstraint = t.plugOptions.axisConstraint;
			if (axisConstraint <= AxisConstraint.Y)
			{
				if (axisConstraint == AxisConstraint.X)
				{
					vector.x = t.startValue.x;
					goto IL_B3;
				}
				if (axisConstraint == AxisConstraint.Y)
				{
					vector.y = t.startValue.y;
					goto IL_B3;
				}
			}
			else
			{
				if (axisConstraint == AxisConstraint.Z)
				{
					vector.z = t.startValue.z;
					goto IL_B3;
				}
				if (axisConstraint == AxisConstraint.W)
				{
					vector.w = t.startValue.w;
					goto IL_B3;
				}
			}
			vector = t.startValue;
			IL_B3:
			if (t.plugOptions.snapping)
			{
				vector.x = (float)Math.Round((double)vector.x);
				vector.y = (float)Math.Round((double)vector.y);
				vector.z = (float)Math.Round((double)vector.z);
				vector.w = (float)Math.Round((double)vector.w);
			}
			t.setter(vector);
		}

		// Token: 0x06000204 RID: 516 RVA: 0x0000BAB0 File Offset: 0x00009CB0
		public override void SetFrom(TweenerCore<Vector4, Vector4, VectorOptions> t, Vector4 fromValue, bool setImmediately, bool isRelative)
		{
			if (isRelative)
			{
				Vector4 vector = t.getter();
				t.endValue += vector;
				fromValue += vector;
			}
			t.startValue = fromValue;
			if (setImmediately)
			{
				AxisConstraint axisConstraint = t.plugOptions.axisConstraint;
				Vector4 vector2;
				if (axisConstraint <= AxisConstraint.Y)
				{
					if (axisConstraint == AxisConstraint.X)
					{
						vector2 = t.getter();
						vector2.x = fromValue.x;
						goto IL_CB;
					}
					if (axisConstraint == AxisConstraint.Y)
					{
						vector2 = t.getter();
						vector2.y = fromValue.y;
						goto IL_CB;
					}
				}
				else
				{
					if (axisConstraint == AxisConstraint.Z)
					{
						vector2 = t.getter();
						vector2.z = fromValue.z;
						goto IL_CB;
					}
					if (axisConstraint == AxisConstraint.W)
					{
						vector2 = t.getter();
						vector2.w = fromValue.w;
						goto IL_CB;
					}
				}
				vector2 = fromValue;
				IL_CB:
				if (t.plugOptions.snapping)
				{
					vector2.x = (float)Math.Round((double)vector2.x);
					vector2.y = (float)Math.Round((double)vector2.y);
					vector2.z = (float)Math.Round((double)vector2.z);
					vector2.w = (float)Math.Round((double)vector2.w);
				}
				t.setter(vector2);
			}
		}

		// Token: 0x06000205 RID: 517 RVA: 0x00008A83 File Offset: 0x00006C83
		public override Vector4 ConvertToStartValue(TweenerCore<Vector4, Vector4, VectorOptions> t, Vector4 value)
		{
			return value;
		}

		// Token: 0x06000206 RID: 518 RVA: 0x0000BBF1 File Offset: 0x00009DF1
		public override void SetRelativeEndValue(TweenerCore<Vector4, Vector4, VectorOptions> t)
		{
			t.endValue += t.startValue;
		}

		// Token: 0x06000207 RID: 519 RVA: 0x0000BC0C File Offset: 0x00009E0C
		public override void SetChangeValue(TweenerCore<Vector4, Vector4, VectorOptions> t)
		{
			AxisConstraint axisConstraint = t.plugOptions.axisConstraint;
			if (axisConstraint <= AxisConstraint.Y)
			{
				if (axisConstraint == AxisConstraint.X)
				{
					t.changeValue = new Vector4(t.endValue.x - t.startValue.x, 0f, 0f, 0f);
					return;
				}
				if (axisConstraint == AxisConstraint.Y)
				{
					t.changeValue = new Vector4(0f, t.endValue.y - t.startValue.y, 0f, 0f);
					return;
				}
			}
			else
			{
				if (axisConstraint == AxisConstraint.Z)
				{
					t.changeValue = new Vector4(0f, 0f, t.endValue.z - t.startValue.z, 0f);
					return;
				}
				if (axisConstraint == AxisConstraint.W)
				{
					t.changeValue = new Vector4(0f, 0f, 0f, t.endValue.w - t.startValue.w);
					return;
				}
			}
			t.changeValue = t.endValue - t.startValue;
		}

		// Token: 0x06000208 RID: 520 RVA: 0x0000BD26 File Offset: 0x00009F26
		public override float GetSpeedBasedDuration(VectorOptions options, float unitsXSecond, Vector4 changeValue)
		{
			return changeValue.magnitude / unitsXSecond;
		}

		// Token: 0x06000209 RID: 521 RVA: 0x0000BD34 File Offset: 0x00009F34
		public override void EvaluateAndApply(VectorOptions options, Tween t, bool isRelative, DOGetter<Vector4> getter, DOSetter<Vector4> setter, float elapsed, Vector4 startValue, Vector4 changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice)
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
			if (axisConstraint <= AxisConstraint.Y)
			{
				if (axisConstraint == AxisConstraint.X)
				{
					Vector4 vector = getter();
					vector.x = startValue.x + changeValue.x * num;
					if (options.snapping)
					{
						vector.x = (float)Math.Round((double)vector.x);
					}
					setter(vector);
					return;
				}
				if (axisConstraint == AxisConstraint.Y)
				{
					Vector4 vector2 = getter();
					vector2.y = startValue.y + changeValue.y * num;
					if (options.snapping)
					{
						vector2.y = (float)Math.Round((double)vector2.y);
					}
					setter(vector2);
					return;
				}
			}
			else
			{
				if (axisConstraint == AxisConstraint.Z)
				{
					Vector4 vector3 = getter();
					vector3.z = startValue.z + changeValue.z * num;
					if (options.snapping)
					{
						vector3.z = (float)Math.Round((double)vector3.z);
					}
					setter(vector3);
					return;
				}
				if (axisConstraint == AxisConstraint.W)
				{
					Vector4 vector4 = getter();
					vector4.w = startValue.w + changeValue.w * num;
					if (options.snapping)
					{
						vector4.w = (float)Math.Round((double)vector4.w);
					}
					setter(vector4);
					return;
				}
			}
			startValue.x += changeValue.x * num;
			startValue.y += changeValue.y * num;
			startValue.z += changeValue.z * num;
			startValue.w += changeValue.w * num;
			if (options.snapping)
			{
				startValue.x = (float)Math.Round((double)startValue.x);
				startValue.y = (float)Math.Round((double)startValue.y);
				startValue.z = (float)Math.Round((double)startValue.z);
				startValue.w = (float)Math.Round((double)startValue.w);
			}
			setter(startValue);
		}
	}
}
