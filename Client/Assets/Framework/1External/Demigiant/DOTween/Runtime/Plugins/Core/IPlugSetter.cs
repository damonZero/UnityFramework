using System;
using DG.Tweening.Core;

namespace DG.Tweening.Plugins.Core
{
	// Token: 0x0200003D RID: 61
	public interface IPlugSetter<T1, out T2, TPlugin, out TPlugOptions>
	{
		// Token: 0x0600023B RID: 571
		DOGetter<T1> Getter();

		// Token: 0x0600023C RID: 572
		DOSetter<T1> Setter();

		// Token: 0x0600023D RID: 573
		T2 EndValue();

		// Token: 0x0600023E RID: 574
		TPlugOptions GetOptions();
	}
}
