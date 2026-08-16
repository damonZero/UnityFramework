using System;
using DG.Tweening.Core;
using UnityEngine;

namespace DG.Tweening.Plugins.Core.PathCore
{
	// Token: 0x02000046 RID: 70
	[Serializable]
	public class Path
	{
		// Token: 0x06000262 RID: 610 RVA: 0x0000DFA4 File Offset: 0x0000C1A4
		public Path(PathType type, Vector3[] waypoints, int subdivisionsXSegment, Color? gizmoColor = null)
		{
			this.type = type;
			this.subdivisionsXSegment = subdivisionsXSegment;
			if (gizmoColor != null)
			{
				this.gizmoColor = gizmoColor.Value;
			}
			this.AssignWaypoints(waypoints, true);
			this.AssignDecoder(type);
			if (TweenManager.isUnityEditor)
			{
				DOTween.GizmosDelegates.Add(new TweenCallback(this.Draw));
			}
		}

		// Token: 0x06000263 RID: 611 RVA: 0x0000E02D File Offset: 0x0000C22D
		internal Path()
		{
		}

		// Token: 0x06000264 RID: 612 RVA: 0x0000E05C File Offset: 0x0000C25C
		public void FinalizePath(bool isClosedPath, AxisConstraint lockPositionAxes, Vector3 currTargetVal)
		{
			if (lockPositionAxes != AxisConstraint.None)
			{
				bool flag = (lockPositionAxes & AxisConstraint.X) == AxisConstraint.X;
				bool flag2 = (lockPositionAxes & AxisConstraint.Y) == AxisConstraint.Y;
				bool flag3 = (lockPositionAxes & AxisConstraint.Z) == AxisConstraint.Z;
				for (int i = 0; i < this.wps.Length; i++)
				{
					Vector3 vector = this.wps[i];
					this.wps[i] = new Vector3(flag ? currTargetVal.x : vector.x, flag2 ? currTargetVal.y : vector.y, flag3 ? currTargetVal.z : vector.z);
				}
			}
			this._decoder.FinalizePath(this, this.wps, isClosedPath);
			this.isFinalized = true;
		}

		// Token: 0x06000265 RID: 613 RVA: 0x0000E103 File Offset: 0x0000C303
		internal Vector3 GetPoint(float perc, bool convertToConstantPerc = false)
		{
			if (convertToConstantPerc)
			{
				perc = this.ConvertToConstantPathPerc(perc);
			}
			return this._decoder.GetPoint(perc, this.wps, this, this.controlPoints);
		}

		// Token: 0x06000266 RID: 614 RVA: 0x0000E12C File Offset: 0x0000C32C
		internal float ConvertToConstantPathPerc(float perc)
		{
			if (this.type == PathType.Linear)
			{
				return perc;
			}
			if (perc > 0f && perc < 1f)
			{
				float num = this.length * perc;
				float num2 = 0f;
				float num3 = 0f;
				float num4 = 0f;
				float num5 = 0f;
				int num6 = this.lengthsTable.Length;
				int i = 0;
				while (i < num6)
				{
					if (this.lengthsTable[i] > num)
					{
						num4 = this.timesTable[i];
						num5 = this.lengthsTable[i];
						if (i > 0)
						{
							num3 = this.lengthsTable[i - 1];
							break;
						}
						break;
					}
					else
					{
						num2 = this.timesTable[i];
						i++;
					}
				}
				perc = num2 + (num - num3) / (num5 - num3) * (num4 - num2);
			}
			if (perc > 1f)
			{
				perc = 1f;
			}
			else if (perc < 0f)
			{
				perc = 0f;
			}
			return perc;
		}

		// Token: 0x06000267 RID: 615 RVA: 0x0000E208 File Offset: 0x0000C408
		internal int GetWaypointIndexFromPerc(float perc, bool isMovingForward)
		{
			if (perc >= 1f)
			{
				return this.wps.Length - 1;
			}
			if (perc <= 0f)
			{
				return 0;
			}
			float num = this.length * perc;
			float num2 = 0f;
			int i = 0;
			int num3 = this.wpLengths.Length;
			while (i < num3)
			{
				num2 += this.wpLengths[i];
				if (i == num3 - 1)
				{
					if (!isMovingForward)
					{
						return i;
					}
					return i - 1;
				}
				else if (num2 >= num)
				{
					if (num2 <= num)
					{
						return i;
					}
					if (!isMovingForward)
					{
						return i;
					}
					return i - 1;
				}
				else
				{
					i++;
				}
			}
			return 0;
		}

