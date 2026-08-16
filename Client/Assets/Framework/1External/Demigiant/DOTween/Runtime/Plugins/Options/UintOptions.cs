using System;

namespace DG.Tweening.Plugins.Options
{
	// Token: 0x02000033 RID: 51
	public struct UintOptions : IPlugOptions
	{
		// Token: 0x0600022F RID: 559 RVA: 0x0000CF32 File Offset: 0x0000B132
		public void Reset()
		{
			this.isNegativeChangeValue = false;
		}

		// Token: 0x040000F6 RID: 246
		public bool isNegativeChangeValue;
	}
}
