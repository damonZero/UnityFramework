using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEditor;

namespace DG.DOTweenEditor
{
	// Token: 0x02000004 RID: 4
	public static class DOTweenEditorPreview
	{
		/// <summary>
		/// Starts the update loop of tween in the editor. Has no effect during playMode.
		/// </summary>
		/// <param name="onPreviewUpdated">Eventual callback to call after every update</param>
		// Token: 0x06000012 RID: 18 RVA: 0x00002600 File Offset: 0x00000800
		public static void Start(Action onPreviewUpdated = null)
		{
			if (DOTweenEditorPreview._isPreviewing || EditorApplication.isPlayingOrWillChangePlaymode)
			{
				return;
			}
			DOTweenEditorPreview._isPreviewing = true;
			DOTweenEditorPreview._onPreviewUpdated = onPreviewUpdated;
			DOTweenEditorPreview._previewTime = EditorApplication.timeSinceStartup;
			EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.update, new EditorApplication.CallbackFunction(DOTweenEditorPreview.PreviewUpdate));
		}

		/// <summary>
		/// Stops the update loop and clears the onPreviewUpdated callback.
		/// </summary>
		/// <param name="resetTweenTargets">If TRUE also resets the tweened objects to their original state</param>
		// Token: 0x06000013 RID: 19 RVA: 0x00002654 File Offset: 0x00000854
		public static void Stop(bool resetTweenTargets = false)
		{
			DOTweenEditorPreview._isPreviewing = false;
			EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.update, new EditorApplication.CallbackFunction(DOTweenEditorPreview.PreviewUpdate));
			DOTweenEditorPreview._onPreviewUpdated = null;
			if (resetTweenTargets)
			{
				foreach (Tween tween in DOTweenEditorPreview._Tweens)
				{
					try
					{
						if (tween.isFrom)
						{
							tween.Complete();
						}
						else
						{
							tween.Rewind(true);
						}
					}
					catch
					{
					}
				}
			}
			DOTweenEditorPreview.ValidateTweens();
		}

		/// <summary>
		/// Readies the tween for editor preview by setting its UpdateType to Manual plus eventual extra settings.
		/// </summary>
		/// <param name="t">The tween to ready</param>
		/// <param name="clearCallbacks">If TRUE (recommended) removes all callbacks (OnComplete/Rewind/etc)</param>
		/// <param name="preventAutoKill">If TRUE prevents the tween from being auto-killed at completion</param>
		/// <param name="andPlay">If TRUE starts playing the tween immediately</param>
		// Token: 0x06000014 RID: 20 RVA: 0x000026FC File Offset: 0x000008FC
		public static void PrepareTweenForPreview(Tween t, bool clearCallbacks = true, bool preventAutoKill = true, bool andPlay = true)
		{
			DOTweenEditorPreview._Tweens.Add(t);
			t.SetUpdate(UpdateType.Manual);
			if (preventAutoKill)
			{
				t.SetAutoKill(false);
			}
			if (clearCallbacks)
			{
				t.OnComplete(null).OnStart(null).OnPlay(null).OnPause(null).OnUpdate(null).OnWaypointChange(null).OnStepComplete(null).OnRewind(null).OnKill(null);
			}
			if (andPlay)
			{
				t.Play<Tween>();
			}
		}

		// Token: 0x06000015 RID: 21 RVA: 0x0000276C File Offset: 0x0000096C
		private static void PreviewUpdate()
		{
			double previewTime = DOTweenEditorPreview._previewTime;
			DOTweenEditorPreview._previewTime = EditorApplication.timeSinceStartup;
			float num = (float)(DOTweenEditorPreview._previewTime - previewTime);
			DOTween.ManualUpdate(num, num);
			if (DOTweenEditorPreview._onPreviewUpdated != null)
			{
				DOTweenEditorPreview._onPreviewUpdated();
			}
		}

		// Token: 0x06000016 RID: 22 RVA: 0x000027A8 File Offset: 0x000009A8
		private static void ValidateTweens()
		{
			for (int i = DOTweenEditorPreview._Tweens.Count - 1; i > -1; i--)
			{
				if (DOTweenEditorPreview._Tweens[i] == null || !DOTweenEditorPreview._Tweens[i].active)
				{
					DOTweenEditorPreview._Tweens.RemoveAt(i);
				}
			}
		}

		// Token: 0x0400000E RID: 14
		private static bool _isPreviewing;

		// Token: 0x0400000F RID: 15
		private static double _previewTime;

		// Token: 0x04000010 RID: 16
		private static Action _onPreviewUpdated;

		// Token: 0x04000011 RID: 17
		private static readonly List<Tween> _Tweens = new List<Tween>();
	}
}
