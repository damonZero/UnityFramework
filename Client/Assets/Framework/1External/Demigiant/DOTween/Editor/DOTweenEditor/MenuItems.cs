using System;
using DG.Tweening.Core;
using UnityEditor;
using UnityEngine;

namespace DG.DOTweenEditor
{
	// Token: 0x02000006 RID: 6
	internal static class MenuItems
	{
		// Token: 0x06000019 RID: 25 RVA: 0x000028B8 File Offset: 0x00000AB8
		[MenuItem("GameObject/Demigiant/DOTween Manager", false, 20)]
		private static void CreateDOTweenComponent(MenuCommand menuCommand)
		{
			GameObject gameObject = new GameObject("[DOTween]");
			gameObject.AddComponent<DOTweenComponent>();
			GameObjectUtility.SetParentAndAlign(gameObject, menuCommand.context as GameObject);
			Undo.RegisterCreatedObjectUndo(gameObject, "Create " + gameObject.name);
			Selection.activeObject = gameObject;
		}
	}
}
