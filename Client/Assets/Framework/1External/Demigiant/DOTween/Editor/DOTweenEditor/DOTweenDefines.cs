using System;

namespace DG.DOTweenEditor
{
	/// <summary>
	/// Not used as menu item anymore, but as a utiity function
	/// </summary>
	// Token: 0x0200000A RID: 10
	internal static class DOTweenDefines
	{
		// Token: 0x0600004A RID: 74 RVA: 0x00003649 File Offset: 0x00001849
		public static void RemoveAllDefines()
		{
		}

		// Token: 0x0600004B RID: 75 RVA: 0x0000364C File Offset: 0x0000184C
		public static void RemoveAllLegacyDefines()
		{
			EditorUtils.RemoveGlobalDefine("DOTAUDIO");
			EditorUtils.RemoveGlobalDefine("DOTPHYSICS");
			EditorUtils.RemoveGlobalDefine("DOTPHYSICS2D");
			EditorUtils.RemoveGlobalDefine("DOTSPRITE");
			EditorUtils.RemoveGlobalDefine("DOTUI");
			EditorUtils.RemoveGlobalDefine("DOTWEEN_NORBODY");
			EditorUtils.RemoveGlobalDefine("DOTWEEN_TK2D");
			EditorUtils.RemoveGlobalDefine("DOTWEEN_TMP");
		}

		// Token: 0x0400002A RID: 42
		public const string GlobalDefine_Legacy_AudioModule = "DOTAUDIO";

		// Token: 0x0400002B RID: 43
		public const string GlobalDefine_Legacy_PhysicsModule = "DOTPHYSICS";

		// Token: 0x0400002C RID: 44
		public const string GlobalDefine_Legacy_Physics2DModule = "DOTPHYSICS2D";

		// Token: 0x0400002D RID: 45
		public const string GlobalDefine_Legacy_SpriteModule = "DOTSPRITE";

		// Token: 0x0400002E RID: 46
		public const string GlobalDefine_Legacy_UIModule = "DOTUI";

		// Token: 0x0400002F RID: 47
		public const string GlobalDefine_Legacy_TK2D = "DOTWEEN_TK2D";

		// Token: 0x04000030 RID: 48
		public const string GlobalDefine_Legacy_TextMeshPro = "DOTWEEN_TMP";

		// Token: 0x04000031 RID: 49
		public const string GlobalDefine_Legacy_NoRigidbody = "DOTWEEN_NORBODY";
	}
}
