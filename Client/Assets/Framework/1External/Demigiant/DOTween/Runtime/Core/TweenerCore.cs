using System;
using System.Text;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;

namespace DG.Tweening.Core
{
	// Token: 0x02000055 RID: 85
	public class TweenerCore<T1, T2, TPlugOptions> : Tweener where TPlugOptions : struct, IPlugOptions
	{
		// Token: 0x060002E8 RID: 744 RVA: 0x00011110 File Offset: 0x0000F310
		internal TweenerCore()
		{
			this.typeofT1 = typeof(T1);
			this.typeofT2 = typeof(T2);
			this.typeofTPlugOptions = typeof(TPlugOptions);
			this.tweenType = TweenType.Tweener;
			this.Reset();
		}

		// Token: 0x060002E9 RID: 745 RVA: 0x00011160 File Offset: 0x0000F360
		public override Tweener ChangeStartValue(object newStartValue, float newDuration = -1f)
		{
			if (this.isSequenced)
			{
				if (Debugger.logPriority >= 1)
				{
					Debugger.LogWarning("You cannot change the values of a tween contained inside a Sequence", this);
				}
				return this;
			}
			Type type = newStartValue.GetType();
			if (type != this.typeofT2)
			{
				if (Debugger.logPriority >= 1)
				{
					string[] array = new string[5];
					array[0] = "ChangeStartValue: incorrect newStartValue type (is ";
					int num = 1;
					Type type2 = type;
					array[num] = ((type2 != null) ? type2.ToString() : null);
					array[2] = ", should be ";
					int num2 = 3;
					Type typeofT = this.typeofT2;
					array[num2] = ((typeofT != null) ? typeofT.ToString() : null);
					array[4] = ")";
					Debugger.LogWarning(string.Concat(array), this);
				}
				return this;
			}
			return Tweener.DoChangeStartValue<T1, T2, TPlugOptions>(this, (T2)((object)newStartValue), newDuration);
		}

		// Token: 0x060002EA RID: 746 RVA: 0x000111FF File Offset: 0x0000F3FF
		public override Tweener ChangeEndValue(object newEndValue, bool snapStartValue)
		{
			return this.ChangeEndValue(newEndValue, -1f, snapStartValue);
		}

		// Token: 0x060002EB RID: 747 RVA: 0x00011210 File Offset: 0x0000F410
		public override Tweener ChangeEndValue(object newEndValue, float newDuration = -1f, bool snapStartValue = false)
		{
			if (this.isSequenced)
			{
				if (Debugger.logPriority >= 1)
				{
					Debugger.LogWarning("You cannot change the values of a tween contained inside a Sequence", this);
				}
				return this;
			}
			Type type = newEndValue.GetType();
			if (type != this.typeofT2)
			{
				if (Debugger.logPriority >= 1)
				{
					string[] array = new string[5];
					array[0] = "ChangeEndValue: incorrect newEndValue type (is ";
					int num = 1;
					Type type2 = type;
					array[num] = ((type2 != null) ? type2.ToString() : null);
					array[2] = ", should be ";
					int num2 = 3;
					Type typeofT = this.typeofT2;
					array[num2] = ((typeofT != null) ? typeofT.ToString() : null);
					array[4] = ")";
					Debugger.LogWarning(string.Concat(array), this);
				}
				return this;
			}
			return Tweener.DoChangeEndValue<T1, T2, TPlugOptions>(this, (T2)((object)newEndValue), newDuration, snapStartValue);
		}

