using System;
using System.Text;
using DG.Tweening;
using DG.Tweening.Core;
using UnityEditor;
using UnityEngine;

namespace DG.DOTweenEditor.UI
{
	// Token: 0x0200000C RID: 12
	[CustomEditor(typeof(DOTweenComponent))]
	public class DOTweenComponentInspector : Editor
	{
		// Token: 0x06000053 RID: 83 RVA: 0x00003D98 File Offset: 0x00001F98
		private void OnEnable()
		{
			this._isRuntime = EditorApplication.isPlaying;
			this.ConnectToSource(true);
			this._strb.Length = 0;
			this._strb.Append("DOTween v").Append(DOTween.Version);
			if (TweenManager.isDebugBuild)
			{
				this._strb.Append(" [Debug build]");
			}
			else
			{
				this._strb.Append(" [Release build]");
			}
			if (EditorUtils.hasPro)
			{
				this._strb.Append("\nDOTweenPro v").Append(EditorUtils.proVersion);
			}
			else
			{
				this._strb.Append("\nDOTweenPro not installed");
			}
			this._title = this._strb.ToString();
			this._playingTweensHex = (EditorGUIUtility.isProSkin ? "<color=#00c514>" : "<color=#005408>");
			this._pausedTweensHex = (EditorGUIUtility.isProSkin ? "<color=#ff832a>" : "<color=#873600>");
		}

