using System;
using System.Globalization;
using UnityEngine;

namespace DG.DOTweenEditor
{
	// Token: 0x02000005 RID: 5
	internal static class EditorVersion
	{
		// Token: 0x06000018 RID: 24 RVA: 0x00002804 File Offset: 0x00000A04
		static EditorVersion()
		{
			string unityVersion = Application.unityVersion;
			int num = unityVersion.IndexOf('.');
			if (num == -1)
			{
				EditorVersion.MajorVersion = int.Parse(unityVersion);
				EditorVersion.Version = (float)EditorVersion.MajorVersion;
				return;
			}
			string text = unityVersion.Substring(0, num);
			EditorVersion.MajorVersion = int.Parse(text);
			string text2 = unityVersion.Substring(num + 1);
			num = text2.IndexOf('.');
			if (num != -1)
			{
				text2 = text2.Substring(0, num);
			}
			EditorVersion.MinorVersion = int.Parse(text2);
			if (!float.TryParse(text + "." + text2, NumberStyles.Float, CultureInfo.InvariantCulture, out EditorVersion.Version))
			{
				Debug.LogWarning(string.Format("DOTweenEditor.EditorVersion ► Error when detecting Unity Version from \"{0}.{1}\"", text, text2));
				EditorVersion.Version = 2018.3f;
			}
		}

		/// <summary>Full major version + first minor version (ex: 2018.1f)</summary>
		// Token: 0x04000012 RID: 18
		public static readonly float Version;

		/// <summary>Major version</summary>
		// Token: 0x04000013 RID: 19
		public static readonly int MajorVersion;

		/// <summary>First minor version (ex: in 2018.1 it would be 1)</summary>
		// Token: 0x04000014 RID: 20
		public static readonly int MinorVersion;
	}
}
