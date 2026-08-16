using System;
using UnityEngine;

namespace DG.Tweening.Plugins.Core.PathCore
{
	// Token: 0x02000045 RID: 69
	internal class LinearDecoder : ABSPathDecoder
	{
		// Token: 0x0600025D RID: 605 RVA: 0x0000DE31 File Offset: 0x0000C031
		internal override void FinalizePath(Path p, Vector3[] wps, bool isClosedPath)
		{
			p.controlPoints = null;
			p.subdivisions = wps.Length * p.subdivisionsXSegment;
			this.SetTimeToLengthTables(p, p.subdivisions);
		}

		// Token: 0x0600025E RID: 606 RVA: 0x0000DE58 File Offset: 0x0000C058
		internal override Vector3 GetPoint(float perc, Vector3[] wps, Path p, ControlPoint[] controlPoints)
		{
			if (perc <= 0f)
			{
				p.linearWPIndex = 1;
				return wps[0];
			}
			int num = 0;
			int num2 = 0;
			int num3 = p.timesTable.Length;
			for (int i = 1; i < num3; i++)
			{
				if (p.timesTable[i] >= perc)
				{
					num = i - 1;
					num2 = i;
					break;
				}
			}
			float num4 = p.timesTable[num];
			float num5 = perc - num4;
			float num6 = p.length * num5;
			Vector3 vector = wps[num];
			Vector3 vector2 = wps[num2];
			p.linearWPIndex = num2;
			return vector + Vector3.ClampMagnitude(vector2 - vector, num6);
		}

		// Token: 0x0600025F RID: 607 RVA: 0x0000DEF8 File Offset: 0x0000C0F8
		internal void SetTimeToLengthTables(Path p, int subdivisions)
		{
			float num = 0f;
			int num2 = p.wps.Length;
			float[] array = new float[num2];
			Vector3 vector = p.wps[0];
			for (int i = 0; i < num2; i++)
			{
				Vector3 vector2 = p.wps[i];
				float num3 = Vector3.Distance(vector2, vector);
				num += num3;
				vector = vector2;
				array[i] = num3;
			}
			float[] array2 = new float[num2];
			float num4 = 0f;
			for (int j = 1; j < num2; j++)
			{
				num4 += array[j];
				array2[j] = num4 / num;
			}
			p.length = num;
			p.wpLengths = array;
			p.timesTable = array2;
		}

		// Token: 0x06000260 RID: 608 RVA: 0x0000890C File Offset: 0x00006B0C
		internal void SetWaypointsLengths(Path p, int subdivisions)
		{
		}
	}
}