		// Token: 0x060002EC RID: 748 RVA: 0x000112B0 File Offset: 0x0000F4B0
		public override Tweener ChangeValues(object newStartValue, object newEndValue, float newDuration = -1f)
		{
			if (this.isSequenced)
			{
				if (Debugger.logPriority >= 1)
				{
					Debugger.LogWarning("You cannot change the values of a tween contained inside a Sequence", this);
				}
				return this;
			}
			Type type = newStartValue.GetType();
			Type type2 = newEndValue.GetType();
			if (type != this.typeofT2)
			{
				if (Debugger.logPriority >= 1)
				{
					string[] array = new string[5];
					array[0] = "ChangeValues: incorrect value type (is ";
					int num = 1;
					Type type3 = type;
					array[num] = ((type3 != null) ? type3.ToString() : null);
					array[2] = ", should be ";
					int num2 = 3;
					Type typeofT = this.typeofT2;
					array[num2] = ((typeofT != null) ? typeofT.ToString() : null);
					array[4] = ")";
					Debugger.LogWarning(string.Concat(array), this);
				}
				return this;
			}
			if (type2 != this.typeofT2)
			{
				if (Debugger.logPriority >= 1)
				{
					string[] array2 = new string[5];
					array2[0] = "ChangeValues: incorrect value type (is ";
					int num3 = 1;
					Type type4 = type2;
					array2[num3] = ((type4 != null) ? type4.ToString() : null);
					array2[2] = ", should be ";
					int num4 = 3;
					Type typeofT2 = this.typeofT2;
					array2[num4] = ((typeofT2 != null) ? typeofT2.ToString() : null);
					array2[4] = ")";
					Debugger.LogWarning(string.Concat(array2), this);
				}
				return this;
			}
			return Tweener.DoChangeValues<T1, T2, TPlugOptions>(this, (T2)((object)newStartValue), (T2)((object)newEndValue), newDuration);
		}

		// Token: 0x060002ED RID: 749 RVA: 0x000113BD File Offset: 0x0000F5BD
		public TweenerCore<T1, T2, TPlugOptions> ChangeStartValue(T2 newStartValue, float newDuration = -1f)
		{
			if (this.isSequenced)
			{
				if (Debugger.logPriority >= 1)
				{
					Debugger.LogWarning("You cannot change the values of a tween contained inside a Sequence", this);
				}
				return this;
			}
			return Tweener.DoChangeStartValue<T1, T2, TPlugOptions>(this, newStartValue, newDuration);
		}

		// Token: 0x060002EE RID: 750 RVA: 0x000113E4 File Offset: 0x0000F5E4
		public TweenerCore<T1, T2, TPlugOptions> ChangeEndValue(T2 newEndValue, bool snapStartValue)
		{
			return this.ChangeEndValue(newEndValue, -1f, snapStartValue);
		}

		// Token: 0x060002EF RID: 751 RVA: 0x000113F3 File Offset: 0x0000F5F3
		public TweenerCore<T1, T2, TPlugOptions> ChangeEndValue(T2 newEndValue, float newDuration = -1f, bool snapStartValue = false)
		{
			if (this.isSequenced)
			{
				if (Debugger.logPriority >= 1)
				{
					Debugger.LogWarning("You cannot change the values of a tween contained inside a Sequence", this);
				}
				return this;
			}
			return Tweener.DoChangeEndValue<T1, T2, TPlugOptions>(this, newEndValue, newDuration, snapStartValue);
		}

		// Token: 0x060002F0 RID: 752 RVA: 0x0001141B File Offset: 0x0000F61B
		public TweenerCore<T1, T2, TPlugOptions> ChangeValues(T2 newStartValue, T2 newEndValue, float newDuration = -1f)
		{
			if (this.isSequenced)
			{
				if (Debugger.logPriority >= 1)
				{
					Debugger.LogWarning("You cannot change the values of a tween contained inside a Sequence", this);
				}
				return this;
			}
			return Tweener.DoChangeValues<T1, T2, TPlugOptions>(this, newStartValue, newEndValue, newDuration);
		}

		// Token: 0x060002F1 RID: 753 RVA: 0x00011443 File Offset: 0x0000F643
		internal override Tweener SetFrom(bool relative)
		{
			this.tweenPlugin.SetFrom(this, relative);
			this.hasManuallySetStartValue = true;
			return this;
		}

