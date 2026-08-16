using System;

namespace DG.Tweening
{
	// Token: 0x0200000D RID: 13
	public enum LinkBehaviour
	{
		// Token: 0x04000053 RID: 83
		PauseOnDisable,
		// Token: 0x04000054 RID: 84
		PauseOnDisablePlayOnEnable,
		// Token: 0x04000055 RID: 85
		PauseOnDisableRestartOnEnable,
		// Token: 0x04000056 RID: 86
		PlayOnEnable,
		// Token: 0x04000057 RID: 87
		RestartOnEnable,
		// Token: 0x04000058 RID: 88
		KillOnDisable,
		// Token: 0x04000059 RID: 89
		KillOnDestroy,
		// Token: 0x0400005A RID: 90
		CompleteOnDisable,
		// Token: 0x0400005B RID: 91
		CompleteAndKillOnDisable,
		// Token: 0x0400005C RID: 92
		RewindOnDisable,
		// Token: 0x0400005D RID: 93
		RewindAndKillOnDisable
	}
}
