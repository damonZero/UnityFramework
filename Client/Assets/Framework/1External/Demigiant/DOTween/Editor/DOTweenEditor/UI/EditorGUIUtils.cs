using System;
using DG.Tweening;
using UnityEditor;
using UnityEngine;

namespace DG.DOTweenEditor.UI
{
	// Token: 0x0200000B RID: 11
	public static class EditorGUIUtils
	{
		// Token: 0x17000015 RID: 21
		// (get) Token: 0x0600004C RID: 76 RVA: 0x000036AC File Offset: 0x000018AC
		public static Texture2D logo
		{
			get
			{
				if (EditorGUIUtils._logo == null)
				{
					string path = "Assets/" + EditorUtils.editorADBDir + "Imgs/DOTweenIcon.png";
					EditorGUIUtils._logo = (AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) as Texture2D);
					EditorUtils.SetEditorTexture(EditorGUIUtils._logo, (FilterMode) 1, 128);
				}
				return EditorGUIUtils._logo;
			}
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00003708 File Offset: 0x00001908
		public static Ease FilteredEasePopup(string label, Ease currEase, GUIStyle style = null)
		{
			if (style == null)
			{
				style = EditorStyles.popup;
			}
			return EditorGUIUtils.FilteredEasePopup(EditorGUILayout.GetControlRect(label != null, 18f, style, new GUILayoutOption[0]), label, currEase, style);
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00003734 File Offset: 0x00001934
		public static Ease FilteredEasePopup(Rect rect, string label, Ease currEase, GUIStyle style = null)
		{
			int num = (currEase == Ease.INTERNAL_Custom) ? (EditorGUIUtils.FilteredEaseTypes.Length - 1) : Array.IndexOf<string>(EditorGUIUtils.FilteredEaseTypes, currEase.ToString());
			if (num == -1)
			{
				num = 0;
			}
			num = ((label == null) ? EditorGUI.Popup(rect, num, EditorGUIUtils.FilteredEaseTypes, (style == null) ? EditorStyles.popup : style) : EditorGUI.Popup(rect, label, num, EditorGUIUtils.FilteredEaseTypes, (style == null) ? EditorStyles.popup : style));
			if (num != EditorGUIUtils.FilteredEaseTypes.Length - 1)
			{
				return (Ease)Enum.Parse(typeof(Ease), EditorGUIUtils.FilteredEaseTypes[num]);
			}
			return Ease.INTERNAL_Custom;
		}

		// Token: 0x0600004F RID: 79 RVA: 0x000037CE File Offset: 0x000019CE
		public static void InspectorLogo()
		{
			GUILayout.Box(EditorGUIUtils.logo, EditorGUIUtils.logoIconStyle, new GUILayoutOption[0]);
		}

		// Token: 0x06000050 RID: 80 RVA: 0x000037E8 File Offset: 0x000019E8
		public static bool ToggleButton(bool toggled, GUIContent content, bool alert = false, GUIStyle guiStyle = null, params GUILayoutOption[] options)
		{
			Color backgroundColor = GUI.backgroundColor;
			GUI.backgroundColor = (toggled ? (alert ? Color.red : Color.green) : Color.white);
			if ((guiStyle == null) ? GUILayout.Button(content, options) : GUILayout.Button(content, guiStyle, options))
			{
				toggled = !toggled;
				GUI.changed = true;
			}
			GUI.backgroundColor = backgroundColor;
			return toggled;
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00003844 File Offset: 0x00001A44
		public static void SetGUIStyles(Vector2? footerSize = null)
		{
			if (!EditorGUIUtils._additionalStylesSet && footerSize != null)
			{
				EditorGUIUtils._additionalStylesSet = true;
				Vector2 value = footerSize.Value;
				EditorGUIUtils.btImgStyle = new GUIStyle(GUI.skin.button);
				EditorGUIUtils.btImgStyle.normal.background = null;
				EditorGUIUtils.btImgStyle.imagePosition = (ImagePosition) 2;
				EditorGUIUtils.btImgStyle.padding = new RectOffset(0, 0, 0, 0);
				EditorGUIUtils.btImgStyle.fixedHeight = value.y;
			}
			if (!EditorGUIUtils._stylesSet)
			{
				EditorGUIUtils._stylesSet = true;
				EditorGUIUtils.boldLabelStyle = new GUIStyle(GUI.skin.label);
				EditorGUIUtils.boldLabelStyle.fontStyle = (FontStyle) 1;
				EditorGUIUtils.redLabelStyle = new GUIStyle(GUI.skin.label);
				EditorGUIUtils.redLabelStyle.normal.textColor = Color.red;
				EditorGUIUtils.setupLabelStyle = new GUIStyle(EditorGUIUtils.boldLabelStyle);
				EditorGUIUtils.setupLabelStyle.alignment = (TextAnchor) 4;
				EditorGUIUtils.wrapCenterLabelStyle = new GUIStyle(GUI.skin.label);
				EditorGUIUtils.wrapCenterLabelStyle.wordWrap = true;
				EditorGUIUtils.wrapCenterLabelStyle.alignment = (TextAnchor) 4;
				EditorGUIUtils.btBigStyle = new GUIStyle(GUI.skin.button);
				EditorGUIUtils.btBigStyle.padding = new RectOffset(0, 0, 10, 10);
				EditorGUIUtils.btSetup = new GUIStyle(EditorGUIUtils.btBigStyle);
				EditorGUIUtils.btSetup.padding = new RectOffset(10, 10, 6, 6);
				EditorGUIUtils.btSetup.wordWrap = true;
				EditorGUIUtils.btSetup.richText = true;
				EditorGUIUtils.titleStyle = new GUIStyle(GUI.skin.label)
				{
					fontSize = 12,
					fontStyle = (FontStyle) 1
				};
				EditorGUIUtils.handlelabelStyle = new GUIStyle(GUI.skin.label)
				{
					normal = 
					{
						textColor = Color.white
					},
					alignment = (TextAnchor) 3
				};
				EditorGUIUtils.handleSelectedLabelStyle = new GUIStyle(EditorGUIUtils.handlelabelStyle)
				{
					normal = 
					{
						textColor = Color.yellow
					},
					fontStyle = (FontStyle) 1
				};
				EditorGUIUtils.wordWrapLabelStyle = new GUIStyle(GUI.skin.label);
				EditorGUIUtils.wordWrapLabelStyle.wordWrap = true;
				EditorGUIUtils.wordWrapRichTextLabelStyle = new GUIStyle(GUI.skin.label);
				EditorGUIUtils.wordWrapRichTextLabelStyle.wordWrap = true;
				EditorGUIUtils.wordWrapRichTextLabelStyle.richText = true;
				EditorGUIUtils.wordWrapItalicLabelStyle = new GUIStyle(EditorGUIUtils.wordWrapLabelStyle);
				EditorGUIUtils.wordWrapItalicLabelStyle.fontStyle = (FontStyle) 2;
				EditorGUIUtils.logoIconStyle = new GUIStyle(GUI.skin.box);
				EditorGUIUtils.logoIconStyle.active.background = (EditorGUIUtils.logoIconStyle.normal.background = null);
				EditorGUIUtils.logoIconStyle.margin = new RectOffset(0, 0, 0, 0);
				EditorGUIUtils.logoIconStyle.padding = new RectOffset(0, 0, 0, 0);
				EditorGUIUtils.sideBtStyle = new GUIStyle(GUI.skin.button);
				EditorGUIUtils.sideBtStyle.margin.top = 1;
				EditorGUIUtils.sideBtStyle.padding = new RectOffset(0, 0, 2, 2);
				EditorGUIUtils.sideLogoIconBoldLabelStyle = new GUIStyle(EditorGUIUtils.boldLabelStyle);
				EditorGUIUtils.sideLogoIconBoldLabelStyle.alignment = (TextAnchor) 3;
				EditorGUIUtils.sideLogoIconBoldLabelStyle.padding.top = 2;
				EditorGUIUtils.wordWrapTextArea = new GUIStyle(GUI.skin.textArea);
				EditorGUIUtils.wordWrapTextArea.wordWrap = true;
				EditorGUIUtils.popupButton = new GUIStyle(EditorStyles.popup);
				EditorGUIUtils.popupButton.fixedHeight = 18f;
				EditorGUIUtils.popupButton.margin.top++;
				EditorGUIUtils.btIconStyle = new GUIStyle(GUI.skin.button);
				EditorGUIUtils.btIconStyle.padding.left -= 2;
				EditorGUIUtils.btIconStyle.fixedWidth = 24f;
				EditorGUIUtils.btIconStyle.stretchWidth = false;
				EditorGUIUtils.infoboxStyle = new GUIStyle(GUI.skin.box)
				{
					alignment = 0,
					richText = true,
					wordWrap = true,
					padding = new RectOffset(5, 5, 5, 6),
					normal = 
					{
						textColor = Color.white,
						background = Texture2D.whiteTexture
					}
				};
			}
		}

		// Token: 0x04000032 RID: 50
		private static bool _stylesSet;

		// Token: 0x04000033 RID: 51
		private static bool _additionalStylesSet;

		// Token: 0x04000034 RID: 52
		public static GUIStyle boldLabelStyle;

		// Token: 0x04000035 RID: 53
		public static GUIStyle setupLabelStyle;

		// Token: 0x04000036 RID: 54
		public static GUIStyle redLabelStyle;

		// Token: 0x04000037 RID: 55
		public static GUIStyle btBigStyle;

		// Token: 0x04000038 RID: 56
		public static GUIStyle btSetup;

		// Token: 0x04000039 RID: 57
		public static GUIStyle btImgStyle;

		// Token: 0x0400003A RID: 58
		public static GUIStyle wrapCenterLabelStyle;

		// Token: 0x0400003B RID: 59
		public static GUIStyle handlelabelStyle;

		// Token: 0x0400003C RID: 60
		public static GUIStyle handleSelectedLabelStyle;

		// Token: 0x0400003D RID: 61
		public static GUIStyle wordWrapLabelStyle;

		// Token: 0x0400003E RID: 62
		public static GUIStyle wordWrapRichTextLabelStyle;

		// Token: 0x0400003F RID: 63
		public static GUIStyle wordWrapItalicLabelStyle;

		// Token: 0x04000040 RID: 64
		public static GUIStyle titleStyle;

		// Token: 0x04000041 RID: 65
		public static GUIStyle logoIconStyle;

		// Token: 0x04000042 RID: 66
		public static GUIStyle sideBtStyle;

		// Token: 0x04000043 RID: 67
		public static GUIStyle sideLogoIconBoldLabelStyle;

		// Token: 0x04000044 RID: 68
		public static GUIStyle wordWrapTextArea;

		// Token: 0x04000045 RID: 69
		public static GUIStyle popupButton;

		// Token: 0x04000046 RID: 70
		public static GUIStyle btIconStyle;

		// Token: 0x04000047 RID: 71
		public static GUIStyle infoboxStyle;

		// Token: 0x04000048 RID: 72
		private static Texture2D _logo;

		// Token: 0x04000049 RID: 73
		public static readonly string[] FilteredEaseTypes = new string[]
		{
			"Linear",
			"InSine",
			"OutSine",
			"InOutSine",
			"InQuad",
			"OutQuad",
			"InOutQuad",
			"InCubic",
			"OutCubic",
			"InOutCubic",
			"InQuart",
			"OutQuart",
			"InOutQuart",
			"InQuint",
			"OutQuint",
			"InOutQuint",
			"InExpo",
			"OutExpo",
			"InOutExpo",
			"InCirc",
			"OutCirc",
			"InOutCirc",
			"InElastic",
			"OutElastic",
			"InOutElastic",
			"InBack",
			"OutBack",
			"InOutBack",
			"InBounce",
			"OutBounce",
			"InOutBounce",
			"Flash",
			"InFlash",
			"OutFlash",
			"InOutFlash",
			":: AnimationCurve"
		};
	}
}
