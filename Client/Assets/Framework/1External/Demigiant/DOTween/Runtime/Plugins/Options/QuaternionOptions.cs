using System;
using UnityEngine;

namespace DG.Tweening.Plugins.Options
{
	// Token: 0x02000032 RID: 50
	public struct QuaternionOptions : IPlugOptions
	{
		// Token: 0x0600022E RID: 558 RVA: 0x0000CF17 File Offset: 0x0000B117
		public void Reset()
		{
			this.rotateMode = RotateMode.Fast;
			this.axisConstraint = AxisConstraint.None;
			this.up = Vector3.zero;
		}

		// Token: 0x040000F3 RID: 243
		public RotateMode rotateMode;

		// Token: 0x040000F4 RID: 244
		public AxisConstraint axisConstraint;

		// Token: 0x040000F5 RID: 245
		public Vector3 up;
	}
}
