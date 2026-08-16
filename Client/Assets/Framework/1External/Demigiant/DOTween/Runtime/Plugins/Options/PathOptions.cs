using System;
using UnityEngine;

namespace DG.Tweening.Plugins.Options
{
	// Token: 0x02000031 RID: 49
	public struct PathOptions : IPlugOptions
	{
		// Token: 0x0600022D RID: 557 RVA: 0x0000CE74 File Offset: 0x0000B074
		public void Reset()
		{
			this.mode = PathMode.Ignore;
			this.orientType = OrientType.None;
			this.lockPositionAxis = (this.lockRotationAxis = AxisConstraint.None);
			this.isClosedPath = false;
			this.lookAtPosition = Vector3.zero;
			this.lookAtTransform = null;
			this.lookAhead = 0f;
			this.hasCustomForwardDirection = false;
			this.forward = Quaternion.identity;
			this.useLocalPosition = false;
			this.parent = null;
			this.isRigidbody = false;
			this.stableZRotation = false;
			this.startupRot = Quaternion.identity;
			this.startupZRot = 0f;
			this.addedExtraStartWp = (this.addedExtraEndWp = false);
		}

		// Token: 0x040000E1 RID: 225
		public PathMode mode;

		// Token: 0x040000E2 RID: 226
		public OrientType orientType;

		// Token: 0x040000E3 RID: 227
		public AxisConstraint lockPositionAxis;

		// Token: 0x040000E4 RID: 228
		public AxisConstraint lockRotationAxis;

		// Token: 0x040000E5 RID: 229
		public bool isClosedPath;

		// Token: 0x040000E6 RID: 230
		public Vector3 lookAtPosition;

		// Token: 0x040000E7 RID: 231
		public Transform lookAtTransform;

		// Token: 0x040000E8 RID: 232
		public float lookAhead;

		// Token: 0x040000E9 RID: 233
		public bool hasCustomForwardDirection;

		// Token: 0x040000EA RID: 234
		public Quaternion forward;

		// Token: 0x040000EB RID: 235
		public bool useLocalPosition;

		// Token: 0x040000EC RID: 236
		public Transform parent;

		// Token: 0x040000ED RID: 237
		public bool isRigidbody;

		// Token: 0x040000EE RID: 238
		public bool stableZRotation;

		// Token: 0x040000EF RID: 239
		internal Quaternion startupRot;

		// Token: 0x040000F0 RID: 240
		internal float startupZRot;

		// Token: 0x040000F1 RID: 241
		internal bool addedExtraStartWp;

		// Token: 0x040000F2 RID: 242
		internal bool addedExtraEndWp;
	}
}
