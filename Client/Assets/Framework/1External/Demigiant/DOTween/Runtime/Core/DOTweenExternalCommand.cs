using System;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace DG.Tweening.Core
{
	// Token: 0x0200004F RID: 79
	public static class DOTweenExternalCommand
	{
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x060002A9 RID: 681 RVA: 0x0000F114 File Offset: 0x0000D314
		// (remove) Token: 0x060002AA RID: 682 RVA: 0x0000F148 File Offset: 0x0000D348
		public static event Action<PathOptions, Tween, Quaternion, Transform> SetOrientationOnPath;

		// Token: 0x060002AB RID: 683 RVA: 0x0000F17B File Offset: 0x0000D37B
		internal static void Dispatch_SetOrientationOnPath(PathOptions options, Tween t, Quaternion newRot, Transform trans)
		{
			if (DOTweenExternalCommand.SetOrientationOnPath != null)
			{
				DOTweenExternalCommand.SetOrientationOnPath(options, t, newRot, trans);
			}
		}
	}
}