		// Token: 0x06000268 RID: 616 RVA: 0x0000E284 File Offset: 0x0000C484
		internal static Vector3[] GetDrawPoints(Path p, int drawSubdivisionsXSegment)
		{
			int num = p.wps.Length;
			if (p.type == PathType.Linear)
			{
				return p.wps;
			}
			int num2 = num * drawSubdivisionsXSegment;
			Vector3[] array = new Vector3[num2 + 1];
			for (int i = 0; i <= num2; i++)
			{
				float perc = (float)i / (float)num2;
				Vector3 point = p.GetPoint(perc, false);
				array[i] = point;
			}
			return array;
		}

		// Token: 0x06000269 RID: 617 RVA: 0x0000E2E0 File Offset: 0x0000C4E0
		public static void RefreshNonLinearDrawWps(Path p)
		{
			int num = p.wps.Length * 10;
			if (p.nonLinearDrawWps == null || p.nonLinearDrawWps.Length != num + 1)
			{
				p.nonLinearDrawWps = new Vector3[num + 1];
			}
			for (int i = 0; i <= num; i++)
			{
				float perc = (float)i / (float)num;
				Vector3 point = p.GetPoint(perc, false);
				p.nonLinearDrawWps[i] = point;
			}
		}

		// Token: 0x0600026A RID: 618 RVA: 0x0000E344 File Offset: 0x0000C544
		internal void Destroy()
		{
			if (TweenManager.isUnityEditor)
			{
				DOTween.GizmosDelegates.Remove(new TweenCallback(this.Draw));
			}
			this.wps = null;
			this.wpLengths = (this.timesTable = (this.lengthsTable = null));
			this.nonLinearDrawWps = null;
			this.isFinalized = false;
		}

		// Token: 0x0600026B RID: 619 RVA: 0x0000E3A0 File Offset: 0x0000C5A0
		internal Path CloneIncremental(int loopIncrement)
		{
			if (this._incrementalClone != null)
			{
				if (this._incrementalIndex == loopIncrement)
				{
					return this._incrementalClone;
				}
				this._incrementalClone.Destroy();
			}
			int num = this.wps.Length;
			Vector3 vector = this.wps[num - 1] - this.wps[0];
			Vector3[] array = new Vector3[this.wps.Length];
			for (int i = 0; i < num; i++)
			{
				array[i] = this.wps[i] + vector * (float)loopIncrement;
			}
			int num2 = this.controlPoints.Length;
			ControlPoint[] array2 = new ControlPoint[num2];
			for (int j = 0; j < num2; j++)
			{
				array2[j] = this.controlPoints[j] + vector * (float)loopIncrement;
			}
			Vector3[] array3 = null;
			if (this.nonLinearDrawWps != null)
			{
				int num3 = this.nonLinearDrawWps.Length;
				array3 = new Vector3[num3];
				for (int k = 0; k < num3; k++)
				{
					array3[k] = this.nonLinearDrawWps[k] + vector * (float)loopIncrement;
				}
			}
			this._incrementalClone = new Path();
			this._incrementalIndex = loopIncrement;
			this._incrementalClone.type = this.type;
			this._incrementalClone.subdivisionsXSegment = this.subdivisionsXSegment;
			this._incrementalClone.subdivisions = this.subdivisions;
			this._incrementalClone.wps = array;
			this._incrementalClone.controlPoints = array2;
			if (TweenManager.isUnityEditor)
			{
				DOTween.GizmosDelegates.Add(new TweenCallback(this._incrementalClone.Draw));
			}
			this._incrementalClone.length = this.length;
			this._incrementalClone.wpLengths = this.wpLengths;
			this._incrementalClone.timesTable = this.timesTable;
			this._incrementalClone.lengthsTable = this.lengthsTable;
			this._incrementalClone._decoder = this._decoder;
			this._incrementalClone.nonLinearDrawWps = array3;
			this._incrementalClone.targetPosition = this.targetPosition;
			this._incrementalClone.lookAtPosition = this.lookAtPosition;
			this._incrementalClone.isFinalized = true;
			return this._incrementalClone;
		}

		// Token: 0x0600026C RID: 620 RVA: 0x0000E5E8 File Offset: 0x0000C7E8
		public void AssignWaypoints(Vector3[] newWps, bool cloneWps = false)
		{
			if (cloneWps)
			{
				int num = newWps.Length;
				this.wps = new Vector3[num];
				for (int i = 0; i < num; i++)
				{
					this.wps[i] = newWps[i];
				}
				return;
			}
			this.wps = newWps;
		}

