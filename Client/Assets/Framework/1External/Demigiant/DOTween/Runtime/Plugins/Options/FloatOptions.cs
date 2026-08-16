using System;

namespace DG.Tweening.Plugins.Options
{
	// Token: 0x02000037 RID: 55
	public struct FloatOptions : IPlugOptions
	{
		// Token: 0x06000233 RID: 563 RVA: 0x0000CF5B File Offset: 0x0000B15B
		public void Reset()
		{
			this.snapping = false;
		}

		// Token: 0x040000FB RID: 251
		public bool snapping;
	}
}
