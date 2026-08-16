using System;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Options;

namespace DG.Tweening.Core
{
	// Token: 0x0200004E RID: 78
	public static class Extensions
	{
		// Token: 0x060002A6 RID: 678 RVA: 0x0000F0F0 File Offset: 0x0000D2F0
		public static T SetSpecialStartupMode<T>(this T t, SpecialStartupMode mode) where T : Tween
		{
			t.specialStartupMode = mode;
			return t;
		}

		// Token: 0x060002A7 RID: 679 RVA: 0x0000F0FF File Offset: 0x0000D2FF
		public static TweenerCore<T1, T2, TPlugOptions> Blendable<T1, T2, TPlugOptions>(this TweenerCore<T1, T2, TPlugOptions> t) where TPlugOptions : struct, IPlugOptions
		{
			t.isBlendable = true;
			return t;
		}

		// Token: 0x060002A8 RID: 680 RVA: 0x0000F109 File Offset: 0x0000D309
		public static TweenerCore<T1, T2, TPlugOptions> NoFrom<T1, T2, TPlugOptions>(this TweenerCore<T1, T2, TPlugOptions> t) where TPlugOptions : struct, IPlugOptions
		{
			t.isFromAllowed = false;
			return t;
		}
	}
}
