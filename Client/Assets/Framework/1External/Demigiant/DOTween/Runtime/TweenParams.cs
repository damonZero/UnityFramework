using System;
using DG.Tweening.Core.Easing;
using UnityEngine;

namespace DG.Tweening
{
	// Token: 0x02000016 RID: 22
	public class TweenParams
	{
		// Token: 0x06000106 RID: 262 RVA: 0x0000667F File Offset: 0x0000487F
		public TweenParams()
		{
			this.Clear();
		}

		// Token: 0x06000107 RID: 263 RVA: 0x00006690 File Offset: 0x00004890
		public TweenParams Clear()
		{
			this.id = (this.target = null);
			this.updateType = DOTween.defaultUpdateType;
			this.isIndependentUpdate = DOTween.defaultTimeScaleIndependent;
			this.onStart = (this.onPlay = (this.onRewind = (this.onUpdate = (this.onStepComplete = (this.onComplete = (this.onKill = null))))));
			this.onWaypointChange = null;
			this.isRecyclable = DOTween.defaultRecyclable;
			this.isSpeedBased = false;
			this.autoKill = DOTween.defaultAutoKill;
			this.loops = 1;
			this.loopType = DOTween.defaultLoopType;
			this.delay = 0f;
			this.isRelative = false;
			this.easeType = Ease.Unset;
			this.customEase = null;
			this.easeOvershootOrAmplitude = DOTween.defaultEaseOvershootOrAmplitude;
			this.easePeriod = DOTween.defaultEasePeriod;
			return this;
		}

		// Token: 0x06000108 RID: 264 RVA: 0x0000676D File Offset: 0x0000496D
		public TweenParams SetAutoKill(bool autoKillOnCompletion = true)
		{
			this.autoKill = autoKillOnCompletion;
			return this;
		}

		// Token: 0x06000109 RID: 265 RVA: 0x00006777 File Offset: 0x00004977
		public TweenParams SetId(object id)
		{
			this.id = id;
			return this;
		}

		// Token: 0x0600010A RID: 266 RVA: 0x00006781 File Offset: 0x00004981
		public TweenParams SetTarget(object target)
		{
			this.target = target;
			return this;
		}

		// Token: 0x0600010B RID: 267 RVA: 0x0000678B File Offset: 0x0000498B
		public TweenParams SetLoops(int loops, LoopType? loopType = null)
		{
			if (loops < -1)
			{
				loops = -1;
			}
			else if (loops == 0)
			{
				loops = 1;
			}
			this.loops = loops;
			if (loopType != null)
			{
				this.loopType = loopType.Value;
			}
			return this;
		}

		// Token: 0x0600010C RID: 268 RVA: 0x000067BC File Offset: 0x000049BC
		public TweenParams SetEase(Ease ease, float? overshootOrAmplitude = null, float? period = null)
		{
			this.easeType = ease;
			this.easeOvershootOrAmplitude = ((overshootOrAmplitude != null) ? overshootOrAmplitude.Value : DOTween.defaultEaseOvershootOrAmplitude);
			this.easePeriod = ((period != null) ? period.Value : DOTween.defaultEasePeriod);
			this.customEase = null;
			return this;
		}

		// Token: 0x0600010D RID: 269 RVA: 0x00006814 File Offset: 0x00004A14
		public TweenParams SetEase(AnimationCurve animCurve)
		{
			this.easeType = Ease.INTERNAL_Custom;
			this.customEase = new EaseFunction(new EaseCurve(animCurve).Evaluate);
			return this;
		}

		// Token: 0x0600010E RID: 270 RVA: 0x00006836 File Offset: 0x00004A36
		public TweenParams SetEase(EaseFunction customEase)
		{
			this.easeType = Ease.INTERNAL_Custom;
			this.customEase = customEase;
			return this;
		}

		// Token: 0x0600010F RID: 271 RVA: 0x00006848 File Offset: 0x00004A48
		public TweenParams SetRecyclable(bool recyclable = true)
		{
			this.isRecyclable = recyclable;
			return this;
		}

		// Token: 0x06000110 RID: 272 RVA: 0x00006852 File Offset: 0x00004A52
		public TweenParams SetUpdate(bool isIndependentUpdate)
		{
			this.updateType = DOTween.defaultUpdateType;
			this.isIndependentUpdate = isIndependentUpdate;
			return this;
		}

		// Token: 0x06000111 RID: 273 RVA: 0x00006867 File Offset: 0x00004A67
		public TweenParams SetUpdate(UpdateType updateType, bool isIndependentUpdate = false)
		{
			this.updateType = updateType;
			this.isIndependentUpdate = isIndependentUpdate;
			return this;
		}

