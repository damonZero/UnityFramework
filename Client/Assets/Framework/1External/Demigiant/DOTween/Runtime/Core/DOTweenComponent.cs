using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DG.Tweening.Core
{
	// Token: 0x0200004C RID: 76
	[AddComponentMenu("")]
	public class DOTweenComponent : MonoBehaviour, IDOTweenInit
	{
		// Token: 0x06000292 RID: 658 RVA: 0x0000EBC8 File Offset: 0x0000CDC8
		private void Awake()
		{
			if (!(DOTween.instance == null))
			{
				if (Debugger.logPriority >= 1)
				{
					Debugger.LogWarning("Duplicate DOTweenComponent instance found in scene: destroying it", null);
				}
				Object.Destroy(base.gameObject);
				return;
			}
			DOTween.instance = this;
			this.inspectorUpdater = 0;
			this._unscaledTime = Time.realtimeSinceStartup;
			Type looseScriptType = Utils.GetLooseScriptType("DG.Tweening.DOTweenModuleUtils");
			if (looseScriptType == null)
			{
				Debugger.LogError("Couldn't load Modules system");
				return;
			}
			looseScriptType.GetMethod("Init", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
		}

		// Token: 0x06000293 RID: 659 RVA: 0x0000EC49 File Offset: 0x0000CE49
		private void Start()
		{
			if (DOTween.instance != this)
			{
				this._duplicateToDestroy = true;
				Object.Destroy(base.gameObject);
			}
		}

		// Token: 0x06000294 RID: 660 RVA: 0x0000EC6C File Offset: 0x0000CE6C
		private void Update()
		{
			this._unscaledDeltaTime = Time.realtimeSinceStartup - this._unscaledTime;
			if (DOTween.useSmoothDeltaTime && this._unscaledDeltaTime > DOTween.maxSmoothUnscaledTime)
			{
				this._unscaledDeltaTime = DOTween.maxSmoothUnscaledTime;
			}
			if (TweenManager.hasActiveDefaultTweens)
			{
				TweenManager.Update(UpdateType.Normal, (DOTween.useSmoothDeltaTime ? Time.smoothDeltaTime : Time.deltaTime) * DOTween.timeScale, this._unscaledDeltaTime * DOTween.timeScale);
			}
			this._unscaledTime = Time.realtimeSinceStartup;
			if (TweenManager.isUnityEditor)
			{
				this.inspectorUpdater++;
				if (DOTween.showUnityEditorReport && TweenManager.hasActiveTweens)
				{
					if (TweenManager.totActiveTweeners > DOTween.maxActiveTweenersReached)
					{
						DOTween.maxActiveTweenersReached = TweenManager.totActiveTweeners;
					}
					if (TweenManager.totActiveSequences > DOTween.maxActiveSequencesReached)
					{
						DOTween.maxActiveSequencesReached = TweenManager.totActiveSequences;
					}
				}
			}
		}

		// Token: 0x06000295 RID: 661 RVA: 0x0000ED36 File Offset: 0x0000CF36
		private void LateUpdate()
		{
			if (TweenManager.hasActiveLateTweens)
			{
				TweenManager.Update(UpdateType.Late, (DOTween.useSmoothDeltaTime ? Time.smoothDeltaTime : Time.deltaTime) * DOTween.timeScale, this._unscaledDeltaTime * DOTween.timeScale);
			}
		}

		// Token: 0x06000296 RID: 662 RVA: 0x0000ED6C File Offset: 0x0000CF6C
		private void FixedUpdate()
		{
			if (TweenManager.hasActiveFixedTweens && Time.timeScale > 0f)
			{
				TweenManager.Update(UpdateType.Fixed, (DOTween.useSmoothDeltaTime ? Time.smoothDeltaTime : Time.deltaTime) * DOTween.timeScale, (DOTween.useSmoothDeltaTime ? Time.smoothDeltaTime : Time.deltaTime) / Time.timeScale * DOTween.timeScale);
			}
		}

		// Token: 0x06000297 RID: 663 RVA: 0x0000EDCC File Offset: 0x0000CFCC
		private void OnDrawGizmos()
		{
			if (!DOTween.drawGizmos || !TweenManager.isUnityEditor)
			{
				return;
			}
			int count = DOTween.GizmosDelegates.Count;
			if (count == 0)
			{
				return;
			}
			for (int i = 0; i < count; i++)
			{
				DOTween.GizmosDelegates[i]();
			}
		}

		// Token: 0x06000298 RID: 664 RVA: 0x0000EE14 File Offset: 0x0000D014
		private void OnDestroy()
		{
			if (this._duplicateToDestroy)
			{
				return;
			}
			if (DOTween.showUnityEditorReport)
			{
				Debugger.LogReport("Max overall simultaneous active Tweeners/Sequences: " + DOTween.maxActiveTweenersReached.ToString() + "/" + DOTween.maxActiveSequencesReached.ToString());
			}
			if (DOTween.useSafeMode)
			{
				int totErrors = DOTween.safeModeReport.GetTotErrors();
				if (totErrors > 0)
				{
					string text = string.Format("DOTween's safe mode captured {0} errors. This is usually ok (it's what safe mode is there for) but if your game is encountering issues you should set Log Behaviour to Default in DOTween Utility Panel in order to get detailed warnings when an error is captured (consider that these errors are always on the user side).", totErrors);
					if (DOTween.safeModeReport.totMissingTargetOrFieldErrors > 0)
					{
						text = text + "\n- " + DOTween.safeModeReport.totMissingTargetOrFieldErrors.ToString() + " missing target or field errors";
					}
					if (DOTween.safeModeReport.totStartupErrors > 0)
					{
						text = text + "\n- " + DOTween.safeModeReport.totStartupErrors.ToString() + " startup errors";
					}
					if (DOTween.safeModeReport.totCallbackErrors > 0)
					{
						text = text + "\n- " + DOTween.safeModeReport.totCallbackErrors.ToString() + " errors inside callbacks (these might be important)";
					}
					if (DOTween.safeModeReport.totUnsetErrors > 0)
					{
						text = text + "\n- " + DOTween.safeModeReport.totUnsetErrors.ToString() + " undetermined errors (these might be important)";
					}
					Debugger.LogSafeModeReport(text);
				}
			}
			if (DOTween.instance == this)
			{
				DOTween.instance = null;
			}
			DOTween.Clear(true, this._isQuitting);
		}

		// Token: 0x06000299 RID: 665 RVA: 0x0000EF6B File Offset: 0x0000D16B
		public void OnApplicationPause(bool pauseStatus)
		{
			if (pauseStatus)
			{
				this._paused = true;
				this._pausedTime = Time.realtimeSinceStartup;
				return;
			}
			if (this._paused)
			{
				this._paused = false;
				this._unscaledTime += Time.realtimeSinceStartup - this._pausedTime;
			}
		}

		// Token: 0x0600029A RID: 666 RVA: 0x0000EFAB File Offset: 0x0000D1AB
		private void OnApplicationQuit()
		{
			this._isQuitting = true;
		}

		// Token: 0x0600029B RID: 667 RVA: 0x0000EFB4 File Offset: 0x0000D1B4
		public IDOTweenInit SetCapacity(int tweenersCapacity, int sequencesCapacity)
		{
			TweenManager.SetCapacities(tweenersCapacity, sequencesCapacity);
			return this;
		}

		// Token: 0x0600029C RID: 668 RVA: 0x0000EFBE File Offset: 0x0000D1BE
		internal IEnumerator WaitForCompletion(Tween t)
		{
			while (t.active && !t.isComplete)
			{
				yield return null;
			}
			yield break;
		}

		// Token: 0x0600029D RID: 669 RVA: 0x0000EFCD File Offset: 0x0000D1CD
		internal IEnumerator WaitForRewind(Tween t)
		{
			while (t.active && (!t.playedOnce || t.position * (float)(t.completedLoops + 1) > 0f))
			{
				yield return null;
			}
			yield break;
		}

		// Token: 0x0600029E RID: 670 RVA: 0x0000EFDC File Offset: 0x0000D1DC
		internal IEnumerator WaitForKill(Tween t)
		{
			while (t.active)
			{
				yield return null;
			}
			yield break;
		}

		// Token: 0x0600029F RID: 671 RVA: 0x0000EFEB File Offset: 0x0000D1EB
		internal IEnumerator WaitForElapsedLoops(Tween t, int elapsedLoops)
		{
			while (t.active && t.completedLoops < elapsedLoops)
			{
				yield return null;
			}
			yield break;
		}

		// Token: 0x060002A0 RID: 672 RVA: 0x0000F001 File Offset: 0x0000D201
		internal IEnumerator WaitForPosition(Tween t, float position)
		{
			while (t.active && t.position * (float)(t.completedLoops + 1) < position)
			{
				yield return null;
			}
			yield break;
		}

		// Token: 0x060002A1 RID: 673 RVA: 0x0000F017 File Offset: 0x0000D217
		internal IEnumerator WaitForStart(Tween t)
		{
			while (t.active && !t.playedOnce)
			{
				yield return null;
			}
			yield break;
		}

		// Token: 0x060002A2 RID: 674 RVA: 0x0000F026 File Offset: 0x0000D226
		internal static void Create()
		{
			if (DOTween.instance != null)
			{
				return;
			}
			GameObject gameObject = new GameObject("[DOTween]");
			Object.DontDestroyOnLoad(gameObject);
			DOTween.instance = gameObject.AddComponent<DOTweenComponent>();
		}

		// Token: 0x060002A3 RID: 675 RVA: 0x0000F050 File Offset: 0x0000D250
		internal static void DestroyInstance()
		{
			if (DOTween.instance != null)
			{
				Object.Destroy(DOTween.instance.gameObject);
			}
			DOTween.instance = null;
		}

		// Token: 0x0400013B RID: 315
		public int inspectorUpdater;

		// Token: 0x0400013C RID: 316
		private float _unscaledTime;

		// Token: 0x0400013D RID: 317
		private float _unscaledDeltaTime;

		// Token: 0x0400013E RID: 318
		private bool _paused;

		// Token: 0x0400013F RID: 319
		private float _pausedTime;

		// Token: 0x04000140 RID: 320
		private bool _isQuitting;

		// Token: 0x04000141 RID: 321
		private bool _duplicateToDestroy;
	}
}
