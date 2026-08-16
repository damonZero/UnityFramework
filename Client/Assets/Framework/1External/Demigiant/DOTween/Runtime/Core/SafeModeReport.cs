using System;

namespace DG.Tweening.Core
{
	// Token: 0x02000050 RID: 80
	internal struct SafeModeReport
	{
		// Token: 0x1700000A RID: 10
		// (get) Token: 0x060002AC RID: 684 RVA: 0x0000F192 File Offset: 0x0000D392
		// (set) Token: 0x060002AD RID: 685 RVA: 0x0000F19A File Offset: 0x0000D39A
		public int totMissingTargetOrFieldErrors { get; private set; }

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x060002AE RID: 686 RVA: 0x0000F1A3 File Offset: 0x0000D3A3
		// (set) Token: 0x060002AF RID: 687 RVA: 0x0000F1AB File Offset: 0x0000D3AB
		public int totCallbackErrors { get; private set; }

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x060002B0 RID: 688 RVA: 0x0000F1B4 File Offset: 0x0000D3B4
		// (set) Token: 0x060002B1 RID: 689 RVA: 0x0000F1BC File Offset: 0x0000D3BC
		public int totStartupErrors { get; private set; }

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x060002B2 RID: 690 RVA: 0x0000F1C5 File Offset: 0x0000D3C5
		// (set) Token: 0x060002B3 RID: 691 RVA: 0x0000F1CD File Offset: 0x0000D3CD
		public int totUnsetErrors { get; private set; }

		// Token: 0x060002B4 RID: 692 RVA: 0x0000F1D8 File Offset: 0x0000D3D8
		public void Add(SafeModeReport.SafeModeReportType type)
		{
			switch (type)
			{
			case SafeModeReport.SafeModeReportType.TargetOrFieldMissing:
			{
				int num = this.totMissingTargetOrFieldErrors;
				this.totMissingTargetOrFieldErrors = num + 1;
				return;
			}
			case SafeModeReport.SafeModeReportType.Callback:
			{
				int num = this.totCallbackErrors;
				this.totCallbackErrors = num + 1;
				return;
			}
			case SafeModeReport.SafeModeReportType.StartupFailure:
			{
				int num = this.totStartupErrors;
				this.totStartupErrors = num + 1;
				return;
			}
			default:
			{
				int num = this.totUnsetErrors;
				this.totUnsetErrors = num + 1;
				return;
			}
			}
		}

		// Token: 0x060002B5 RID: 693 RVA: 0x0000F23E File Offset: 0x0000D43E
		public int GetTotErrors()
		{
			return this.totMissingTargetOrFieldErrors + this.totCallbackErrors + this.totStartupErrors + this.totUnsetErrors;
		}

		// Token: 0x020000BA RID: 186
		internal enum SafeModeReportType
		{
			// Token: 0x0400025D RID: 605
			Unset,
			// Token: 0x0400025E RID: 606
			TargetOrFieldMissing,
			// Token: 0x0400025F RID: 607
			Callback,
			// Token: 0x04000260 RID: 608
			StartupFailure
		}
	}
}
