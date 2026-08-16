using System;
using DG.Tweening.Core;
using UnityEditor;
using UnityEngine;

namespace DG.DOTweenEditor.UI
{
	// Token: 0x0200000E RID: 14
	[CustomEditor(typeof(DOTweenSettings))]
	public class DOTweenSettingsInspector : Editor
	{
		// Token: 0x06000065 RID: 101 RVA: 0x0000568C File Offset: 0x0000388C
		private void OnEnable()
		{
			this._src = (base.target as DOTweenSettings);
		}

		// Token: 0x06000066 RID: 102 RVA: 0x0000569F File Offset: 0x0000389F
		public override void OnInspectorGUI()
		{
			GUI.enabled = false;
			base.DrawDefaultInspector();
			GUI.enabled = true;
		}

		// Token: 0x04000061 RID: 97
		private DOTweenSettings _src;
	}
}