		// Token: 0x06000054 RID: 84 RVA: 0x00003E84 File Offset: 0x00002084
		public override void OnInspectorGUI()
		{
			this._isRuntime = EditorApplication.isPlaying;
			this.ConnectToSource(false);
			EditorGUIUtils.SetGUIStyles(null);
			GUILayout.Space(4f);
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUI.DrawTexture(GUILayoutUtility.GetRect(0f, 93f, 18f, 18f), this._headerImg, (ScaleMode) 2, true);
			GUILayout.Label(this._isRuntime ? "RUNTIME MODE" : "EDITOR MODE", new GUILayoutOption[0]);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			int totActiveTweens = TweenManager.totActiveTweens;
			int num = TweenManager.TotalPlayingTweens();
			int value = totActiveTweens - num;
			int totActiveDefaultTweens = TweenManager.totActiveDefaultTweens;
			int totActiveLateTweens = TweenManager.totActiveLateTweens;
			int totActiveFixedTweens = TweenManager.totActiveFixedTweens;
			int totActiveManualTweens = TweenManager.totActiveManualTweens;
			GUILayout.Label(this._title, TweenManager.isDebugBuild ? EditorGUIUtils.redLabelStyle : EditorGUIUtils.boldLabelStyle, new GUILayoutOption[0]);
			if (!this._isRuntime)
			{
				GUI.backgroundColor = new Color(0f, 0.31f, 0.48f);
				GUI.contentColor = Color.white;
				GUILayout.Label("This component is <b>added automatically</b> by DOTween at runtime.\nAdding it yourself is <b>not recommended</b> unless you really know what you're doing: you'll have to be sure it's <b>never destroyed</b> and that it's present <b>in every scene</b>.", EditorGUIUtils.infoboxStyle, new GUILayoutOption[0]);
				GUI.backgroundColor = (GUI.contentColor = (GUI.contentColor = Color.white));
			}
			GUILayout.Space(6f);
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			if (GUILayout.Button("Documentation", new GUILayoutOption[0]))
			{
				Application.OpenURL("http://dotween.demigiant.com/documentation.php");
			}
			if (GUILayout.Button("Check Updates", new GUILayoutOption[0]))
			{
				Application.OpenURL("http://dotween.demigiant.com/download.php?v=" + DOTween.Version);
			}
			GUILayout.EndHorizontal();
			if (this._isRuntime)
			{
				GUILayout.BeginHorizontal(new GUILayoutOption[0]);
				if (GUILayout.Button(this._settings.showPlayingTweens ? "Hide Playing Tweens" : "Show Playing Tweens", new GUILayoutOption[0]))
				{
					this._settings.showPlayingTweens = !this._settings.showPlayingTweens;
					EditorUtility.SetDirty(this._settings);
				}
				if (GUILayout.Button(this._settings.showPausedTweens ? "Hide Paused Tweens" : "Show Paused Tweens", new GUILayoutOption[0]))
				{
					this._settings.showPausedTweens = !this._settings.showPausedTweens;
					EditorUtility.SetDirty(this._settings);
				}
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal(new GUILayoutOption[0]);
				if (GUILayout.Button("Play all", new GUILayoutOption[0]))
				{
					DOTween.PlayAll();
				}
				if (GUILayout.Button("Pause all", new GUILayoutOption[0]))
				{
					DOTween.PauseAll();
				}
				if (GUILayout.Button("Kill all", new GUILayoutOption[0]))
				{
					DOTween.KillAll(false);
				}
				GUILayout.EndHorizontal();
				GUILayout.Space(8f);
				GUILayout.Label("<b>Legend: </b> TW = Tweener, SE = Sequence", EditorGUIUtils.wordWrapRichTextLabelStyle, new GUILayoutOption[0]);
				GUILayout.Space(8f);
				this._strb.Length = 0;
				this._strb.Append("Active tweens: ").Append(totActiveTweens).Append(" (").Append(TweenManager.totActiveTweeners).Append(" TW, ").Append(TweenManager.totActiveSequences).Append(" SE)").Append("\nDefault/Late/Fixed/Manual tweens: ").Append(totActiveDefaultTweens).Append("/").Append(totActiveLateTweens).Append("/").Append(totActiveFixedTweens).Append("/").Append(totActiveManualTweens).Append(this._playingTweensHex).Append("\nPlaying tweens: ").Append(num);
				if (this._settings.showPlayingTweens)
				{
					foreach (Tween tween in TweenManager._activeTweens)
					{
						if (tween != null && tween.isPlaying)
						{
							this._strb.Append("\n   - [").Append((tween.tweenType == TweenType.Tweener) ? "TW" : "SE");
							this.AppendTweenIdLabel(this._strb, tween);
							this._strb.Append("] ").Append(this.GetTargetTypeLabel(tween.target));
						}
					}
				}
				this._strb.Append("</color>");
				this._strb.Append(this._pausedTweensHex).Append("\nPaused tweens: ").Append(value);
				if (this._settings.showPausedTweens)
				{
					foreach (Tween tween2 in TweenManager._activeTweens)
					{
						if (tween2 != null && !tween2.isPlaying)
						{
							this._strb.Append("\n   - [").Append((tween2.tweenType == TweenType.Tweener) ? "TW" : "SE");
							this.AppendTweenIdLabel(this._strb, tween2);
							this._strb.Append("] ").Append(this.GetTargetTypeLabel(tween2.target));
						}
					}
				}
				this._strb.Append("</color>");
				this._strb.Append("\nPooled tweens: ").Append(TweenManager.TotalPooledTweens()).Append(" (").Append(TweenManager.totPooledTweeners).Append(" TW, ").Append(TweenManager.totPooledSequences).Append(" SE)");
				GUILayout.Label(this._strb.ToString(), EditorGUIUtils.wordWrapRichTextLabelStyle, new GUILayoutOption[0]);
				GUILayout.Space(8f);
				this._strb.Remove(0, this._strb.Length);
				this._strb.Append("Tweens Capacity: ").Append(TweenManager.maxTweeners).Append(" TW, ").Append(TweenManager.maxSequences).Append(" SE").Append("\nMax Simultaneous Active Tweens: ").Append(DOTween.maxActiveTweenersReached).Append(" TW, ").Append(DOTween.maxActiveSequencesReached).Append(" SE");
				GUILayout.Label(this._strb.ToString(), EditorGUIUtils.wordWrapRichTextLabelStyle, new GUILayoutOption[0]);
			}
			GUILayout.Space(8f);
			this._strb.Remove(0, this._strb.Length);
			this._strb.Append("<b>SETTINGS ▼</b>");
			this._strb.Append("\nSafe Mode: ").Append((this._isRuntime ? DOTween.useSafeMode : this._settings.useSafeMode) ? "ON" : "OFF");
			this._strb.Append("\nLog Behaviour: ").Append(this._isRuntime ? DOTween.logBehaviour : this._settings.logBehaviour);
			this._strb.Append("\nShow Unity Editor Report: ").Append(this._isRuntime ? DOTween.showUnityEditorReport : this._settings.showUnityEditorReport);
			this._strb.Append("\nTimeScale (Unity/DOTween): ").Append(Time.timeScale).Append("/").Append(this._isRuntime ? DOTween.timeScale : this._settings.timeScale);
			GUILayout.Label(this._strb.ToString(), EditorGUIUtils.wordWrapRichTextLabelStyle, new GUILayoutOption[0]);
			GUILayout.Label("NOTE: DOTween's TimeScale is not the same as Unity's Time.timeScale: it is actually multiplied by it except for tweens that are set to update independently", EditorGUIUtils.wordWrapRichTextLabelStyle, new GUILayoutOption[0]);
			GUILayout.Space(8f);
			this._strb.Remove(0, this._strb.Length);
			this._strb.Append("<b>DEFAULTS ▼</b>");
			this._strb.Append("\ndefaultRecyclable: ").Append(this._isRuntime ? DOTween.defaultRecyclable : this._settings.defaultRecyclable);
			this._strb.Append("\ndefaultUpdateType: ").Append(this._isRuntime ? DOTween.defaultUpdateType : this._settings.defaultUpdateType);
			this._strb.Append("\ndefaultTSIndependent: ").Append(this._isRuntime ? DOTween.defaultTimeScaleIndependent : this._settings.defaultTimeScaleIndependent);
			this._strb.Append("\ndefaultAutoKill: ").Append(this._isRuntime ? DOTween.defaultAutoKill : this._settings.defaultAutoKill);
			this._strb.Append("\ndefaultAutoPlay: ").Append(this._isRuntime ? DOTween.defaultAutoPlay : this._settings.defaultAutoPlay);
			this._strb.Append("\ndefaultEaseType: ").Append(this._isRuntime ? DOTween.defaultEaseType : this._settings.defaultEaseType);
			this._strb.Append("\ndefaultLoopType: ").Append(this._isRuntime ? DOTween.defaultLoopType : this._settings.defaultLoopType);
			GUILayout.Label(this._strb.ToString(), EditorGUIUtils.wordWrapRichTextLabelStyle, new GUILayoutOption[0]);
			GUILayout.Space(10f);
		}

