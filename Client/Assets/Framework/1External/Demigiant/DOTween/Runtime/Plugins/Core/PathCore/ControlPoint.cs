using System;
using UnityEngine;

namespace DG.Tweening.Plugins.Core.PathCore
{
	// Token: 0x02000042 RID: 66
	[Serializable]
	public struct ControlPoint
	{
		// Token: 0x06000251 RID: 593 RVA: 0x0000D97A File Offset: 0x0000BB7A
		public ControlPoint(Vector3 a, Vector3 b)
		{
			this.a = a;
			this.b = b;
		}

		// Token: 0x06000252 RID: 594 RVA: 0x0000D98A File Offset: 0x0000BB8A
		public static ControlPoint operator +(ControlPoint cp, Vector3 v)
		{
			return new ControlPoint(cp.a + v, cp.b + v);
		}

		// Token: 0x06000253 RID: 595 RVA: 0x0000D9AC File Offset: 0x0000BBAC
		public override string ToString()
		{
			return string.Concat(new string[]
			{
				"[",
				this.a.ToString(),
				" | ",
				this.b.ToString(),
				"]"
			});
		}

		// Token: 0x04000118 RID: 280
		public Vector3 a;

		// Token: 0x04000119 RID: 281
		public Vector3 b;
	}
}