		// Token: 0x06000112 RID: 274 RVA: 0x00006878 File Offset: 0x00004A78
		public TweenParams OnStart(TweenCallback action)
		{
			this.onStart = action;
			return this;
		}

		// Token: 0x06000113 RID: 275 RVA: 0x00006882 File Offset: 0x00004A82
		public TweenParams OnPlay(TweenCallback action)
		{
			this.onPlay = action;
			return this;
		}

		// Token: 0x06000114 RID: 276 RVA: 0x0000688C File Offset: 0x00004A8C
		public TweenParams OnRewind(TweenCallback action)
		{
			this.onRewind = action;
			return this;
		}

		// Token: 0x06000115 RID: 277 RVA: 0x00006896 File Offset: 0x00004A96
		public TweenParams OnUpdate(TweenCallback action)
		{
			this.onUpdate = action;
			return this;
		}

		// Token: 0x06000116 RID: 278 RVA: 0x000068A0 File Offset: 0x00004AA0
		public TweenParams OnStepComplete(TweenCallback action)
		{
			this.onStepComplete = action;
			return this;
		}

		// Token: 0x06000117 RID: 279 RVA: 0x000068AA File Offset: 0x00004AAA
		public TweenParams OnComplete(TweenCallback action)
		{
			this.onComplete = action;
			return this;
		}

		// Token: 0x06000118 RID: 280 RVA: 0x000068B4 File Offset: 0x00004AB4
		public TweenParams OnKill(TweenCallback action)
		{
			this.onKill = action;
			return this;
		}

		// Token: 0x06000119 RID: 281 RVA: 0x000068BE File Offset: 0x00004ABE
		public TweenParams OnWaypointChange(TweenCallback<int> action)
		{
			this.onWaypointChange = action;
			return this;
		}

		// Token: 0x0600011A RID: 282 RVA: 0x000068C8 File Offset: 0x00004AC8
		public TweenParams SetDelay(float delay)
		{
			this.delay = delay;
			return this;
		}

		// Token: 0x0600011B RID: 283 RVA: 0x000068D2 File Offset: 0x00004AD2
		public TweenParams SetRelative(bool isRelative = true)
		{
			this.isRelative = isRelative;
			return this;
		}

		// Token: 0x0600011C RID: 284 RVA: 0x000068DC File Offset: 0x00004ADC
		public TweenParams SetSpeedBased(bool isSpeedBased = true)
		{
			this.isSpeedBased = isSpeedBased;
			return this;
		}

		// Token: 0x0400007A RID: 122
		public static readonly TweenParams Params = new TweenParams();

		// Token: 0x0400007B RID: 123
		internal object id;

		// Token: 0x0400007C RID: 124
		internal object target;

		// Token: 0x0400007D RID: 125
		internal UpdateType updateType;

		// Token: 0x0400007E RID: 126
		internal bool isIndependentUpdate;

		// Token: 0x0400007F RID: 127
		internal TweenCallback onStart;

		// Token: 0x04000080 RID: 128
		internal TweenCallback onPlay;

		// Token: 0x04000081 RID: 129
		internal TweenCallback onRewind;

		// Token: 0x04000082 RID: 130
		internal TweenCallback onUpdate;

		// Token: 0x04000083 RID: 131
		internal TweenCallback onStepComplete;

		// Token: 0x04000084 RID: 132
		internal TweenCallback onComplete;

		// Token: 0x04000085 RID: 133
		internal TweenCallback onKill;

		// Token: 0x04000086 RID: 134
		internal TweenCallback<int> onWaypointChange;

		// Token: 0x04000087 RID: 135
		internal bool isRecyclable;

		// Token: 0x04000088 RID: 136
		internal bool isSpeedBased;

		// Token: 0x04000089 RID: 137
		internal bool autoKill;

		// Token: 0x0400008A RID: 138
		internal int loops;

		// Token: 0x0400008B RID: 139
		internal LoopType loopType;

		// Token: 0x0400008C RID: 140
		internal float delay;

		// Token: 0x0400008D RID: 141
		internal bool isRelative;

		// Token: 0x0400008E RID: 142
		internal Ease easeType;

		// Token: 0x0400008F RID: 143
		internal EaseFunction customEase;

		// Token: 0x04000090 RID: 144
		internal float easeOvershootOrAmplitude;

		// Token: 0x04000091 RID: 145
		internal float easePeriod;
	}
}