		// Token: 0x06000055 RID: 85 RVA: 0x0000478C File Offset: 0x0000298C
		private void ConnectToSource(bool forceReconnection = false)
		{
			this._headerImg = (AssetDatabase.LoadAssetAtPath("Assets/" + EditorUtils.editorADBDir + "Imgs/DOTweenIcon.png", typeof(Texture2D)) as Texture2D);
			if (this._settings == null || forceReconnection)
			{
				this._settings = (this._isRuntime ? (Resources.Load("DOTweenSettings") as DOTweenSettings) : DOTweenUtilityWindow.GetDOTweenSettings());
			}
		}

		// Token: 0x06000056 RID: 86 RVA: 0x000047FC File Offset: 0x000029FC
		private void AppendTweenIdLabel(StringBuilder strb, Tween t)
		{
			if (!string.IsNullOrEmpty(t.stringId))
			{
				strb.Append(":<b>").Append(t.stringId).Append("</b>");
				return;
			}
			if (t.intId != -999)
			{
				strb.Append(":<b>").Append(t.intId).Append("</b>");
				return;
			}
			if (t.id != null)
			{
				strb.Append(":<b>").Append(t.id).Append("</b>");
			}
		}

		// Token: 0x06000057 RID: 87 RVA: 0x00004890 File Offset: 0x00002A90
		private string GetTargetTypeLabel(object tweenTarget)
		{
			if (tweenTarget == null)
			{
				return null;
			}
			string text = tweenTarget.ToString();
			int num = text.LastIndexOf('.');
			if (num != -1)
			{
				text = "(" + text.Substring(num + 1);
			}
			return text;
		}

		// Token: 0x0400004A RID: 74
		private DOTweenSettings _settings;

		// Token: 0x0400004B RID: 75
		private string _title;

		// Token: 0x0400004C RID: 76
		private readonly StringBuilder _strb = new StringBuilder();

		// Token: 0x0400004D RID: 77
		private bool _isRuntime;

		// Token: 0x0400004E RID: 78
		private Texture2D _headerImg;

		// Token: 0x0400004F RID: 79
		private string _playingTweensHex;

		// Token: 0x04000050 RID: 80
		private string _pausedTweensHex;
	}
}
