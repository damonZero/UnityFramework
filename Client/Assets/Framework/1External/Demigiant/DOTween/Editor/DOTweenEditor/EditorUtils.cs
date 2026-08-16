using System;
using System.IO;
using System.Reflection;
using System.Text;
using DG.Tweening;
using UnityEditor;
using UnityEngine;

namespace DG.DOTweenEditor
{
	// Token: 0x02000007 RID: 7
	public static class EditorUtils
	{
		// Token: 0x17000004 RID: 4
		// (get) Token: 0x0600001A RID: 26 RVA: 0x00002904 File Offset: 0x00000B04
		// (set) Token: 0x0600001B RID: 27 RVA: 0x0000290B File Offset: 0x00000B0B
		public static string projectPath { get; private set; }

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600001C RID: 28 RVA: 0x00002913 File Offset: 0x00000B13
		// (set) Token: 0x0600001D RID: 29 RVA: 0x0000291A File Offset: 0x00000B1A
		public static string assetsPath { get; private set; }

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600001E RID: 30 RVA: 0x00002922 File Offset: 0x00000B22
		public static bool hasPro
		{
			get
			{
				EditorUtils.RetrieveDependenciesData(false);
				return EditorUtils._hasPro;
			}
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x0600001F RID: 31 RVA: 0x0000292F File Offset: 0x00000B2F
		public static bool hasDOTweenTimeline
		{
			get
			{
				EditorUtils.RetrieveDependenciesData(false);
				return EditorUtils.hasPro && EditorUtils._hasDOTweenTimeline;
			}
		}

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000020 RID: 32 RVA: 0x00002945 File Offset: 0x00000B45
		public static bool hasDOTweenTimelineUnityPackage
		{
			get
			{
				EditorUtils.RetrieveDependenciesData(false);
				return EditorUtils.hasPro && EditorUtils._hasDOTweenTimelineUnityPackage;
			}
		}

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000021 RID: 33 RVA: 0x0000295B File Offset: 0x00000B5B
		public static string proVersion
		{
			get
			{
				EditorUtils.RetrieveDependenciesData(false);
				return EditorUtils._proVersion;
			}
		}

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000022 RID: 34 RVA: 0x00002968 File Offset: 0x00000B68
		public static string editorADBDir
		{
			get
			{
				EditorUtils.RetrieveDependenciesData(false);
				return EditorUtils._editorADBDir;
			}
		}

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000023 RID: 35 RVA: 0x00002975 File Offset: 0x00000B75
		public static string demigiantDir
		{
			get
			{
				EditorUtils.RetrieveDependenciesData(false);
				return EditorUtils._demigiantDir;
			}
		}

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000024 RID: 36 RVA: 0x00002982 File Offset: 0x00000B82
		public static string dotweenDir
		{
			get
			{
				EditorUtils.RetrieveDependenciesData(false);
				return EditorUtils._dotweenDir;
			}
		}

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x06000025 RID: 37 RVA: 0x0000298F File Offset: 0x00000B8F
		public static string dotweenProDir
		{
			get
			{
				EditorUtils.RetrieveDependenciesData(false);
				return EditorUtils._dotweenProDir;
			}
		}

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x06000026 RID: 38 RVA: 0x0000299C File Offset: 0x00000B9C
		public static string dotweenProEditorDir
		{
			get
			{
				EditorUtils.RetrieveDependenciesData(false);
				return EditorUtils._dotweenProEditorDir;
			}
		}

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x06000027 RID: 39 RVA: 0x000029A9 File Offset: 0x00000BA9
		public static string dotweenModulesDir
		{
			get
			{
				EditorUtils.RetrieveDependenciesData(false);
				return EditorUtils._dotweenModulesDir;
			}
		}

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x06000028 RID: 40 RVA: 0x000029B6 File Offset: 0x00000BB6
		public static string dotweenTimelineDir
		{
			get
			{
				EditorUtils.RetrieveDependenciesData(false);
				return EditorUtils._dotweenTimelineDir;
			}
		}

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000029 RID: 41 RVA: 0x000029C3 File Offset: 0x00000BC3
		public static string dotweenTimelineUnityPackageFilePath
		{
			get
			{
				EditorUtils.RetrieveDependenciesData(false);
				return EditorUtils._dotweenTimelineUnityPackageFilePath;
			}
		}

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x0600002A RID: 42 RVA: 0x000029D0 File Offset: 0x00000BD0
		// (set) Token: 0x0600002B RID: 43 RVA: 0x000029D7 File Offset: 0x00000BD7
		public static bool isOSXEditor { get; private set; } = Application.platform == 0;

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x0600002C RID: 44 RVA: 0x000029DF File Offset: 0x00000BDF
		// (set) Token: 0x0600002D RID: 45 RVA: 0x000029E6 File Offset: 0x00000BE6
		public static string pathSlash { get; private set; }

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x0600002E RID: 46 RVA: 0x000029EE File Offset: 0x00000BEE
		// (set) Token: 0x0600002F RID: 47 RVA: 0x000029F5 File Offset: 0x00000BF5
		public static string pathSlashToReplace { get; private set; }

		// Token: 0x06000030 RID: 48 RVA: 0x00002A00 File Offset: 0x00000C00
		static EditorUtils()
		{
			bool flag = Application.platform == (RuntimePlatform) 7;
			EditorUtils.pathSlash = (flag ? "\\" : "/");
			EditorUtils.pathSlashToReplace = (flag ? "/" : "\\");
			EditorUtils.projectPath = Application.dataPath;
			EditorUtils.projectPath = EditorUtils.projectPath.Substring(0, EditorUtils.projectPath.LastIndexOf("/"));
			EditorUtils.projectPath = EditorUtils.projectPath.Replace(EditorUtils.pathSlashToReplace, EditorUtils.pathSlash);
			EditorUtils.assetsPath = EditorUtils.projectPath + EditorUtils.pathSlash + "Assets";
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00002AAE File Offset: 0x00000CAE
		public static void RetrieveDependenciesData(bool force = false)
		{
			if (!force && EditorUtils._retrievedDependenciesData)
			{
				return;
			}
			EditorUtils._retrievedDependenciesData = true;
			EditorUtils.CheckForPro();
			EditorUtils.StoreEditorADBDir();
			EditorUtils.StoreDOTweenDirsAndFilePaths();
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00002AD0 File Offset: 0x00000CD0
		public static void DelayedCall(float delay, Action callback)
		{
			new DelayedCall(delay, callback);
		}

		/// <summary>
		/// Checks that the given editor texture use the correct import settings,
		/// and applies them if they're incorrect.
		/// </summary>
		// Token: 0x06000033 RID: 51 RVA: 0x00002ADC File Offset: 0x00000CDC
		public static void SetEditorTexture(Texture2D texture, FilterMode filterMode = 0, int maxTextureSize = 32)
		{
			if (texture.wrapMode == (TextureWrapMode) 1)
			{
				return;
			}
			string assetPath = AssetDatabase.GetAssetPath(texture);
			TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
			textureImporter.textureType = (TextureImporterType) 2;
			textureImporter.npotScale = 0;
			textureImporter.filterMode = filterMode;
			textureImporter.wrapMode = (TextureWrapMode) 1;
			textureImporter.maxTextureSize = maxTextureSize;
			textureImporter.textureFormat = (TextureImporterFormat) (-3);
			AssetDatabase.ImportAsset(assetPath);
		}

		/// <summary>
		/// Returns TRUE if setup is required
		/// </summary>
		// Token: 0x06000034 RID: 52 RVA: 0x00002B33 File Offset: 0x00000D33
		public static bool DOTweenSetupRequired()
		{
			return Directory.Exists(EditorUtils.dotweenDir) && Directory.GetFiles(EditorUtils.dotweenDir + "Editor", "DOTweenUpgradeManager.*").Length != 0;
		}

		// Token: 0x06000035 RID: 53 RVA: 0x00002B60 File Offset: 0x00000D60
		public static void DeleteDOTweenUpgradeManagerFiles()
		{
			Type type = Type.GetType("DG.DOTweenUpgradeManager.Autorun, DOTweenUpgradeManager");
			if (type == null)
			{
				return;
			}
			string text = type.Assembly.Location;
			text = text.Substring(0, text.LastIndexOf('.'));
			AssetDatabase.StartAssetEditing();
			EditorUtils.DeleteAssetsIfExist(new string[]
			{
				EditorUtils.FullPathToADBPath(text + ".dll"),
				EditorUtils.FullPathToADBPath(text + ".dll.mdb"),
				EditorUtils.FullPathToADBPath(text + ".pdb"),
				EditorUtils.FullPathToADBPath(text + ".xml")
			});
			AssetDatabase.StopAssetEditing();
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00002BFC File Offset: 0x00000DFC
		public static void DeleteLegacyNoModulesDOTweenFiles()
		{
			string str = EditorUtils.FullPathToADBPath(EditorUtils.dotweenDir);
			AssetDatabase.StartAssetEditing();
			EditorUtils.DeleteAssetsIfExist(new string[]
			{
				str + "DOTween43.dll",
				str + "DOTween43.xml",
				str + "DOTween43.dll.mdb",
				str + "DOTween43.dll.addon",
				str + "DOTween43.xml.addon",
				str + "DOTween43.dll.mdb.addon",
				str + "DOTween46.dll",
				str + "DOTween46.xml",
				str + "DOTween46.dll.mdb",
				str + "DOTween46.dll.addon",
				str + "DOTween46.xml.addon",
				str + "DOTween46.dll.mdb.addon",
				str + "DOTween50.dll",
				str + "DOTween50.xml",
				str + "DOTween50.dll.mdb",
				str + "DOTween50.dll.addon",
				str + "DOTween50.xml.addon",
				str + "DOTween50.dll.mdb.addon",
				str + "DOTweenTextMeshPro.cs.addon",
				str + "DOTweenTextMeshPro_mod.cs",
				str + "DOTweenTk2d.cs.addon"
			});
			AssetDatabase.StopAssetEditing();
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00002D5C File Offset: 0x00000F5C
		public static void DeleteOldDemiLibCore()
		{
			string text = EditorUtils.GetAssemblyFilePath(typeof(DOTween).Assembly);
			string text2 = (text.IndexOf("/") != -1) ? "/" : "\\";
			text = text.Substring(0, text.LastIndexOf(text2));
			text = text.Substring(0, text.LastIndexOf(text2)) + text2 + "DemiLib";
			string text3 = EditorUtils.FullPathToADBPath(text);
			if (!EditorUtils.AssetExists(text3))
			{
				return;
			}
			string text4 = text3 + "/Core";
			if (!EditorUtils.AssetExists(text4))
			{
				return;
			}
			EditorUtils.DeleteAssetsIfExist(new string[]
			{
				text3 + "/DemiLib.dll",
				text3 + "/DemiLib.xml",
				text3 + "/DemiLib.dll.mdb",
				text3 + "/Editor/DemiEditor.dll",
				text3 + "/Editor/DemiEditor.xml",
				text3 + "/Editor/DemiEditor.dll.mdb",
				text3 + "/Editor/Imgs"
			});
			if (EditorUtils.AssetExists(text3 + "/Editor") && Directory.GetFiles(text + text2 + "Editor").Length == 0)
			{
				AssetDatabase.DeleteAsset(text3 + "/Editor");
				AssetDatabase.ImportAsset(text4, (ImportAssetOptions) 256);
			}
		}

		// Token: 0x06000038 RID: 56 RVA: 0x00002E98 File Offset: 0x00001098
		private static void DeleteAssetsIfExist(string[] adbFilePaths)
		{
			foreach (string text in adbFilePaths)
			{
				if (EditorUtils.AssetExists(text))
				{
					AssetDatabase.DeleteAsset(text);
				}
			}
		}

		/// <summary>
		/// Returns TRUE if the file/directory at the given path exists.
		/// </summary>
		/// <param name="adbPath">Path, relative to Unity's project folder</param>
		/// <returns></returns>
		// Token: 0x06000039 RID: 57 RVA: 0x00002EC8 File Offset: 0x000010C8
		public static bool AssetExists(string adbPath)
		{
			string path = EditorUtils.ADBPathToFullPath(adbPath);
			return File.Exists(path) || Directory.Exists(path);
		}

		/// <summary>
		/// Converts the given project-relative path to a full path,
		/// with backward (\) slashes).
		/// </summary>
		// Token: 0x0600003A RID: 58 RVA: 0x00002EEC File Offset: 0x000010EC
		public static string ADBPathToFullPath(string adbPath)
		{
			adbPath = adbPath.Replace(EditorUtils.pathSlashToReplace, EditorUtils.pathSlash);
			return EditorUtils.projectPath + EditorUtils.pathSlash + adbPath;
		}

		/// <summary>
		/// Converts the given full path to a path usable with AssetDatabase methods
		/// (relative to Unity's project folder, and with the correct Unity forward (/) slashes).
		/// </summary>
		// Token: 0x0600003B RID: 59 RVA: 0x00002F10 File Offset: 0x00001110
		public static string FullPathToADBPath(string fullPath)
		{
			return fullPath.Substring(EditorUtils.projectPath.Length + 1).Replace("\\", "/");
		}

		/// <summary>
		/// Connects to a <see cref="T:UnityEngine.ScriptableObject" /> asset.
		/// If the asset already exists at the given path, loads it and returns it.
		/// Otherwise, either returns NULL or automatically creates it before loading and returning it
		/// (depending on the given parameters).
		/// </summary>
		/// <typeparam name="T">Asset type</typeparam>
		/// <param name="adbFilePath">File path (relative to Unity's project folder)</param>
		/// <param name="createIfMissing">If TRUE and the requested asset doesn't exist, forces its creation</param>
		// Token: 0x0600003C RID: 60 RVA: 0x00002F34 File Offset: 0x00001134
		public static T ConnectToSourceAsset<T>(string adbFilePath, bool createIfMissing = false) where T : ScriptableObject
		{
			if (!EditorUtils.AssetExists(adbFilePath))
			{
				if (!createIfMissing)
				{
					return default(T);
				}
				EditorUtils.CreateScriptableAsset<T>(adbFilePath);
			}
			T t = (T)((object)AssetDatabase.LoadAssetAtPath(adbFilePath, typeof(T)));
			if (t == null)
			{
				EditorUtils.CreateScriptableAsset<T>(adbFilePath);
				t = (T)((object)AssetDatabase.LoadAssetAtPath(adbFilePath, typeof(T)));
			}
			return t;
		}

		/// <summary>
		/// Full path for the given loaded assembly, assembly file included
		/// </summary>
		// Token: 0x0600003D RID: 61 RVA: 0x00002FA0 File Offset: 0x000011A0
		public static string GetAssemblyFilePath(Assembly assembly)
		{
			string text = Uri.UnescapeDataString(new UriBuilder(assembly.CodeBase).Path);
			if (text.Substring(text.Length - 3) == "dll")
			{
				return text;
			}
			return Path.GetFullPath(assembly.Location);
		}

		/// <summary>
		/// Adds the given global define if it's not already present
		/// </summary>
		// Token: 0x0600003E RID: 62 RVA: 0x00002FEC File Offset: 0x000011EC
		public static void AddGlobalDefine(string id)
		{
			bool flag = false;
			int num = 0;
			foreach (BuildTargetGroup buildTargetGroup in (BuildTargetGroup[])Enum.GetValues(typeof(BuildTargetGroup)))
			{
				if (EditorUtils.IsValidBuildTargetGroup(buildTargetGroup))
				{
					string text = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
					if (Array.IndexOf<string>(text.Split(new char[]
					{
						';'
					}), id) == -1)
					{
						flag = true;
						num++;
						text += ((text.Length > 0) ? (";" + id) : id);
						PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, text);
					}
				}
			}
			if (flag)
			{
				Debug.Log(string.Format("DOTween : added global define \"{0}\" to {1} BuildTargetGroups", id, num));
			}
		}

		/// <summary>
		/// Removes the given global define if it's present
		/// </summary>
		// Token: 0x0600003F RID: 63 RVA: 0x0000309C File Offset: 0x0000129C
		public static void RemoveGlobalDefine(string id)
		{
			bool flag = false;
			int num = 0;
			foreach (BuildTargetGroup buildTargetGroup in (BuildTargetGroup[])Enum.GetValues(typeof(BuildTargetGroup)))
			{
				if (EditorUtils.IsValidBuildTargetGroup(buildTargetGroup))
				{
					string[] array2 = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(new char[]
					{
						';'
					});
					if (Array.IndexOf<string>(array2, id) != -1)
					{
						flag = true;
						num++;
						EditorUtils._Strb.Length = 0;
						for (int j = 0; j < array2.Length; j++)
						{
							if (!(array2[j] == id))
							{
								if (EditorUtils._Strb.Length > 0)
								{
									EditorUtils._Strb.Append(';');
								}
								EditorUtils._Strb.Append(array2[j]);
							}
						}
						PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, EditorUtils._Strb.ToString());
					}
				}
			}
			EditorUtils._Strb.Length = 0;
			if (flag)
			{
				Debug.Log(string.Format("DOTween : removed global define \"{0}\" from {1} BuildTargetGroups", id, num));
			}
		}

		/// <summary>
		/// Returns TRUE if the given global define is present in all the <see cref="T:UnityEditor.BuildTargetGroup" />
		/// or only in the given <see cref="T:UnityEditor.BuildTargetGroup" />, depending on passed parameters.<para />
		/// </summary>
		/// <param name="id"></param>
		/// <param name="buildTargetGroup"><see cref="T:UnityEditor.BuildTargetGroup" />to use. Leave NULL to check in all of them.</param>
		// Token: 0x06000040 RID: 64 RVA: 0x0000319C File Offset: 0x0000139C
		public static bool HasGlobalDefine(string id, BuildTargetGroup? buildTargetGroup = null)
		{
			BuildTargetGroup[] array;
			if (buildTargetGroup != null)
			{
				(array = new BuildTargetGroup[1])[0] = buildTargetGroup.Value;
			}
			else
			{
				array = (BuildTargetGroup[])Enum.GetValues(typeof(BuildTargetGroup));
			}
			foreach (BuildTargetGroup buildTargetGroup2 in array)
			{
				if (EditorUtils.IsValidBuildTargetGroup(buildTargetGroup2) && Array.IndexOf<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup2).Split(new char[]
				{
					';'
				}), id) != -1)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00003218 File Offset: 0x00001418
		private static void CheckForPro()
		{
			EditorUtils._hasCheckedForPro = true;
			try
			{
				EditorUtils._proVersion = (Assembly.Load("DOTweenPro, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null").GetType("DG.Tweening.DOTweenPro").GetField("Version", BindingFlags.Static | BindingFlags.Public).GetValue(null) as string);
				EditorUtils._hasPro = true;
			}
			catch
			{
				EditorUtils._hasPro = false;
				EditorUtils._proVersion = "-";
			}
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00003288 File Offset: 0x00001488
		private static void StoreEditorADBDir()
		{
			EditorUtils._editorADBDir = "Framework/External/Demigiant/DOTween/Editor/";
			// Path.GetDirectoryName(EditorUtils.GetAssemblyFilePath(Assembly.GetExecutingAssembly())).Substring(Application.dataPath.Length + 1).Replace("\\", "/") + "/";
		}

		// Token: 0x06000043 RID: 67 RVA: 0x000032C8 File Offset: 0x000014C8
		private static void StoreDOTweenDirsAndFilePaths()
		{
			EditorUtils._dotweenDir = "Framework/External/Demigiant/DOTween/Editor/";
				// Path.GetDirectoryName(EditorUtils.GetAssemblyFilePath(Assembly.GetExecutingAssembly()));
				string text = (EditorUtils._dotweenDir.IndexOf("/") != -1) ? "/" : "\\";
			EditorUtils._dotweenDir = EditorUtils._dotweenDir.Substring(0, EditorUtils._dotweenDir.LastIndexOf(text) + 1);
			string text2 = EditorUtils._dotweenDir.Substring(0, EditorUtils._dotweenDir.LastIndexOf(text));
			text2 = text2.Substring(0, text2.LastIndexOf(text) + 1);
			EditorUtils._dotweenProDir = text2 + "DOTweenPro" + text;
			EditorUtils._dotweenTimelineDir = text2 + "DOTweenTimeline" + text;
			EditorUtils._demigiantDir = ((text2.Substring(text2.Length - 10, 9) == "Demigiant") ? text2 : null);
			EditorUtils._dotweenDir = EditorUtils._dotweenDir.Replace(EditorUtils.pathSlashToReplace, EditorUtils.pathSlash);
			EditorUtils._dotweenProDir = EditorUtils._dotweenProDir.Replace(EditorUtils.pathSlashToReplace, EditorUtils.pathSlash);
			EditorUtils._dotweenProEditorDir = EditorUtils._dotweenProDir + "Editor" + EditorUtils.pathSlash;
			EditorUtils._dotweenModulesDir = EditorUtils._dotweenDir + "Modules" + EditorUtils.pathSlash;
			if (EditorUtils._demigiantDir != null)
			{
				EditorUtils._demigiantDir = EditorUtils._demigiantDir.Replace(EditorUtils.pathSlashToReplace, EditorUtils.pathSlash);
			}
			EditorUtils._dotweenTimelineUnityPackageFilePath = EditorUtils._dotweenProDir + "DOTweenTimeline_UnityPackage.unitypackage";
			EditorUtils._hasDOTweenTimelineUnityPackage = File.Exists(EditorUtils._dotweenTimelineUnityPackageFilePath);
			EditorUtils._hasDOTweenTimeline = Directory.Exists(EditorUtils._dotweenTimelineDir);
		}

		// Token: 0x06000044 RID: 68 RVA: 0x0000344B File Offset: 0x0000164B
		private static void CreateScriptableAsset<T>(string adbFilePath) where T : ScriptableObject
		{
			AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), adbFilePath);
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00003460 File Offset: 0x00001660
		private static bool IsValidBuildTargetGroup(BuildTargetGroup group)
		{
			if (group == null)
			{
				return false;
			}
			MethodBase method = Type.GetType("UnityEditor.Modules.ModuleManager, UnityEditor.dll").GetMethod("GetTargetStringFromBuildTargetGroup", BindingFlags.Static | BindingFlags.NonPublic);
			MethodInfo method2 = typeof(PlayerSettings).GetMethod("GetPlatformName", BindingFlags.Static | BindingFlags.NonPublic);
			string value = (string)method.Invoke(null, new object[]
			{
				group
			});
			string value2 = (string)method2.Invoke(null, new object[]
			{
				group
			});
			return !string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(value2);
		}

		// Token: 0x0400001A RID: 26
		private static readonly StringBuilder _Strb = new StringBuilder();

		// Token: 0x0400001B RID: 27
		private static bool _retrievedDependenciesData;

		// Token: 0x0400001C RID: 28
		private static bool _hasPro;

		// Token: 0x0400001D RID: 29
		private static bool _hasDOTweenTimeline;

		// Token: 0x0400001E RID: 30
		private static bool _hasDOTweenTimelineUnityPackage;

		// Token: 0x0400001F RID: 31
		private static string _proVersion;

		// Token: 0x04000020 RID: 32
		private static bool _hasCheckedForPro;

		// Token: 0x04000021 RID: 33
		private static string _editorADBDir;

		// Token: 0x04000022 RID: 34
		private static string _demigiantDir;

		// Token: 0x04000023 RID: 35
		private static string _dotweenDir;

		// Token: 0x04000024 RID: 36
		private static string _dotweenProDir;

		// Token: 0x04000025 RID: 37
		private static string _dotweenProEditorDir;

		// Token: 0x04000026 RID: 38
		private static string _dotweenModulesDir;

		// Token: 0x04000027 RID: 39
		private static string _dotweenTimelineDir;

		// Token: 0x04000028 RID: 40
		private static string _dotweenTimelineUnityPackageFilePath;
	}
}
