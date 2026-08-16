using System;
using DG.DOTweenEditor.UI;
using UnityEditor;

namespace DG.DOTweenEditor
{
	// Token: 0x02000009 RID: 9
	public class UtilityWindowPostProcessor : AssetPostprocessor
	{
		// Token: 0x06000048 RID: 72 RVA: 0x00003584 File Offset: 0x00001784
		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			if (UtilityWindowPostProcessor._setupDialogRequested)
			{
				return;
			}
			if (Array.FindAll<string>(importedAssets, (string name) => name.Contains("DOTween") && !name.EndsWith(".meta") && !name.EndsWith(".jpg") && !name.EndsWith(".png")).Length != 0)
			{
				EditorUtils.DelayedCall(0.1f, delegate
				{
					DOTweenUtilityWindowModules.ApplyModulesSettings();
				});
			}
			if (Array.FindAll<string>(importedAssets, (string name) => name.Contains("DOTweenPro") && !name.EndsWith(".meta") && !name.EndsWith(".jpg") && !name.EndsWith(".png")).Length != 0)
			{
				EditorUtils.DelayedCall(0.1f, delegate
				{
					ASMDEFManager.RefreshExistingASMDEFFiles();
				});
			}
		}

		// Token: 0x04000029 RID: 41
		private static bool _setupDialogRequested;
	}
}
