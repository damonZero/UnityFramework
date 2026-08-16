using System;

namespace DG.Tweening
{
	/// <summary>
	/// Spiral tween mode
	/// </summary>
	// Token: 0x02000008 RID: 8
	public enum SpiralMode
	{
		/// <summary>The spiral motion will expand outwards for the whole the tween</summary>
		// Token: 0x0400003C RID: 60
		Expand,
		/// <summary>The spiral motion will expand outwards for half the tween and then will spiral back to the starting position</summary>
		// Token: 0x0400003D RID: 61
		ExpandThenContract
	}
}
