using System;

namespace DG.Tweening.Core
{
	// Token: 0x02000048 RID: 72
	public abstract class ABSSequentiable
	{
		// Token: 0x04000135 RID: 309
		public TweenType tweenType;

		// Token: 0x04000136 RID: 310
		internal float sequencedPosition;

		// Token: 0x04000137 RID: 311
		internal float sequencedEndPosition;

		// Token: 0x04000138 RID: 312
		internal TweenCallback onStart;
	}
}
