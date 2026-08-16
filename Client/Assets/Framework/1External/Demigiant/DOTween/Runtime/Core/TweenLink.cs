using System;
using UnityEngine;

namespace DG.Tweening.Core
{
	// Token: 0x02000052 RID: 82
	public class TweenLink
	{
		// Token: 0x060002B7 RID: 695 RVA: 0x0000F278 File Offset: 0x0000D478
		public TweenLink(GameObject target, LinkBehaviour behaviour)
		{
			this.target = target;
			this.behaviour = behaviour;
			this.lastSeenActive = target.activeInHierarchy;
		}

		// Token: 0x04000162 RID: 354
		public readonly GameObject target;

		// Token: 0x04000163 RID: 355
		public readonly LinkBehaviour behaviour;

		// Token: 0x04000164 RID: 356
		public bool lastSeenActive;
	}
}
