using System;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace DG.Tweening.Plugins
{
	// Token: 0x02000022 RID: 34
	public class PathPlugin : ABSTweenPlugin<Vector3, Path, PathOptions>
	{
		// Token: 0x060001B7 RID: 439 RVA: 0x00009714 File Offset: 0x00007914
		public override void Reset(TweenerCore<Vector3, Path, PathOptions> t)
		{
			t.endValue.Destroy();
			t.startValue = (t.endValue = (t.changeValue = null));
		}

		// Token: 0x060001B8 RID: 440 RVA: 0x0000890C File Offset: 0x00006B0C
		public override void SetFrom(TweenerCore<Vector3, Path, PathOptions> t, bool isRelative)
		{
		}

		// Token: 0x060001B9 RID: 441 RVA: 0x0000890C File Offset: 0x00006B0C
		public override void SetFrom(TweenerCore<Vector3, Path, PathOptions> t, Path fromValue, bool setImmediately, bool isRelative)
		{
		}

		// Token: 0x060001BA RID: 442 RVA: 0x00009745 File Offset: 0x00007945
		public static ABSTweenPlugin<Vector3, Path, PathOptions> Get()
		{
			return PluginsManager.GetCustomPlugin<PathPlugin, Vector3, Path, PathOptions>();
		}

		// Token: 0x060001BB RID: 443 RVA: 0x0000974C File Offset: 0x0000794C
		public override Path ConvertToStartValue(TweenerCore<Vector3, Path, PathOptions> t, Vector3 value)
		{
			return t.endValue;
		}

		// Token: 0x060001BC RID: 444 RVA: 0x00009754 File Offset: 0x00007954
		public override void SetRelativeEndValue(TweenerCore<Vector3, Path, PathOptions> t)
		{
			if (t.endValue.isFinalized)
			{
				return;
			}
			Vector3 vector = t.getter();
			int num = t.endValue.wps.Length;
			for (int i = 0; i < num; i++)
			{
				t.endValue.wps[i] += vector;
			}
		}

		// Token: 0x060001BD RID: 445 RVA: 0x000097B8 File Offset: 0x000079B8
		public override void SetChangeValue(TweenerCore<Vector3, Path, PathOptions> t)
		{
			Transform transform = ((Component)t.target).transform;
			if (t.plugOptions.orientType == OrientType.ToPath && t.plugOptions.useLocalPosition)
			{
				t.plugOptions.parent = transform.parent;
			}
			if (t.endValue.isFinalized)
			{
				t.changeValue = t.endValue;
				return;
			}
			Vector3 vector = t.getter();
			Path endValue = t.endValue;
			int num = endValue.wps.Length;
			int num2 = 0;
			bool flag = false;
			bool flag2 = false;
			if (!Utils.Vector3AreApproximatelyEqual(endValue.wps[0], vector))
			{
				flag = true;
				num2++;
			}
			if (t.plugOptions.isClosedPath)
			{
				Vector3 vector2 = endValue.wps[num - 1];
				if (endValue.type == PathType.CubicBezier)
				{
					if (num < 3)
					{
						Debug.LogError("CubicBezier paths must contain waypoints in multiple of 3 excluding the starting point added automatically by DOTween (1: waypoint, 2: IN control point, 3: OUT control point — the minimum amount of waypoints for a single curve is 3)");
					}
					else
					{
						vector2 = endValue.wps[num - 3];
					}
				}
				if (vector2 != vector)
				{
					flag2 = true;
					num2++;
				}
			}
			Vector3[] array = new Vector3[num + num2];
			int num3 = flag ? 1 : 0;
			if (flag)
			{
				array[0] = vector;
			}
			for (int i = 0; i < num; i++)
			{
				array[i + num3] = endValue.wps[i];
			}
			if (flag2)
			{
				array[array.Length - 1] = array[0];
			}
			endValue.wps = array;
			endValue.addedExtraStartWp = flag;
			endValue.addedExtraEndWp = flag2;
			endValue.FinalizePath(t.plugOptions.isClosedPath, t.plugOptions.lockPositionAxis, vector);
			t.plugOptions.startupRot = transform.rotation;
			t.plugOptions.startupZRot = transform.eulerAngles.z;
			t.changeValue = t.endValue;
		}

		// Token: 0x060001BE RID: 446 RVA: 0x00009983 File Offset: 0x00007B83
		public override float GetSpeedBasedDuration(PathOptions options, float unitsXSecond, Path changeValue)
		{
			return changeValue.length / unitsXSecond;
		}

		// Token: 0x060001BF RID: 447 RVA: 0x00009990 File Offset: 0x00007B90
		public override void EvaluateAndApply(PathOptions options, Tween t, bool isRelative, DOGetter<Vector3> getter, DOSetter<Vector3> setter, float elapsed, Path startValue, Path changeValue, float duration, bool usingInversePosition, UpdateNotice updateNotice)
		{
			if (t.loopType == LoopType.Incremental && !options.isClosedPath)
			{
				int num = t.isComplete ? (t.completedLoops - 1) : t.completedLoops;
				if (num > 0)
				{
					changeValue = changeValue.CloneIncremental(num);
				}
			}
			float perc = EaseManager.Evaluate(t.easeType, t.customEase, elapsed, duration, t.easeOvershootOrAmplitude, t.easePeriod);
			float num2 = changeValue.ConvertToConstantPathPerc(perc);
			Vector3 point = changeValue.GetPoint(num2, false);
			changeValue.targetPosition = point;
			setter(point);
			if (options.mode != PathMode.Ignore && options.orientType != OrientType.None)
			{
				this.SetOrientation(options, t, changeValue, num2, point, updateNotice);
			}
			bool flag = !usingInversePosition;
			if (t.isBackwards)
			{
				flag = !flag;
			}
			int waypointIndexFromPerc = changeValue.GetWaypointIndexFromPerc(perc, flag);
			if (waypointIndexFromPerc != t.miscInt)
			{
				int miscInt = t.miscInt;
				t.miscInt = waypointIndexFromPerc;
				if (t.onWaypointChange != null)
				{
					bool flag2 = t.isBackwards;
					if (t.hasLoops && t.loopType == LoopType.Yoyo)
					{
						flag2 = ((!t.isBackwards && t.completedLoops % 2 != 0) || (t.isBackwards && t.completedLoops % 2 == 0));
					}
					if (flag2)
					{
						for (int i = miscInt - 1; i > waypointIndexFromPerc - 1; i--)
						{
							Tween.OnTweenCallback<int>(t.onWaypointChange, t, i);
						}
					}
					else
					{
						for (int j = miscInt + 1; j < waypointIndexFromPerc; j++)
						{
							Tween.OnTweenCallback<int>(t.onWaypointChange, t, j);
						}
					}
					Tween.OnTweenCallback<int>(t.onWaypointChange, t, waypointIndexFromPerc);
				}
			}
		}

		// Token: 0x060001C0 RID: 448 RVA: 0x00009B24 File Offset: 0x00007D24
		public void SetOrientation(PathOptions options, Tween t, Path path, float pathPerc, Vector3 tPos, UpdateNotice updateNotice)
		{
			Transform transform = ((Component)t.target).transform;
			Quaternion quaternion = Quaternion.identity;
			if (updateNotice == UpdateNotice.RewindStep)
			{
				transform.rotation = options.startupRot;
			}
			switch (options.orientType)
			{
			case OrientType.ToPath:
			{
				Vector3 vector;
				if (path.type == PathType.Linear && options.lookAhead <= 0.0001f)
				{
					vector = tPos + path.wps[path.linearWPIndex] - path.wps[path.linearWPIndex - 1];
				}
				else
				{
					float num = pathPerc + options.lookAhead;
					if (num > 1f)
					{
						num = (options.isClosedPath ? (num - 1f) : ((path.type == PathType.Linear) ? 1f : 1.00001f));
					}
					vector = path.GetPoint(num, false);
				}
				if (path.type == PathType.Linear)
				{
					Vector3 vector2 = path.wps[path.wps.Length - 1];
					if (vector == vector2)
					{
						vector = ((tPos == vector2) ? (vector2 + (vector2 - path.wps[path.wps.Length - 2])) : vector2);
					}
				}
				Vector3 vector3 = transform.up;
				if (options.useLocalPosition && options.parent != null)
				{
					vector = options.parent.TransformPoint(vector);
				}
				if (options.lockRotationAxis != AxisConstraint.None)
				{
					if ((options.lockRotationAxis & AxisConstraint.X) == AxisConstraint.X)
					{
						Vector3 vector4 = transform.InverseTransformPoint(vector);
						vector4.y = 0f;
						vector = transform.TransformPoint(vector4);
						vector3 = ((options.useLocalPosition && options.parent != null) ? options.parent.up : Vector3.up);
					}
					if ((options.lockRotationAxis & AxisConstraint.Y) == AxisConstraint.Y)
					{
						Vector3 vector5 = transform.InverseTransformPoint(vector);
						if (vector5.z < 0f)
						{
							vector5.z = -vector5.z;
						}
						vector5.x = 0f;
						vector = transform.TransformPoint(vector5);
					}
					if ((options.lockRotationAxis & AxisConstraint.Z) == AxisConstraint.Z)
					{
						if (options.useLocalPosition && options.parent != null)
						{
							vector3 = options.parent.TransformDirection(Vector3.up);
						}
						else
						{
							vector3 = transform.TransformDirection(Vector3.up);
						}
						vector3.z = options.startupZRot;
					}
				}
				if (options.mode == PathMode.Full3D)
				{
					Vector3 vector6 = vector - transform.position;
					if (vector6 == Vector3.zero)
					{
						vector6 = transform.forward;
					}
					quaternion = Quaternion.LookRotation(vector6, vector3);
				}
				else
				{
					float num2 = 0f;
					float num3 = Utils.Angle2D(transform.position, vector);
					if (num3 < 0f)
					{
						num3 = 360f + num3;
					}
					if (options.mode == PathMode.Sidescroller2D)
					{
						num2 = (float)((vector.x < transform.position.x) ? 180 : 0);
						if (num3 > 90f && num3 < 270f)
						{
							num3 = 180f - num3;
						}
					}
					quaternion = Quaternion.Euler(0f, num2, num3);
				}
				break;
			}
			case OrientType.LookAtTransform:
				if (options.lookAtTransform != null)
				{
					path.lookAtPosition = new Vector3?(options.lookAtTransform.position);
					quaternion = Quaternion.LookRotation(options.lookAtTransform.position - transform.position, options.stableZRotation ? Vector3.up : transform.up);
				}
				break;
			case OrientType.LookAtPosition:
				path.lookAtPosition = new Vector3?(options.lookAtPosition);
				quaternion = Quaternion.LookRotation(options.lookAtPosition - transform.position, options.stableZRotation ? Vector3.up : transform.up);
				break;
			}
			if (options.hasCustomForwardDirection)
			{
				quaternion *= options.forward;
			}
			DOTweenExternalCommand.Dispatch_SetOrientationOnPath(options, t, quaternion, transform);
		}

		// Token: 0x040000D3 RID: 211
		public const float MinLookAhead = 0.0001f;
	}
}
