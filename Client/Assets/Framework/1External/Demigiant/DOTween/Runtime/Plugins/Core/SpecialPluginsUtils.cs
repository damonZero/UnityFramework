using System;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace DG.Tweening.Plugins.Core
{
	// Token: 0x0200003C RID: 60
	internal static class SpecialPluginsUtils
	{
		// Token: 0x06000237 RID: 567 RVA: 0x0000CFB4 File Offset: 0x0000B1B4
		internal static bool SetLookAt(TweenerCore<Quaternion, Vector3, QuaternionOptions> t)
		{
			Transform transform = t.target as Transform;
			Vector3 vector = t.endValue;
			vector -= transform.position;
			AxisConstraint axisConstraint = t.plugOptions.axisConstraint;
			if (axisConstraint != AxisConstraint.X)
			{
				if (axisConstraint != AxisConstraint.Y)
				{
					if (axisConstraint == AxisConstraint.Z)
					{
						vector.z = 0f;
					}
				}
				else
				{
					vector.y = 0f;
				}
			}
			else
			{
				vector.x = 0f;
			}
			Vector3 eulerAngles = Quaternion.LookRotation(vector, t.plugOptions.up).eulerAngles;
			t.endValue = eulerAngles;
			return true;
		}

		// Token: 0x06000238 RID: 568 RVA: 0x0000D048 File Offset: 0x0000B248
		internal static bool SetPunch(TweenerCore<Vector3, Vector3[], Vector3ArrayOptions> t)
		{
			Vector3 vector;
			try
			{
				vector = t.getter();
			}
			catch
			{
				return false;
			}
			t.isRelative = (t.isSpeedBased = false);
			t.easeType = Ease.OutQuad;
			t.customEase = null;
			int num = t.endValue.Length;
			for (int i = 0; i < num; i++)
			{
				t.endValue[i] = t.endValue[i] + vector;
			}
			return true;
		}

		// Token: 0x06000239 RID: 569 RVA: 0x0000D0D4 File Offset: 0x0000B2D4
		internal static bool SetShake(TweenerCore<Vector3, Vector3[], Vector3ArrayOptions> t)
		{
			if (!SpecialPluginsUtils.SetPunch(t))
			{
				return false;
			}
			t.easeType = Ease.Linear;
			return true;
		}

		// Token: 0x0600023A RID: 570 RVA: 0x0000D0E8 File Offset: 0x0000B2E8
		internal static bool SetCameraShakePosition(TweenerCore<Vector3, Vector3[], Vector3ArrayOptions> t)
		{
			if (!SpecialPluginsUtils.SetShake(t))
			{
				return false;
			}
			Camera camera = t.target as Camera;
			if (camera == null)
			{
				return false;
			}
			Vector3 vector = t.getter();
			Transform transform = camera.transform;
			int num = t.endValue.Length;
			for (int i = 0; i < num; i++)
			{
				Vector3 vector2 = t.endValue[i];
				t.endValue[i] = transform.localRotation * (vector2 - vector) + vector;
			}
			return true;
		}
	}
}
