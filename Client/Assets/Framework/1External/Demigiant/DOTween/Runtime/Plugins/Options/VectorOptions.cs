using System;

namespace DG.Tweening.Plugins.Options
{
	// Token: 0x0200003A RID: 58
	public struct VectorOptions : IPlugOptions
	{
		// Token: 0x06000236 RID: 566 RVA: 0x0000CFA2 File Offset: 0x0000B1A2
		public void Reset()
		{
			this.axisConstraint = AxisConstraint.None;
			this.snapping = false;
		}

		// Token: 0x04000102 RID: 258
		public AxisConstraint axisConstraint;

		// Token: 0x04000103 RID: 259
		public bool snapping;
	}
}
