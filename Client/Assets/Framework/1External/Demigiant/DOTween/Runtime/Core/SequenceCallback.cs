using System;

namespace DG.Tweening.Core
{
	// Token: 0x02000051 RID: 81
	internal class SequenceCallback : ABSSequentiable
	{
		// Token: 0x060002B6 RID: 694 RVA: 0x0000F25B File Offset: 0x0000D45B
		public SequenceCallback(float sequencedPosition, TweenCallback callback)
		{
			this.tweenType = TweenType.Callback;
			this.sequencedPosition = sequencedPosition;
			this.onStart = callback;
		}
	}
}
