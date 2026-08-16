using System;

namespace DG.Tweening.Plugins.Options
{
	// Token: 0x02000036 RID: 54
	public struct ColorOptions : IPlugOptions
	{
		// Token: 0x06000232 RID: 562 RVA: 0x0000CF52 File Offset: 0x0000B152
		public void Reset()
		{
			this.alphaOnly = false;
		}

		// Token: 0x040000FA RID: 250
		public bool alphaOnly;
	}
}