		// Token: 0x060002F2 RID: 754 RVA: 0x0001145A File Offset: 0x0000F65A
		internal Tweener SetFrom(T2 fromValue, bool setImmediately, bool relative)
		{
			this.tweenPlugin.SetFrom(this, fromValue, setImmediately, relative);
			this.hasManuallySetStartValue = true;
			return this;
		}

		// Token: 0x060002F3 RID: 755 RVA: 0x00011474 File Offset: 0x0000F674
		internal sealed override void Reset()
		{
			base.Reset();
			if (this.tweenPlugin != null)
			{
				this.tweenPlugin.Reset(this);
			}
			this.plugOptions.Reset();
			this.getter = null;
			this.setter = null;
			this.hasManuallySetStartValue = false;
			this.isFromAllowed = true;
		}

		// Token: 0x060002F4 RID: 756 RVA: 0x000114C8 File Offset: 0x0000F6C8
		internal override bool Validate()
		{
			try
			{
				this.getter();
			}
			catch
			{
				return false;
			}
			return true;
		}

		// Token: 0x060002F5 RID: 757 RVA: 0x000114FC File Offset: 0x0000F6FC
		internal override float UpdateDelay(float elapsed)
		{
			return Tweener.DoUpdateDelay<T1, T2, TPlugOptions>(this, elapsed);
		}

		// Token: 0x060002F6 RID: 758 RVA: 0x00011505 File Offset: 0x0000F705
		internal override bool Startup()
		{
			return Tweener.DoStartup<T1, T2, TPlugOptions>(this);
		}

		// Token: 0x060002F7 RID: 759 RVA: 0x00011510 File Offset: 0x0000F710
		internal override bool ApplyTween(float prevPosition, int prevCompletedLoops, int newCompletedSteps, bool useInversePosition, UpdateMode updateMode, UpdateNotice updateNotice)
		{
			float elapsed = useInversePosition ? (this.duration - base.position) : base.position;
			if (DOTween.useSafeMode)
			{
				try
				{
					this.tweenPlugin.EvaluateAndApply(this.plugOptions, this, base.isRelative, this.getter, this.setter, elapsed, this.startValue, this.changeValue, this.duration, useInversePosition, updateNotice);
					return false;
				}
				catch (Exception ex)
				{
					if (Debugger.logPriority >= 1)
					{
						var str = new StringBuilder();
						str.Append("\ntween type: " + GetType().Name);
						str.Append("\ntarget: " + target);
						str.Append("\nduration: " + duration);
						str.Append("\ntraceback: " + creatTraceback);
						Debugger.LogError(string.Format(
							"{0}\n\n  Target or field is missing/null ({1}) ► {2}\n\n{3}\n\n", str,
							ex.TargetSite, ex.Message, ex.StackTrace));
					}
					DOTween.safeModeReport.Add(SafeModeReport.SafeModeReportType.TargetOrFieldMissing);
					return true;
				}
			}
			this.tweenPlugin.EvaluateAndApply(this.plugOptions, this, base.isRelative, this.getter, this.setter, elapsed, this.startValue, this.changeValue, this.duration, useInversePosition, updateNotice);
			return false;
		}

		// Token: 0x0400018D RID: 397
		public T2 startValue;

		// Token: 0x0400018E RID: 398
		public T2 endValue;

		// Token: 0x0400018F RID: 399
		public T2 changeValue;

		// Token: 0x04000190 RID: 400
		public TPlugOptions plugOptions;

		// Token: 0x04000191 RID: 401
		public DOGetter<T1> getter;

		// Token: 0x04000192 RID: 402
		public DOSetter<T1> setter;

		// Token: 0x04000193 RID: 403
		internal ABSTweenPlugin<T1, T2, TPlugOptions> tweenPlugin;

		// Token: 0x04000194 RID: 404
		private const string _TxtCantChangeSequencedValues = "You cannot change the values of a tween contained inside a Sequence";
	}
}
