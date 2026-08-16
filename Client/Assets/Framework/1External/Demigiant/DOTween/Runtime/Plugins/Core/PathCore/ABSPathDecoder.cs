using System;
using UnityEngine;

namespace DG.Tweening.Plugins.Core.PathCore
{
	// Token: 0x02000043 RID: 67
	internal abstract class ABSPathDecoder
	{
		// Token: 0x06000254 RID: 596
		internal abstract void FinalizePath(Path p, Vector3[] wps, bool isClosedPath);

		// Token: 0x06000255 RID: 597
		internal abstract Vector3 GetPoint(float perc, Vector3[] wps, Path p, ControlPoint[] controlPoints);
	}
}