		// Token: 0x0600026D RID: 621 RVA: 0x0000E630 File Offset: 0x0000C830
		public void AssignDecoder(PathType pathType)
		{
			this.type = pathType;
			if (pathType == PathType.Linear)
			{
				if (Path._linearDecoder == null)
				{
					Path._linearDecoder = new LinearDecoder();
				}
				this._decoder = Path._linearDecoder;
				return;
			}
			if (pathType != PathType.CubicBezier)
			{
				if (Path._catmullRomDecoder == null)
				{
					Path._catmullRomDecoder = new CatmullRomDecoder();
				}
				this._decoder = Path._catmullRomDecoder;
				return;
			}
			if (Path._cubicBezierDecoder == null)
			{
				Path._cubicBezierDecoder = new CubicBezierDecoder();
			}
			this._decoder = Path._cubicBezierDecoder;
		}

		// Token: 0x0600026E RID: 622 RVA: 0x0000E6A3 File Offset: 0x0000C8A3
		internal void Draw()
		{
			Path.Draw(this);
		}

		// Token: 0x0600026F RID: 623 RVA: 0x0000E6AC File Offset: 0x0000C8AC
		private static void Draw(Path p)
		{
			if (p.timesTable == null)
			{
				return;
			}
			Color color = p.gizmoColor;
			color.a *= 0.5f;
			Gizmos.color = p.gizmoColor;
			int num = p.wps.Length;
			if (p._changed || (p.type != PathType.Linear && p.nonLinearDrawWps == null))
			{
				p._changed = false;
				if (p.type != PathType.Linear)
				{
					Path.RefreshNonLinearDrawWps(p);
				}
			}
			if (p.type == PathType.Linear)
			{
				Vector3 vector = p.wps[0];
				for (int i = 0; i < num; i++)
				{
					Vector3 vector2 = p.wps[i];
					Gizmos.DrawLine(vector2, vector);
					vector = vector2;
				}
			}
			else
			{
				Vector3 vector = p.nonLinearDrawWps[0];
				int num2 = p.nonLinearDrawWps.Length;
				for (int j = 1; j < num2; j++)
				{
					Vector3 vector3 = p.nonLinearDrawWps[j];
					Gizmos.DrawLine(vector3, vector);
					vector = vector3;
				}
			}
			Gizmos.color = color;
			for (int k = 0; k < num; k++)
			{
				Gizmos.DrawSphere(p.wps[k], 0.075f);
			}
			if (p.lookAtPosition != null)
			{
				Vector3 value = p.lookAtPosition.Value;
				Gizmos.DrawLine(p.targetPosition, value);
				Gizmos.DrawWireSphere(value, 0.075f);
			}
		}

		// Token: 0x0400011C RID: 284
		private static CatmullRomDecoder _catmullRomDecoder;

		// Token: 0x0400011D RID: 285
		private static LinearDecoder _linearDecoder;

		// Token: 0x0400011E RID: 286
		private static CubicBezierDecoder _cubicBezierDecoder;

		// Token: 0x0400011F RID: 287
		public float[] wpLengths;

		// Token: 0x04000120 RID: 288
		[SerializeField]
		public Vector3[] wps;

		// Token: 0x04000121 RID: 289
		[SerializeField]
		internal PathType type;

		// Token: 0x04000122 RID: 290
		[SerializeField]
		internal int subdivisionsXSegment;

		// Token: 0x04000123 RID: 291
		[SerializeField]
		internal int subdivisions;

		// Token: 0x04000124 RID: 292
		[SerializeField]
		internal ControlPoint[] controlPoints;

		// Token: 0x04000125 RID: 293
		[SerializeField]
		public float length;

		// Token: 0x04000126 RID: 294
		[SerializeField]
		internal bool isFinalized;

		// Token: 0x04000127 RID: 295
		[SerializeField]
		internal float[] timesTable;

		// Token: 0x04000128 RID: 296
		[SerializeField]
		internal float[] lengthsTable;

		// Token: 0x04000129 RID: 297
		internal int linearWPIndex = -1;

		// Token: 0x0400012A RID: 298
		internal bool addedExtraStartWp;

		// Token: 0x0400012B RID: 299
		internal bool addedExtraEndWp;

		// Token: 0x0400012C RID: 300
		private Path _incrementalClone;

		// Token: 0x0400012D RID: 301
		private int _incrementalIndex;

		// Token: 0x0400012E RID: 302
		private ABSPathDecoder _decoder;

		// Token: 0x0400012F RID: 303
		private bool _changed;

		// Token: 0x04000130 RID: 304
		public Vector3[] nonLinearDrawWps;

		// Token: 0x04000131 RID: 305
		internal Vector3 targetPosition;

		// Token: 0x04000132 RID: 306
		internal Vector3? lookAtPosition;

		// Token: 0x04000133 RID: 307
		internal Color gizmoColor = new Color(1f, 1f, 1f, 0.7f);
	}
}
