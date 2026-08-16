using System;

namespace DG.Tweening.Plugins.Options
{
	// Token: 0x02000038 RID: 56
	public struct RectOptions : IPlugOptions
	{
		// Token: 0x06000234 RID: 564 RVA: 0x0000CF64 File Offset: 0x0000B164
		public void Reset()
		{
			this.snapping = false;
		}

		// Token: 0x040000FC RID: 252
		public bool snapping;
	}
}
