using System;
using System.IO;
using DG.DOTweenEditor.UI;
using DG.Tweening.Core;
using UnityEditor;
using UnityEngine;

namespace DG.DOTweenEditor
{
	// Token: 0x02000003 RID: 3
	internal static class ASMDEFManager
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000003 RID: 3 RVA: 0x000020F7 File Offset: 0x000002F7
		// (set) Token: 0x06000004 RID: 4 RVA: 0x000020FE File Offset: 0x000002FE
		public static bool hasModulesASMDEF { get; private set; }

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000005 RID: 5 RVA: 0x00002106 File Offset: 0x00000306
		// (set) Token: 0x06000006 RID: 6 RVA: 0x0000210D File Offset: 0x0000030D
		public static bool hasProASMDEF { get; private set; }

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000007 RID: 7 RVA: 0x00002115 File Offset: 0x00000315
		// (set) Token: 0x06000008 RID: 8 RVA: 0x0000211C File Offset: 0x0000031C
		public static bool hasProEditorASMDEF { get; private set; }

		// Token: 0x06000009 RID: 9 RVA: 0x00002124 File Offset: 0x00000324
		static ASMDEFManager()
		{
			ASMDEFManager.Refresh();
		}

		// Token: 0x0600000A RID: 10 RVA: 0x0000212C File Offset: 0x0000032C
		public static void Refresh()
		{
			ASMDEFManager.hasModulesASMDEF = File.Exists(EditorUtils.dotweenModulesDir + "DOTween.Modules.asmdef");
			ASMDEFManager.hasProASMDEF = File.Exists(EditorUtils.dotweenProDir + "DOTweenPro.Scripts.asmdef");
			ASMDEFManager.hasProEditorASMDEF = File.Exists(EditorUtils.dotweenProEditorDir + "DOTweenPro.EditorScripts.asmdef");
		}

		// Token: 0x0600000B RID: 11 RVA: 0x00002184 File Offset: 0x00000384
		public static void RefreshExistingASMDEFFiles()
		{
			ASMDEFManager.Refresh();
			if (!ASMDEFManager.hasModulesASMDEF)
			{
				if (ASMDEFManager.hasProASMDEF)
				{
					ASMDEFManager.RemoveASMDEF(ASMDEFManager.ASMDEFType.DOTweenPro);
				}
				if (ASMDEFManager.hasProEditorASMDEF)
				{
					ASMDEFManager.RemoveASMDEF(ASMDEFManager.ASMDEFType.DOTweenProEditor);
				}
				return;
			}
			if (!EditorUtils.hasPro)
			{
				return;
			}
			if (!ASMDEFManager.hasProASMDEF)
			{
				ASMDEFManager.CreateASMDEF(ASMDEFManager.ASMDEFType.DOTweenPro, false);
			}
			if (!ASMDEFManager.hasProEditorASMDEF)
			{
				ASMDEFManager.CreateASMDEF(ASMDEFManager.ASMDEFType.DOTweenProEditor, false);
			}
			DOTweenSettings dotweenSettings = DOTweenUtilityWindow.GetDOTweenSettings();
			if (dotweenSettings == null)
			{
				return;
			}
			ASMDEFManager.ValidateProASMDEFReferences(dotweenSettings, ASMDEFManager.ASMDEFType.DOTweenPro, EditorUtils.dotweenProDir + "DOTweenPro.Scripts.asmdef");
			ASMDEFManager.ValidateProASMDEFReferences(dotweenSettings, ASMDEFManager.ASMDEFType.DOTweenProEditor, EditorUtils.dotweenProEditorDir + "DOTweenPro.EditorScripts.asmdef");
		}

		// Token: 0x0600000C RID: 12 RVA: 0x00002218 File Offset: 0x00000418
		public static void CreateAllASMDEF()
		{
			ASMDEFManager.CreateASMDEF(ASMDEFManager.ASMDEFType.Modules, false);
			if (!EditorUtils.hasPro)
			{
				return;
			}
			ASMDEFManager.CreateASMDEF(ASMDEFManager.ASMDEFType.DOTweenPro, false);
			ASMDEFManager.CreateASMDEF(ASMDEFManager.ASMDEFType.DOTweenProEditor, false);
		}

