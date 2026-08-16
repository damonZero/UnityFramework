using System;

namespace DG.Tweening.Plugins.Options
{
	// Token: 0x02000039 RID: 57
	public struct StringOptions : IPlugOptions
	{
		// Token: 0x06000235 RID: 565 RVA: 0x0000CF70 File Offset: 0x0000B170
		public void Reset()
		{
			this.richTextEnabled = false;
			this.scrambleMode = ScrambleMode.None;
			this.scrambledChars = null;
			this.startValueStrippedLength = (this.changeValueStrippedLength = 0);
		}

		// Token: 0x040000FD RID: 253
		public bool richTextEnabled;

		// Token: 0x040000FE RID: 254
		public ScrambleMode scrambleMode;

		// Token: 0x040000FF RID: 255
		public char[] scrambledChars;

		// Token: 0x04000100 RID: 256
		internal int startValueStrippedLength;

		// Token: 0x04000101 RID: 257
		internal int changeValueStrippedLength;
	}
}
