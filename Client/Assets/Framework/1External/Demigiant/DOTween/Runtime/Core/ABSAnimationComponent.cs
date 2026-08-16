using System;
using UnityEngine;
using UnityEngine.Events;

namespace DG.Tweening.Core
{
	// Token: 0x0200000B RID: 11
	[AddComponentMenu("")]
	public abstract class ABSAnimationComponent : MonoBehaviour
	{
		// Token: 0x0600002C RID: 44
		public abstract void DOPlay();

		// Token: 0x0600002D RID: 45
		public abstract void DOPlayBackwards();

		// Token: 0x0600002E RID: 46
		public abstract void DOPlayForward();

		// Token: 0x0600002F RID: 47
		public abstract void DOPause();

		// Token: 0x06000030 RID: 48
		public abstract void DOTogglePause();

		// Token: 0x06000031 RID: 49
		public abstract void DORewind();

		/// <summary>
		/// Restarts the tween
		/// </summary>
		// Token: 0x06000032 RID: 50
		public abstract void DORestart();

		/// <summary>
		/// Restarts the tween
		/// </summary>
		/// <param name="fromHere">If TRUE, re-evaluates the tween's start and end values from its current position.
		/// Set it to TRUE when spawning the same DOTweenPath in different positions (like when using a pooling system)</param>
		// Token: 0x06000033 RID: 51
		public abstract void DORestart(bool fromHere);

		// Token: 0x06000034 RID: 52
		public abstract void DOComplete();

		// Token: 0x06000035 RID: 53
		public abstract void DOKill();

		// Token: 0x04000046 RID: 70
		public UpdateType updateType;

		// Token: 0x04000047 RID: 71
		public bool isSpeedBased;

		// Token: 0x04000048 RID: 72
		public bool hasOnStart;

		// Token: 0x04000049 RID: 73
		public bool hasOnPlay;

		// Token: 0x0400004A RID: 74
		public bool hasOnUpdate;

		// Token: 0x0400004B RID: 75
		public bool hasOnStepComplete;

		// Token: 0x0400004C RID: 76
		public bool hasOnComplete;

		// Token: 0x0400004D RID: 77
		public bool hasOnTweenCreated;

		// Token: 0x0400004E RID: 78
		public bool hasOnRewind;

		// Token: 0x0400004F RID: 79
		public UnityEvent onStart;

		// Token: 0x04000050 RID: 80
		public UnityEvent onPlay;

		// Token: 0x04000051 RID: 81
		public UnityEvent onUpdate;

		// Token: 0x04000052 RID: 82
		public UnityEvent onStepComplete;

		// Token: 0x04000053 RID: 83
		public UnityEvent onComplete;

		// Token: 0x04000054 RID: 84
		public UnityEvent onTweenCreated;

		// Token: 0x04000055 RID: 85
		public UnityEvent onRewind;

		// Token: 0x04000056 RID: 86
		[NonSerialized]
		public Tween tween;
	}
}