		// Token: 0x0600000D RID: 13 RVA: 0x00002237 File Offset: 0x00000437
		public static void RemoveAllASMDEF()
		{
			ASMDEFManager.RemoveASMDEF(ASMDEFManager.ASMDEFType.Modules);
			ASMDEFManager.RemoveASMDEF(ASMDEFManager.ASMDEFType.DOTweenPro);
			ASMDEFManager.RemoveASMDEF(ASMDEFManager.ASMDEFType.DOTweenProEditor);
		}

		// Token: 0x0600000E RID: 14 RVA: 0x0000224C File Offset: 0x0000044C
		private static void ValidateProASMDEFReferences(DOTweenSettings src, ASMDEFManager.ASMDEFType asmdefType, string asmdefFilepath)
		{
			bool flag = false;
			using (StreamReader streamReader = new StreamReader(asmdefFilepath))
			{
				string text;
				while ((text = streamReader.ReadLine()) != null)
				{
					if (text.Contains("Unity.TextMeshPro"))
					{
						flag = true;
						break;
					}
				}
			}
			if (flag != src.modules.textMeshProEnabled)
			{
				ASMDEFManager.CreateASMDEF(asmdefType, true);
			}
		}

		// Token: 0x0600000F RID: 15 RVA: 0x000022B8 File Offset: 0x000004B8
		private static void LogASMDEFChange(ASMDEFManager.ASMDEFType asmdefType, ASMDEFManager.ChangeType changeType)
		{
			string arg = "";
			switch (asmdefType)
			{
			case ASMDEFManager.ASMDEFType.Modules:
				arg = "DOTween/Modules/DOTween.Modules.asmdef";
				break;
			case ASMDEFManager.ASMDEFType.DOTweenPro:
				arg = "DOTweenPro/DOTweenPro.Scripts.asmdef";
				break;
			case ASMDEFManager.ASMDEFType.DOTweenProEditor:
				arg = "DOTweenPro/Editor/DOTweenPro.EditorScripts.asmdef";
				break;
			}
			Debug.Log(string.Format("<b>DOTween ASMDEF file <color=#{0}>{1}</color></b> ► {2}", (changeType == ASMDEFManager.ChangeType.Deleted) ? "ff0000" : ((changeType == ASMDEFManager.ChangeType.Created) ? "00ff00" : "ff6600"), (changeType == ASMDEFManager.ChangeType.Deleted) ? "removed" : ((changeType == ASMDEFManager.ChangeType.Created) ? "created" : "changed"), arg));
		}

		// Token: 0x06000010 RID: 16 RVA: 0x0000233C File Offset: 0x0000053C
		private static void CreateASMDEF(ASMDEFManager.ASMDEFType type, bool forceOverwrite = false)
		{
			ASMDEFManager.Refresh();
			bool flag = false;
			string arg = null;
			string text = null;
			string text2 = null;
			switch (type)
			{
			case ASMDEFManager.ASMDEFType.Modules:
				flag = ASMDEFManager.hasModulesASMDEF;
				arg = "DOTween.Modules";
				text = "DOTween.Modules.asmdef";
				text2 = EditorUtils.dotweenModulesDir;
				break;
			case ASMDEFManager.ASMDEFType.DOTweenPro:
				flag = ASMDEFManager.hasProASMDEF;
				arg = "DOTweenPro.Scripts";
				text = "DOTweenPro.Scripts.asmdef";
				text2 = EditorUtils.dotweenProDir;
				break;
			case ASMDEFManager.ASMDEFType.DOTweenProEditor:
				flag = ASMDEFManager.hasProEditorASMDEF;
				arg = "DOTweenPro.EditorScripts";
				text = "DOTweenPro.EditorScripts.asmdef";
				text2 = EditorUtils.dotweenProEditorDir;
				break;
			}
			if (flag && !forceOverwrite)
			{
				EditorUtility.DisplayDialog("Create ASMDEF", text + " already exists", "Ok");
				return;
			}
			if (!Directory.Exists(text2))
			{
				EditorUtility.DisplayDialog("Create ASMDEF", string.Format("Directory not found\n({0})", text2), "Ok");
				return;
			}
			string text3 = text2 + text;
			using (StreamWriter streamWriter = File.CreateText(text3))
			{
				streamWriter.WriteLine("{");
				if (type != ASMDEFManager.ASMDEFType.Modules)
				{
					if (type - ASMDEFManager.ASMDEFType.DOTweenPro <= 1)
					{
						streamWriter.WriteLine("\t\"name\": \"{0}\",", arg);
						streamWriter.WriteLine("\t\"references\": [");
						DOTweenSettings dotweenSettings = DOTweenUtilityWindow.GetDOTweenSettings();
						if (dotweenSettings != null && dotweenSettings.modules.textMeshProEnabled)
						{
							streamWriter.WriteLine("\t\t\"{0}\",", "Unity.TextMeshPro");
						}
						if (type == ASMDEFManager.ASMDEFType.DOTweenProEditor)
						{
							streamWriter.WriteLine("\t\t\"{0}\",", "DOTween.Modules");
							streamWriter.WriteLine("\t\t\"{0}\"", "DOTweenPro.Scripts");
							streamWriter.WriteLine("\t],");
							streamWriter.WriteLine("\t\"includePlatforms\": [");
							streamWriter.WriteLine("\t\t\"Editor\"");
							streamWriter.WriteLine("\t],");
							streamWriter.WriteLine("\t\"autoReferenced\": false");
						}
						else
						{
							streamWriter.WriteLine("\t\t\"{0}\"", "DOTween.Modules");
							streamWriter.WriteLine("\t]");
						}
					}
				}
				else
				{
					streamWriter.WriteLine("\t\"name\": \"{0}\"", arg);
				}
				streamWriter.WriteLine("}");
			}
			AssetDatabase.ImportAsset(EditorUtils.FullPathToADBPath(text3), (ImportAssetOptions) 1);
			ASMDEFManager.Refresh();
			ASMDEFManager.LogASMDEFChange(type, flag ? ASMDEFManager.ChangeType.Overwritten : ASMDEFManager.ChangeType.Created);
		}

