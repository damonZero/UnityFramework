using System;

namespace DG.Tweening.Plugins.Options
{
	// Token: 0x02000034 RID: 52
	public struct Vector3ArrayOptions : IPlugOptions
	{
		// Token: 0x06000230 RID: 560 RVA: 0x0000CF3B File Offset: 0x0000B13B
		public void Reset()
		{
			this.axisConstraint = AxisConstraint.None;
			this.snapping = false;
			this.durations = null;
		}

		// Token: 0x040000F7 RID: 247
		public AxisConstraint axisConstraint;

		// Token: 0x040000F8 RID: 248
		public bool snapping;

		// Token: 0x040000F9 RID: 249
		internal float[] durations;
	}
}
