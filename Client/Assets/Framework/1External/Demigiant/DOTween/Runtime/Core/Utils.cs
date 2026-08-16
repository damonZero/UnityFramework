using System;
using System.Reflection;
using UnityEngine;

namespace DG.Tweening.Core
{
	// Token: 0x02000054 RID: 84
	public static class Utils
	{
		// Token: 0x060002E2 RID: 738 RVA: 0x00010F94 File Offset: 0x0000F194
		public static Vector3 Vector3FromAngle(float degrees, float magnitude)
		{
			float num = degrees * 0.0174532924f;
			return new Vector3(magnitude * Mathf.Cos(num), magnitude * Mathf.Sin(num), 0f);
		}

		// Token: 0x060002E3 RID: 739 RVA: 0x00010FC4 File Offset: 0x0000F1C4
		public static float Angle2D(Vector3 from, Vector3 to)
		{
			Vector2 right = Vector2.right;
			to -= from;
			float num = Vector2.Angle(right, to);
			if (Vector3.Cross(right, to).z > 0f)
			{
				num = 360f - num;
			}
			return num * -1f;
		}

		// Token: 0x060002E4 RID: 740 RVA: 0x00011014 File Offset: 0x0000F214
		public static Vector3 RotateAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
		{
			return rotation * (point - pivot) + pivot;
		}

		// Token: 0x060002E5 RID: 741 RVA: 0x00011029 File Offset: 0x0000F229
		public static bool Vector3AreApproximatelyEqual(Vector3 a, Vector3 b)
		{
			return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z);
		}

		// Token: 0x060002E6 RID: 742 RVA: 0x00011064 File Offset: 0x0000F264
		public static Type GetLooseScriptType(string typeName)
		{
			for (int i = 0; i < Utils._defAssembliesToQuery.Length; i++)
			{
				Type type = Type.GetType(string.Format("{0}, {1}", typeName, Utils._defAssembliesToQuery[i]));
				if (type != null)
				{
					return type;
				}
			}
			if (Utils._loadedAssemblies == null)
			{
				Utils._loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
			}
			for (int j = 0; j < Utils._loadedAssemblies.Length; j++)
			{
				Type type2 = Type.GetType(string.Format("{0}, {1}", typeName, Utils._loadedAssemblies[j].GetName()));
				if (type2 != null)
				{
					return type2;
				}
			}
			return null;
		}

		// Token: 0x0400018B RID: 395
		private static Assembly[] _loadedAssemblies;

		// Token: 0x0400018C RID: 396
		private static readonly string[] _defAssembliesToQuery = new string[]
		{
			"DOTween.Modules",
			"Assembly-CSharp",
			"Assembly-CSharp-firstpass"
		};
	}
}