		// Token: 0x06000011 RID: 17 RVA: 0x0000255C File Offset: 0x0000075C
		private static void RemoveASMDEF(ASMDEFManager.ASMDEFType type)
		{
			bool flag = false;
			string text = null;
			string str = null;
			switch (type)
			{
			case ASMDEFManager.ASMDEFType.Modules:
				flag = ASMDEFManager.hasModulesASMDEF;
				str = EditorUtils.dotweenModulesDir;
				text = "DOTween.Modules.asmdef";
				break;
			case ASMDEFManager.ASMDEFType.DOTweenPro:
				flag = ASMDEFManager.hasProASMDEF;
				text = "DOTweenPro.Scripts.asmdef";
				str = EditorUtils.dotweenProDir;
				break;
			case ASMDEFManager.ASMDEFType.DOTweenProEditor:
				flag = ASMDEFManager.hasProEditorASMDEF;
				text = "DOTweenPro.EditorScripts.asmdef";
				str = EditorUtils.dotweenProEditorDir;
				break;
			}
			ASMDEFManager.Refresh();
			if (!flag)
			{
				EditorUtility.DisplayDialog("Remove ASMDEF", text + " not present", "Ok");
				return;
			}
			AssetDatabase.DeleteAsset(EditorUtils.FullPathToADBPath(str + text));
			ASMDEFManager.Refresh();
			ASMDEFManager.LogASMDEFChange(type, ASMDEFManager.ChangeType.Deleted);
		}

		// Token: 0x04000007 RID: 7
		private const string _ModulesId = "DOTween.Modules";

		// Token: 0x04000008 RID: 8
		private const string _ProId = "DOTweenPro.Scripts";

		// Token: 0x04000009 RID: 9
		private const string _ProEditorId = "DOTweenPro.EditorScripts";

		// Token: 0x0400000A RID: 10
		private const string _ModulesASMDEFFile = "DOTween.Modules.asmdef";

		// Token: 0x0400000B RID: 11
		private const string _ProASMDEFFile = "DOTweenPro.Scripts.asmdef";

		// Token: 0x0400000C RID: 12
		private const string _ProEditorASMDEFFile = "DOTweenPro.EditorScripts.asmdef";

		// Token: 0x0400000D RID: 13
		private const string _RefTextMeshPro = "Unity.TextMeshPro";

		// Token: 0x02000010 RID: 16
		public enum ASMDEFType
		{
			// Token: 0x04000074 RID: 116
			Modules,
			// Token: 0x04000075 RID: 117
			DOTweenPro,
			// Token: 0x04000076 RID: 118
			DOTweenProEditor
		}

		// Token: 0x02000011 RID: 17
		private enum ChangeType
		{
			// Token: 0x04000078 RID: 120
			Deleted,
			// Token: 0x04000079 RID: 121
			Created,
			// Token: 0x0400007A RID: 122
			Overwritten
		}
	}
}
