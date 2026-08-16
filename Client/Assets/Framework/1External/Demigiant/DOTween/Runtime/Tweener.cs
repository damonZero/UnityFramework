using System;
using System.Text;
using DG.Tweening.Core;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace DG.Tweening
{
    // Token: 0x0200001A RID: 26
    public abstract class Tweener : Tween
    {
        // Token: 0x0600017C RID: 380 RVA: 0x0000842C File Offset: 0x0000662C
        internal Tweener()
        {
        }

        // Token: 0x0600017D RID: 381
        public abstract Tweener ChangeStartValue(object newStartValue, float newDuration = -1f);

        // Token: 0x0600017E RID: 382
        public abstract Tweener ChangeEndValue(object newEndValue, float newDuration = -1f,
            bool snapStartValue = false);

        // Token: 0x0600017F RID: 383
        public abstract Tweener ChangeEndValue(object newEndValue, bool snapStartValue);

        // Token: 0x06000180 RID: 384
        public abstract Tweener ChangeValues(object newStartValue, object newEndValue, float newDuration = -1f);

        // Token: 0x06000181 RID: 385
        internal abstract Tweener SetFrom(bool relative);

        // Token: 0x06000182 RID: 386 RVA: 0x0000843C File Offset: 0x0000663C
        internal static bool Setup<T1, T2, TPlugOptions>(TweenerCore<T1, T2, TPlugOptions> t, DOGetter<T1> getter,
            DOSetter<T1> setter, T2 endValue, float duration, ABSTweenPlugin<T1, T2, TPlugOptions> plugin = null)
            where TPlugOptions : struct, IPlugOptions
        {
            if (plugin != null)
            {
                t.tweenPlugin = plugin;
            }
            else
            {
                if (t.tweenPlugin == null)
                {
                    t.tweenPlugin = PluginsManager.GetDefaultPlugin<T1, T2, TPlugOptions>();
                }

                if (t.tweenPlugin == null)
                {
                    Debugger.LogError("No suitable plugin found for this type");
                    return false;
                }
            }

            t.getter = getter;
            t.setter = setter;
            t.endValue = endValue;
            t.duration = duration;
            t.autoKill = DOTween.defaultAutoKill;
            t.isRecyclable = DOTween.defaultRecyclable;
            t.easeType = DOTween.defaultEaseType;
            t.easeOvershootOrAmplitude = DOTween.defaultEaseOvershootOrAmplitude;
            t.easePeriod = DOTween.defaultEasePeriod;
            t.loopType = DOTween.defaultLoopType;
            t.isPlaying = (DOTween.defaultAutoPlay == AutoPlay.All ||
                           DOTween.defaultAutoPlay == AutoPlay.AutoPlayTweeners);
            return true;
        }

        // Token: 0x06000183 RID: 387 RVA: 0x000084F8 File Offset: 0x000066F8
        internal static float DoUpdateDelay<T1, T2, TPlugOptions>(TweenerCore<T1, T2, TPlugOptions> t, float elapsed)
            where TPlugOptions : struct, IPlugOptions
        {
            float delay = t.delay;
            if (elapsed > delay)
            {
                t.elapsedDelay = delay;
                t.delayComplete = true;
                return elapsed - delay;
            }

            t.elapsedDelay = elapsed;
            return 0f;
        }

        // Token: 0x06000184 RID: 388 RVA: 0x00008530 File Offset: 0x00006730
        internal static bool DoStartup<T1, T2, TPlugOptions>(TweenerCore<T1, T2, TPlugOptions> t)
            where TPlugOptions : struct, IPlugOptions
        {
            t.startupDone = true;
            if (t.specialStartupMode != SpecialStartupMode.None && !Tweener.DOStartupSpecials<T1, T2, TPlugOptions>(t))
            {
                return false;
            }

            if (!t.hasManuallySetStartValue)
            {
                if (DOTween.useSafeMode)
                {
                    try
                    {
                        t.startValue = t.tweenPlugin.ConvertToStartValue(t, t.getter());
                        goto IL_98;
                    }
                    catch (Exception ex)
                    {
                        // if (Debugger.logPriority >= 1)
                        // {
                        // }
                        //因为报错，没有堆栈信息，不好查到底是哪一个Tween动画出了问题，这里加个创建堆栈的输出
                        var str = new StringBuilder();
                        str.Append("\ntween type: " + t.GetType().Name);
                        str.Append("\ntarget: " + t.target);
                        str.Append("\nduration: " + t.duration);
                        str.Append("\ntraceback: " + t.creatTraceback);

                        Debugger.LogError(string.Format(
                            "{0}\n\n Tween startup failed (NULL target/property - {1}): the tween will now be killed ► {2}",
                            str, ex.TargetSite, ex.Message));

                        DOTween.safeModeReport.Add(SafeModeReport.SafeModeReportType.StartupFailure);
                        return false;
                    }
                }

                t.startValue = t.tweenPlugin.ConvertToStartValue(t, t.getter());
            }

            IL_98:
            if (t.isRelative)
            {
                t.tweenPlugin.SetRelativeEndValue(t);
            }

            t.tweenPlugin.SetChangeValue(t);
            Tweener.DOStartupDurationBased<T1, T2, TPlugOptions>(t);
            if (t.duration <= 0f)
            {
                t.easeType = Ease.INTERNAL_Zero;
            }

            return true;
        }

        // Token: 0x06000185 RID: 389 RVA: 0x00008624 File Offset: 0x00006824
        internal static TweenerCore<T1, T2, TPlugOptions> DoChangeStartValue<T1, T2, TPlugOptions>(
            TweenerCore<T1, T2, TPlugOptions> t, T2 newStartValue, float newDuration)
            where TPlugOptions : struct, IPlugOptions
        {
            t.hasManuallySetStartValue = true;
            t.startValue = newStartValue;
            if (t.startupDone)
            {
                if (t.specialStartupMode != SpecialStartupMode.None &&
                    !Tweener.DOStartupSpecials<T1, T2, TPlugOptions>(t))
                {
                    return null;
                }

                t.tweenPlugin.SetChangeValue(t);
            }

            if (newDuration > 0f)
            {
                t.duration = newDuration;
                if (t.startupDone)
                {
                    Tweener.DOStartupDurationBased<T1, T2, TPlugOptions>(t);
                }
            }

            Tween.DoGoto(t, 0f, 0, UpdateMode.IgnoreOnUpdate);
            return t;
        }

        // Token: 0x06000186 RID: 390 RVA: 0x00008694 File Offset: 0x00006894
        internal static TweenerCore<T1, T2, TPlugOptions> DoChangeEndValue<T1, T2, TPlugOptions>(
            TweenerCore<T1, T2, TPlugOptions> t, T2 newEndValue, float newDuration, bool snapStartValue)
            where TPlugOptions : struct, IPlugOptions
        {
            t.endValue = newEndValue;
            t.isRelative = false;
            if (t.startupDone)
            {
                if (t.specialStartupMode != SpecialStartupMode.None &&
                    !Tweener.DOStartupSpecials<T1, T2, TPlugOptions>(t))
                {
                    return null;
                }

                if (snapStartValue)
                {
                    if (DOTween.useSafeMode)
                    {
                        try
                        {
                            t.startValue = t.tweenPlugin.ConvertToStartValue(t, t.getter());
                            goto IL_B5;
                        }
                        catch (Exception ex)
                        {
                            // if (Debugger.logPriority >= 1)
                            // {
                            // }
                            //因为报错，没有堆栈信息，不好查到底是哪一个Tween动画出了问题，这里加个创建堆栈的输出
                            var str = new StringBuilder();
                            str.Append("\ntween type: " + t.GetType().Name);
                            str.Append("\ntarget: " + t.target);
                            str.Append("\nduration: " + t.duration);
                            str.Append("\ntraceback: " + t.creatTraceback);
                            Debugger.LogError(string.Format(
                                "{0}\n\n  Target or field is missing/null ({1}) ► {2}\n\n{3}\n\n", str,
                                ex.TargetSite, ex.Message, ex.StackTrace));

                            TweenManager.Despawn(t, true);
                            DOTween.safeModeReport.Add(SafeModeReport.SafeModeReportType.TargetOrFieldMissing);
                            return null;
                        }
                    }

                    t.startValue = t.tweenPlugin.ConvertToStartValue(t, t.getter());
                }

                IL_B5:
                t.tweenPlugin.SetChangeValue(t);
            }

            if (newDuration > 0f)
            {
                t.duration = newDuration;
                if (t.startupDone)
                {
                    Tweener.DOStartupDurationBased<T1, T2, TPlugOptions>(t);
                }
            }

            Tween.DoGoto(t, 0f, 0, UpdateMode.IgnoreOnUpdate);
            return t;
        }

        // Token: 0x06000187 RID: 391 RVA: 0x000087A0 File Offset: 0x000069A0
        internal static TweenerCore<T1, T2, TPlugOptions> DoChangeValues<T1, T2, TPlugOptions>(
            TweenerCore<T1, T2, TPlugOptions> t, T2 newStartValue, T2 newEndValue, float newDuration)
            where TPlugOptions : struct, IPlugOptions
        {
            t.hasManuallySetStartValue = true;
            t.isRelative = (t.isFrom = false);
            t.startValue = newStartValue;
            t.endValue = newEndValue;
            if (t.startupDone)
            {
                if (t.specialStartupMode != SpecialStartupMode.None &&
                    !Tweener.DOStartupSpecials<T1, T2, TPlugOptions>(t))
                {
                    return null;
                }

                t.tweenPlugin.SetChangeValue(t);
            }

            if (newDuration > 0f)
            {
                t.duration = newDuration;
                if (t.startupDone)
                {
                    Tweener.DOStartupDurationBased<T1, T2, TPlugOptions>(t);
                }
            }

            Tween.DoGoto(t, 0f, 0, UpdateMode.IgnoreOnUpdate);
            return t;
        }

        // Token: 0x06000188 RID: 392 RVA: 0x00008824 File Offset: 0x00006A24
        private static bool DOStartupSpecials<T1, T2, TPlugOptions>(TweenerCore<T1, T2, TPlugOptions> t)
            where TPlugOptions : struct, IPlugOptions
        {
            bool result;
            try
            {
                switch (t.specialStartupMode)
                {
                    case SpecialStartupMode.SetLookAt:
                        if (!SpecialPluginsUtils.SetLookAt(t as TweenerCore<Quaternion, Vector3, QuaternionOptions>))
                        {
                            return false;
                        }

                        break;
                    case SpecialStartupMode.SetShake:
                        if (!SpecialPluginsUtils.SetShake(t as TweenerCore<Vector3, Vector3[], Vector3ArrayOptions>))
                        {
                            return false;
                        }

                        break;
                    case SpecialStartupMode.SetPunch:
                        if (!SpecialPluginsUtils.SetPunch(t as TweenerCore<Vector3, Vector3[], Vector3ArrayOptions>))
                        {
                            return false;
                        }

                        break;
                    case SpecialStartupMode.SetCameraShakePosition:
                        if (!SpecialPluginsUtils.SetCameraShakePosition(
                            t as TweenerCore<Vector3, Vector3[], Vector3ArrayOptions>))
                        {
                            return false;
                        }

                        break;
                }

                result = true;
            }
            catch
            {
                result = false;
            }

            return result;
        }

        // Token: 0x06000189 RID: 393 RVA: 0x000088B0 File Offset: 0x00006AB0
        private static void DOStartupDurationBased<T1, T2, TPlugOptions>(TweenerCore<T1, T2, TPlugOptions> t)
            where TPlugOptions : struct, IPlugOptions
        {
            if (t.isSpeedBased)
            {
                t.duration = t.tweenPlugin.GetSpeedBasedDuration(t.plugOptions, t.duration, t.changeValue);
            }

            t.fullDuration = ((t.loops > -1) ? (t.duration * (float) t.loops) : float.PositiveInfinity);
        }

        // Token: 0x040000C8 RID: 200
        internal bool hasManuallySetStartValue;

        // Token: 0x040000C9 RID: 201
        internal bool isFromAllowed = true;
    }
}