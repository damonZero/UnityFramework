using System;
using DG.Tweening.Core.Enums;
using UnityEngine;

namespace DG.Tweening.Core
{
	// Token: 0x0200004D RID: 77
	public class DOTweenSettings : ScriptableObject
	{
		// Token: 0x04000142 RID: 322
		public const string AssetName = "DOTweenSettings";

		// Token: 0x04000143 RID: 323
		public const string AssetFullFilename = "DOTweenSettings.asset";

		// Token: 0x04000144 RID: 324
		public bool useSafeMode = true;

		// Token: 0x04000145 RID: 325
		public DOTweenSettings.SafeModeOptions safeModeOptions = new DOTweenSettings.SafeModeOptions();

		// Token: 0x04000146 RID: 326
		public float timeScale = 1f;

		// Token: 0x04000147 RID: 327
		public bool useSmoothDeltaTime;

		// Token: 0x04000148 RID: 328
		public float maxSmoothUnscaledTime = 0.15f;

		// Token: 0x04000149 RID: 329
		public RewindCallbackMode rewindCallbackMode;

		// Token: 0x0400014A RID: 330
		public bool showUnityEditorReport;

		// Token: 0x0400014B RID: 331
		public LogBehaviour logBehaviour = LogBehaviour.ErrorsOnly;

		// Token: 0x0400014C RID: 332
		public bool drawGizmos = true;

		// Token: 0x0400014D RID: 333
		public bool defaultRecyclable;

		// Token: 0x0400014E RID: 334
		public AutoPlay defaultAutoPlay = AutoPlay.All;

		// Token: 0x0400014F RID: 335
		public UpdateType defaultUpdateType;

		// Token: 0x04000150 RID: 336
		public bool defaultTimeScaleIndependent;

		// Token: 0x04000151 RID: 337
		public Ease defaultEaseType = Ease.OutQuad;

		// Token: 0x04000152 RID: 338
		public float defaultEaseOvershootOrAmplitude = 1.70158f;

		// Token: 0x04000153 RID: 339
		public float defaultEasePeriod;

		// Token: 0x04000154 RID: 340
		public bool defaultAutoKill = true;

		// Token: 0x04000155 RID: 341
		public LoopType defaultLoopType;

		// Token: 0x04000156 RID: 342
		public bool debugMode;

		// Token: 0x04000157 RID: 343
		public bool debugStoreTargetId;

		// Token: 0x04000158 RID: 344
		public bool showPreviewPanel = true;

		// Token: 0x04000159 RID: 345
		public DOTweenSettings.SettingsLocation storeSettingsLocation;

		// Token: 0x0400015A RID: 346
		public DOTweenSettings.ModulesSetup modules = new DOTweenSettings.ModulesSetup();

		// Token: 0x0400015B RID: 347
		public bool showPlayingTweens;

		// Token: 0x0400015C RID: 348
		public bool showPausedTweens;

		// Token: 0x020000B7 RID: 183
		public enum SettingsLocation
		{
			// Token: 0x0400024E RID: 590
			AssetsDirectory,
			// Token: 0x0400024F RID: 591
			DOTweenDirectory,
			// Token: 0x04000250 RID: 592
			DemigiantDirectory
		}

		// Token: 0x020000B8 RID: 184
		[Serializable]
		public class SafeModeOptions
		{
			// Token: 0x04000251 RID: 593
			public NestedTweenFailureBehaviour nestedTweenFailureBehaviour;
		}

		// Token: 0x020000B9 RID: 185
		[Serializable]
		public class ModulesSetup
		{
			// Token: 0x04000252 RID: 594
			public bool showPanel;

			// Token: 0x04000253 RID: 595
			public bool audioEnabled = true;

			// Token: 0x04000254 RID: 596
			public bool physicsEnabled = true;

			// Token: 0x04000255 RID: 597
			public bool physics2DEnabled = true;

			// Token: 0x04000256 RID: 598
			public bool spriteEnabled = true;

			// Token: 0x04000257 RID: 599
			public bool uiEnabled = true;

			// Token: 0x04000258 RID: 600
			public bool textMeshProEnabled;

			// Token: 0x04000259 RID: 601
			public bool tk2DEnabled;

			// Token: 0x0400025A RID: 602
			public bool deAudioEnabled;

			// Token: 0x0400025B RID: 603
			public bool deUnityExtendedEnabled;
		}
	}
}
