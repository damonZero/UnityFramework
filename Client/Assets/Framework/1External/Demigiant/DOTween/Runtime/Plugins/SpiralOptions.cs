using System;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace DG.Tweening.Plugins
{
	// Token: 0x02000009 RID: 9
	public struct SpiralOptions : IPlugOptions
	{
		// Token: 0x06000020 RID: 32 RVA: 0x00002BFC File Offset: 0x00000DFC
		public void Reset()
		{
			this.depth = (this.frequency = (this.speed = 0f));
			this.mode = SpiralMode.Expand;
			this.snapping = false;
		}

		// Token: 0x0400003E RID: 62
		public float depth;

		// Token: 0x0400003F RID: 63
		public float frequency;

		// Token: 0x04000040 RID: 64
		public float speed;

		// Token: 0x04000041 RID: 65
		public SpiralMode mode;

		// Token: 0x04000042 RID: 66
		public bool snapping;

		// Token: 0x04000043 RID: 67
		internal float unit;

		// Token: 0x04000044 RID: 68
		internal Quaternion axisQ;
	}
}
