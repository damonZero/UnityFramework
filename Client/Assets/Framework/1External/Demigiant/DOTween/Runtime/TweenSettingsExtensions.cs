using System;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Plugins;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace DG.Tweening
{
    // Token: 0x02000017 RID: 23
    public static class TweenSettingsExtensions
    {
        // Token: 0x0600011E RID: 286 RVA: 0x000068F2 File Offset: 0x00004AF2
        public static T SetAutoKill<T>(this T t) where T : Tween
        {
            if (t == null || !t.active || t.creationLocked)
            {
                return t;
            }

            t.autoKill = true;
            return t;
        }

        // Token: 0x0600011F RID: 287 RVA: 0x00006925 File Offset: 0x00004B25
        public static T SetAutoKill<T>(this T t, bool autoKillOnCompletion) where T : Tween
        {
            if (t == null || !t.active || t.creationLocked)
            {
                return t;
            }

            t.autoKill = autoKillOnCompletion;
            return t;
        }

        // Token: 0x06000120 RID: 288 RVA: 0x00006958 File Offset: 0x00004B58
        public static T SetId<T>(this T t, object objectId) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.id = objectId;
            return t;
        }

        // Token: 0x06000121 RID: 289 RVA: 0x0000697E File Offset: 0x00004B7E
        public static T SetId<T>(this T t, string stringId) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.stringId = stringId;
            return t;
        }

        // Token: 0x06000122 RID: 290 RVA: 0x000069A4 File Offset: 0x00004BA4
        public static T SetId<T>(this T t, int intId) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.intId = intId;
            return t;
        }

        // Token: 0x06000123 RID: 291 RVA: 0x000069CC File Offset: 0x00004BCC
        public static T SetLink<T>(this T t, GameObject gameObject) where T : Tween
        {
            if (t == null || !t.active || t.isSequenced || gameObject == null)
            {
                return t;
            }

            TweenManager.AddTweenLink(t, new TweenLink(gameObject, LinkBehaviour.KillOnDestroy));
            return t;
        }

        // Token: 0x06000124 RID: 292 RVA: 0x00006A1C File Offset: 0x00004C1C
        public static T SetLink<T>(this T t, GameObject gameObject, LinkBehaviour behaviour) where T : Tween
        {
            if (t == null || !t.active || t.isSequenced || gameObject == null)
            {
                return t;
            }

            TweenManager.AddTweenLink(t, new TweenLink(gameObject, behaviour));
            return t;
        }

        // Token: 0x06000125 RID: 293 RVA: 0x00006A6C File Offset: 0x00004C6C
        public static T SetTarget<T>(this T t, object target) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            if (DOTween.debugStoreTargetId)
            {
                Component component = target as Component;
                t.debugTargetId = ((component != null) ? component.name : target.ToString());
            }

            t.target = target;
            if (target is Component com)
            {
                t.SetLink(com.gameObject);
            }else if (target is GameObject obj)
            {
                t.SetLink(obj);
            }
            return t;
        }

        /// <summary>
        /// 设置创建 堆栈
        /// </summary>
        /// <param name="t"></param>
        /// <param name="traceback"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T SetCallTraceback<T>(this T t, string traceback) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.creatTraceback = traceback;
            return t;
        }

        // Token: 0x06000126 RID: 294 RVA: 0x00006AD0 File Offset: 0x00004CD0
        public static T SetLoops<T>(this T t, int loops) where T : Tween
        {
            if (t == null || !t.active || t.creationLocked)
            {
                return t;
            }

            if (loops < -1)
            {
                loops = -1;
            }
            else if (loops == 0)
            {
                loops = 1;
            }

            t.loops = loops;
            if (t.tweenType == TweenType.Tweener)
            {
                if (loops > -1)
                {
                    t.fullDuration = t.duration * (float) loops;
                }
                else
                {
                    t.fullDuration = float.PositiveInfinity;
                }
            }

            return t;
        }

        // Token: 0x06000127 RID: 295 RVA: 0x00006B5C File Offset: 0x00004D5C
        public static T SetLoops<T>(this T t, int loops, LoopType loopType) where T : Tween
        {
            if (t == null || !t.active || t.creationLocked)
            {
                return t;
            }

            if (loops < -1)
            {
                loops = -1;
            }
            else if (loops == 0)
            {
                loops = 1;
            }

            t.loops = loops;
            t.loopType = loopType;
            if (t.tweenType == TweenType.Tweener)
            {
                if (loops > -1)
                {
                    t.fullDuration = t.duration * (float) loops;
                }
                else
                {
                    t.fullDuration = float.PositiveInfinity;
                }
            }

            return t;
        }

        // Token: 0x06000128 RID: 296 RVA: 0x00006BF4 File Offset: 0x00004DF4
        public static T SetEase<T>(this T t, Ease ease) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.easeType = ease;
            if (EaseManager.IsFlashEase(ease))
            {
                t.easeOvershootOrAmplitude = (float) ((int) t.easeOvershootOrAmplitude);
            }

            t.customEase = null;
            return t;
        }

        // Token: 0x06000129 RID: 297 RVA: 0x00006C54 File Offset: 0x00004E54
        public static T SetEase<T>(this T t, Ease ease, float overshoot) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.easeType = ease;
            if (EaseManager.IsFlashEase(ease))
            {
                overshoot = (float) ((int) overshoot);
            }

            t.easeOvershootOrAmplitude = overshoot;
            t.customEase = null;
            return t;
        }

        // Token: 0x0600012A RID: 298 RVA: 0x00006CAC File Offset: 0x00004EAC
        public static T SetEase<T>(this T t, Ease ease, float amplitude, float period) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.easeType = ease;
            if (EaseManager.IsFlashEase(ease))
            {
                amplitude = (float) ((int) amplitude);
            }

            t.easeOvershootOrAmplitude = amplitude;
            t.easePeriod = period;
            t.customEase = null;
            return t;
        }

        // Token: 0x0600012B RID: 299 RVA: 0x00006D10 File Offset: 0x00004F10
        public static T SetEase<T>(this T t, AnimationCurve animCurve) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.easeType = Ease.INTERNAL_Custom;
            t.customEase = new EaseFunction(new EaseCurve(animCurve).Evaluate);
            return t;
        }

        // Token: 0x0600012C RID: 300 RVA: 0x00006D5E File Offset: 0x00004F5E
        public static T SetEase<T>(this T t, EaseFunction customEase) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.easeType = Ease.INTERNAL_Custom;
            t.customEase = customEase;
            return t;
        }

        // Token: 0x0600012D RID: 301 RVA: 0x00006D91 File Offset: 0x00004F91
        public static T SetRecyclable<T>(this T t) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.isRecyclable = true;
            return t;
        }

        // Token: 0x0600012E RID: 302 RVA: 0x00006DB7 File Offset: 0x00004FB7
        public static T SetRecyclable<T>(this T t, bool recyclable) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.isRecyclable = recyclable;
            return t;
        }

        // Token: 0x0600012F RID: 303 RVA: 0x00006DDD File Offset: 0x00004FDD
        public static T SetUpdate<T>(this T t, bool isIndependentUpdate) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            TweenManager.SetUpdateType(t, DOTween.defaultUpdateType, isIndependentUpdate);
            return t;
        }

        // Token: 0x06000130 RID: 304 RVA: 0x00006E08 File Offset: 0x00005008
        public static T SetUpdate<T>(this T t, UpdateType updateType) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            TweenManager.SetUpdateType(t, updateType, DOTween.defaultTimeScaleIndependent);
            return t;
        }

        // Token: 0x06000131 RID: 305 RVA: 0x00006E33 File Offset: 0x00005033
        public static T SetUpdate<T>(this T t, UpdateType updateType, bool isIndependentUpdate) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            TweenManager.SetUpdateType(t, updateType, isIndependentUpdate);
            return t;
        }

        // Token: 0x06000132 RID: 306 RVA: 0x00006E5A File Offset: 0x0000505A
        public static T OnStart<T>(this T t, TweenCallback action) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.onStart = action;
            return t;
        }

        // Token: 0x06000133 RID: 307 RVA: 0x00006E80 File Offset: 0x00005080
        public static T OnPlay<T>(this T t, TweenCallback action) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.onPlay = action;
            return t;
        }

        // Token: 0x06000134 RID: 308 RVA: 0x00006EA6 File Offset: 0x000050A6
        public static T OnPause<T>(this T t, TweenCallback action) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.onPause = action;
            return t;
        }

        // Token: 0x06000135 RID: 309 RVA: 0x00006ECC File Offset: 0x000050CC
        public static T OnRewind<T>(this T t, TweenCallback action) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.onRewind = action;
            return t;
        }

        // Token: 0x06000136 RID: 310 RVA: 0x00006EF2 File Offset: 0x000050F2
        public static T OnUpdate<T>(this T t, TweenCallback action) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.onUpdate = action;
            return t;
        }

        // Token: 0x06000137 RID: 311 RVA: 0x00006F18 File Offset: 0x00005118
        public static T OnStepComplete<T>(this T t, TweenCallback action) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.onStepComplete = action;
            return t;
        }

        // Token: 0x06000138 RID: 312 RVA: 0x00006F3E File Offset: 0x0000513E
        public static T OnComplete<T>(this T t, TweenCallback action) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.onComplete = action;
            return t;
        }

        // Token: 0x06000139 RID: 313 RVA: 0x00006F64 File Offset: 0x00005164
        public static T OnKill<T>(this T t, TweenCallback action) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.onKill = action;
            return t;
        }

        // Token: 0x0600013A RID: 314 RVA: 0x00006F8A File Offset: 0x0000518A
        public static T OnWaypointChange<T>(this T t, TweenCallback<int> action) where T : Tween
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.onWaypointChange = action;
            return t;
        }

        // Token: 0x0600013B RID: 315 RVA: 0x00006FB0 File Offset: 0x000051B0
        public static T SetAs<T>(this T t, Tween asTween) where T : Tween
        {
            if (t == null || !t.active || t.creationLocked)
            {
                return t;
            }

            t.timeScale = asTween.timeScale;
            t.isBackwards = asTween.isBackwards;
            TweenManager.SetUpdateType(t, asTween.updateType, asTween.isIndependentUpdate);
            t.id = asTween.id;
            t.onStart = asTween.onStart;
            t.onPlay = asTween.onPlay;
            t.onRewind = asTween.onRewind;
            t.onUpdate = asTween.onUpdate;
            t.onStepComplete = asTween.onStepComplete;
            t.onComplete = asTween.onComplete;
            t.onKill = asTween.onKill;
            t.onWaypointChange = asTween.onWaypointChange;
            t.isRecyclable = asTween.isRecyclable;
            t.isSpeedBased = asTween.isSpeedBased;
            t.autoKill = asTween.autoKill;
            t.loops = asTween.loops;
            t.loopType = asTween.loopType;
            if (t.tweenType == TweenType.Tweener)
            {
                if (t.loops > -1)
                {
                    t.fullDuration = t.duration * (float) t.loops;
                }
                else
                {
                    t.fullDuration = float.PositiveInfinity;
                }
            }

            t.delay = asTween.delay;
            t.delayComplete = (t.delay <= 0f);
            t.isRelative = asTween.isRelative;
            t.easeType = asTween.easeType;
            t.customEase = asTween.customEase;
            t.easeOvershootOrAmplitude = asTween.easeOvershootOrAmplitude;
            t.easePeriod = asTween.easePeriod;
            return t;
        }

        // Token: 0x0600013C RID: 316 RVA: 0x000071E0 File Offset: 0x000053E0
        public static T SetAs<T>(this T t, TweenParams tweenParams) where T : Tween
        {
            if (t == null || !t.active || t.creationLocked)
            {
                return t;
            }

            TweenManager.SetUpdateType(t, tweenParams.updateType, tweenParams.isIndependentUpdate);
            t.id = tweenParams.id;
            t.onStart = tweenParams.onStart;
            t.onPlay = tweenParams.onPlay;
            t.onRewind = tweenParams.onRewind;
            t.onUpdate = tweenParams.onUpdate;
            t.onStepComplete = tweenParams.onStepComplete;
            t.onComplete = tweenParams.onComplete;
            t.onKill = tweenParams.onKill;
            t.onWaypointChange = tweenParams.onWaypointChange;
            t.isRecyclable = tweenParams.isRecyclable;
            t.isSpeedBased = tweenParams.isSpeedBased;
            t.autoKill = tweenParams.autoKill;
            t.loops = tweenParams.loops;
            t.loopType = tweenParams.loopType;
            if (t.tweenType == TweenType.Tweener)
            {
                if (t.loops > -1)
                {
                    t.fullDuration = t.duration * (float) t.loops;
                }
                else
                {
                    t.fullDuration = float.PositiveInfinity;
                }
            }

            t.delay = tweenParams.delay;
            t.delayComplete = (t.delay <= 0f);
            t.isRelative = tweenParams.isRelative;
            if (tweenParams.easeType == Ease.Unset)
            {
                if (t.tweenType == TweenType.Sequence)
                {
                    t.easeType = Ease.Linear;
                }
                else
                {
                    t.easeType = DOTween.defaultEaseType;
                }
            }
            else
            {
                t.easeType = tweenParams.easeType;
            }

            t.customEase = tweenParams.customEase;
            t.easeOvershootOrAmplitude = tweenParams.easeOvershootOrAmplitude;
            t.easePeriod = tweenParams.easePeriod;
            return t;
        }

        // Token: 0x0600013D RID: 317 RVA: 0x00007423 File Offset: 0x00005623
        public static Sequence Append(this Sequence s, Tween t)
        {
            if (s == null || !s.active || s.creationLocked)
            {
                return s;
            }

            if (t == null || !t.active || t.isSequenced)
            {
                return s;
            }

            Sequence.DoInsert(s, t, s.duration);
            return s;
        }

        // Token: 0x0600013E RID: 318 RVA: 0x0000745E File Offset: 0x0000565E
        public static Sequence Prepend(this Sequence s, Tween t)
        {
            if (s == null || !s.active || s.creationLocked)
            {
                return s;
            }

            if (t == null || !t.active || t.isSequenced)
            {
                return s;
            }

            Sequence.DoPrepend(s, t);
            return s;
        }

        // Token: 0x0600013F RID: 319 RVA: 0x00007493 File Offset: 0x00005693
        public static Sequence Join(this Sequence s, Tween t)
        {
            if (s == null || !s.active || s.creationLocked)
            {
                return s;
            }

            if (t == null || !t.active || t.isSequenced)
            {
                return s;
            }

            Sequence.DoInsert(s, t, s.lastTweenInsertTime);
            return s;
        }

        // Token: 0x06000140 RID: 320 RVA: 0x000074CE File Offset: 0x000056CE
        public static Sequence Insert(this Sequence s, float atPosition, Tween t)
        {
            if (s == null || !s.active || s.creationLocked)
            {
                return s;
            }

            if (t == null || !t.active || t.isSequenced)
            {
                return s;
            }

            Sequence.DoInsert(s, t, atPosition);
            return s;
        }

        // Token: 0x06000141 RID: 321 RVA: 0x00007504 File Offset: 0x00005704
        public static Sequence AppendInterval(this Sequence s, float interval)
        {
            if (s == null || !s.active || s.creationLocked)
            {
                return s;
            }

            Sequence.DoAppendInterval(s, interval);
            return s;
        }

        // Token: 0x06000142 RID: 322 RVA: 0x00007524 File Offset: 0x00005724
        public static Sequence PrependInterval(this Sequence s, float interval)
        {
            if (s == null || !s.active || s.creationLocked)
            {
                return s;
            }

            Sequence.DoPrependInterval(s, interval);
            return s;
        }

        // Token: 0x06000143 RID: 323 RVA: 0x00007544 File Offset: 0x00005744
        public static Sequence AppendCallback(this Sequence s, TweenCallback callback)
        {
            if (s == null || !s.active || s.creationLocked)
            {
                return s;
            }

            if (callback == null)
            {
                return s;
            }

            Sequence.DoInsertCallback(s, callback, s.duration);
            return s;
        }

        // Token: 0x06000144 RID: 324 RVA: 0x0000756F File Offset: 0x0000576F
        public static Sequence PrependCallback(this Sequence s, TweenCallback callback)
        {
            if (s == null || !s.active || s.creationLocked)
            {
                return s;
            }

            if (callback == null)
            {
                return s;
            }

            Sequence.DoInsertCallback(s, callback, 0f);
            return s;
        }

        // Token: 0x06000145 RID: 325 RVA: 0x00007599 File Offset: 0x00005799
        public static Sequence InsertCallback(this Sequence s, float atPosition, TweenCallback callback)
        {
            if (s == null || !s.active || s.creationLocked)
            {
                return s;
            }

            if (callback == null)
            {
                return s;
            }

            Sequence.DoInsertCallback(s, callback, atPosition);
            return s;
        }

        // Token: 0x06000146 RID: 326 RVA: 0x000075C0 File Offset: 0x000057C0
        public static T From<T>(this T t) where T : Tweener
        {
            if (t == null || !t.active || t.creationLocked || !t.isFromAllowed)
            {
                return t;
            }

            t.isFrom = true;
            t.SetFrom(false);
            return t;
        }

        // Token: 0x06000147 RID: 327 RVA: 0x00007618 File Offset: 0x00005818
        public static T From<T>(this T t, bool isRelative) where T : Tweener
        {
            if (t == null || !t.active || t.creationLocked || !t.isFromAllowed)
            {
                return t;
            }

            t.isFrom = true;
            if (!isRelative)
            {
                t.SetFrom(false);
            }
            else
            {
                t.SetFrom(!t.isBlendable);
            }

            return t;
        }

        // Token: 0x06000148 RID: 328 RVA: 0x0000768F File Offset: 0x0000588F
        public static TweenerCore<T1, T2, TPlugOptions> From<T1, T2, TPlugOptions>(
            this TweenerCore<T1, T2, TPlugOptions> t, T2 fromValue, bool setImmediately = true, bool isRelative = false)
            where TPlugOptions : struct, IPlugOptions
        {
            if (t == null || !t.active || t.creationLocked || !t.isFromAllowed)
            {
                return t;
            }

            t.isFrom = true;
            t.SetFrom(fromValue, setImmediately, isRelative);
            return t;
        }

        // Token: 0x06000149 RID: 329 RVA: 0x000076C0 File Offset: 0x000058C0
        public static TweenerCore<Color, Color, ColorOptions> From(this TweenerCore<Color, Color, ColorOptions> t,
            float fromAlphaValue, bool setImmediately = true, bool isRelative = false)
        {
            if (t == null || !t.active || t.creationLocked || !t.isFromAllowed)
            {
                return t;
            }

            t.isFrom = true;
            t.SetFrom(new Color(0f, 0f, 0f, fromAlphaValue), setImmediately, isRelative);
            return t;
        }

        // Token: 0x0600014A RID: 330 RVA: 0x00007710 File Offset: 0x00005910
        public static TweenerCore<Vector3, Vector3, VectorOptions> From(
            this TweenerCore<Vector3, Vector3, VectorOptions> t, float fromValue, bool setImmediately = true,
            bool isRelative = false)
        {
            if (t == null || !t.active || t.creationLocked || !t.isFromAllowed)
            {
                return t;
            }

            t.isFrom = true;
            t.SetFrom(new Vector3(fromValue, fromValue, fromValue), setImmediately, isRelative);
            return t;
        }

        // Token: 0x0600014B RID: 331 RVA: 0x00007748 File Offset: 0x00005948
        public static T SetDelay<T>(this T t, float delay) where T : Tween
        {
            if (t == null || !t.active || t.creationLocked)
            {
                return t;
            }

            if (t.tweenType == TweenType.Sequence)
            {
                (t as Sequence).PrependInterval(delay);
            }
            else
            {
                t.delay = delay;
                t.delayComplete = (delay <= 0f);
            }

            return t;
        }

        // Token: 0x0600014C RID: 332 RVA: 0x000077C0 File Offset: 0x000059C0
        public static T SetDelay<T>(this T t, float delay, bool asPrependedIntervalIfSequence) where T : Tween
        {
            if (t == null || !t.active || t.creationLocked)
            {
                return t;
            }

            if (t.tweenType != TweenType.Sequence || !asPrependedIntervalIfSequence)
            {
                t.delay = delay;
                t.delayComplete = (delay <= 0f);
            }
            else
            {
                (t as Sequence).PrependInterval(delay);
            }

            return t;
        }

        // Token: 0x0600014D RID: 333 RVA: 0x0000783C File Offset: 0x00005A3C
        public static T SetRelative<T>(this T t) where T : Tween
        {
            if (t == null || !t.active || t.creationLocked || t.isFrom || t.isBlendable)
            {
                return t;
            }

            t.isRelative = true;
            return t;
        }

        // Token: 0x0600014E RID: 334 RVA: 0x00007894 File Offset: 0x00005A94
        public static T SetRelative<T>(this T t, bool isRelative) where T : Tween
        {
            if (t == null || !t.active || t.creationLocked || t.isFrom || t.isBlendable)
            {
                return t;
            }

            t.isRelative = isRelative;
            return t;
        }

        // Token: 0x0600014F RID: 335 RVA: 0x000078EC File Offset: 0x00005AEC
        public static T SetSpeedBased<T>(this T t) where T : Tween
        {
            if (t == null || !t.active || t.creationLocked)
            {
                return t;
            }

            t.isSpeedBased = true;
            return t;
        }

        // Token: 0x06000150 RID: 336 RVA: 0x0000791F File Offset: 0x00005B1F
        public static T SetSpeedBased<T>(this T t, bool isSpeedBased) where T : Tween
        {
            if (t == null || !t.active || t.creationLocked)
            {
                return t;
            }

            t.isSpeedBased = isSpeedBased;
            return t;
        }

        // Token: 0x06000151 RID: 337 RVA: 0x00007952 File Offset: 0x00005B52
        public static Tweener SetOptions(this TweenerCore<float, float, FloatOptions> t, bool snapping)
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.plugOptions.snapping = snapping;
            return t;
        }

        // Token: 0x06000152 RID: 338 RVA: 0x0000796E File Offset: 0x00005B6E
        public static Tweener SetOptions(this TweenerCore<Vector2, Vector2, VectorOptions> t, bool snapping)
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.plugOptions.snapping = snapping;
            return t;
        }

        // Token: 0x06000153 RID: 339 RVA: 0x0000798A File Offset: 0x00005B8A
        public static Tweener SetOptions(this TweenerCore<Vector2, Vector2, VectorOptions> t,
            AxisConstraint axisConstraint, bool snapping = false)
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.plugOptions.axisConstraint = axisConstraint;
            t.plugOptions.snapping = snapping;
            return t;
        }

        // Token: 0x06000154 RID: 340 RVA: 0x000079B2 File Offset: 0x00005BB2
        public static Tweener SetOptions(this TweenerCore<Vector3, Vector3, VectorOptions> t, bool snapping)
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.plugOptions.snapping = snapping;
            return t;
        }

        // Token: 0x06000155 RID: 341 RVA: 0x000079CE File Offset: 0x00005BCE
        public static Tweener SetOptions(this TweenerCore<Vector3, Vector3, VectorOptions> t,
            AxisConstraint axisConstraint, bool snapping = false)
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.plugOptions.axisConstraint = axisConstraint;
            t.plugOptions.snapping = snapping;
            return t;
        }

        // Token: 0x06000156 RID: 342 RVA: 0x000079F6 File Offset: 0x00005BF6
        public static Tweener SetOptions(this TweenerCore<Vector4, Vector4, VectorOptions> t, bool snapping)
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.plugOptions.snapping = snapping;
            return t;
        }

        // Token: 0x06000157 RID: 343 RVA: 0x00007A12 File Offset: 0x00005C12
        public static Tweener SetOptions(this TweenerCore<Vector4, Vector4, VectorOptions> t,
            AxisConstraint axisConstraint, bool snapping = false)
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.plugOptions.axisConstraint = axisConstraint;
            t.plugOptions.snapping = snapping;
            return t;
        }

        // Token: 0x06000158 RID: 344 RVA: 0x00007A3A File Offset: 0x00005C3A
        public static Tweener SetOptions(this TweenerCore<Quaternion, Vector3, QuaternionOptions> t,
            bool useShortest360Route = true)
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.plugOptions.rotateMode = (useShortest360Route ? RotateMode.Fast : RotateMode.FastBeyond360);
            return t;
        }

        // Token: 0x06000159 RID: 345 RVA: 0x00007A5C File Offset: 0x00005C5C
        public static Tweener SetOptions(this TweenerCore<Color, Color, ColorOptions> t, bool alphaOnly)
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.plugOptions.alphaOnly = alphaOnly;
            return t;
        }

        // Token: 0x0600015A RID: 346 RVA: 0x00007A78 File Offset: 0x00005C78
        public static Tweener SetOptions(this TweenerCore<Rect, Rect, RectOptions> t, bool snapping)
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.plugOptions.snapping = snapping;
            return t;
        }

        // Token: 0x0600015B RID: 347 RVA: 0x00007A94 File Offset: 0x00005C94
        public static Tweener SetOptions(this TweenerCore<string, string, StringOptions> t, bool richTextEnabled,
            ScrambleMode scrambleMode = ScrambleMode.None, string scrambleChars = null)
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.plugOptions.richTextEnabled = richTextEnabled;
            t.plugOptions.scrambleMode = scrambleMode;
            if (!string.IsNullOrEmpty(scrambleChars))
            {
                if (scrambleChars.Length <= 1)
                {
                    scrambleChars += scrambleChars;
                }

                t.plugOptions.scrambledChars = scrambleChars.ToCharArray();
                t.plugOptions.scrambledChars.ScrambleChars();
            }

            return t;
        }

        // Token: 0x0600015C RID: 348 RVA: 0x00007B02 File Offset: 0x00005D02
        public static Tweener SetOptions(this TweenerCore<Vector3, Vector3[], Vector3ArrayOptions> t, bool snapping)
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.plugOptions.snapping = snapping;
            return t;
        }

        // Token: 0x0600015D RID: 349 RVA: 0x00007B1E File Offset: 0x00005D1E
        public static Tweener SetOptions(this TweenerCore<Vector3, Vector3[], Vector3ArrayOptions> t,
            AxisConstraint axisConstraint, bool snapping = false)
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.plugOptions.axisConstraint = axisConstraint;
            t.plugOptions.snapping = snapping;
            return t;
        }

        // Token: 0x0600015E RID: 350 RVA: 0x00007B46 File Offset: 0x00005D46
        public static TweenerCore<Vector3, Path, PathOptions> SetOptions(this TweenerCore<Vector3, Path, PathOptions> t,
            AxisConstraint lockPosition, AxisConstraint lockRotation = AxisConstraint.None)
        {
            return t.SetOptions(false, lockPosition, lockRotation);
        }

        // Token: 0x0600015F RID: 351 RVA: 0x00007B51 File Offset: 0x00005D51
        public static TweenerCore<Vector3, Path, PathOptions> SetOptions(this TweenerCore<Vector3, Path, PathOptions> t,
            bool closePath, AxisConstraint lockPosition = AxisConstraint.None,
            AxisConstraint lockRotation = AxisConstraint.None)
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.plugOptions.isClosedPath = closePath;
            t.plugOptions.lockPositionAxis = lockPosition;
            t.plugOptions.lockRotationAxis = lockRotation;
            return t;
        }

        // Token: 0x06000160 RID: 352 RVA: 0x00007B85 File Offset: 0x00005D85
        public static TweenerCore<Vector3, Path, PathOptions> SetLookAt(this TweenerCore<Vector3, Path, PathOptions> t,
            Vector3 lookAtPosition, Vector3? forwardDirection = null, Vector3? up = null)
        {
            return t.SetLookAt(OrientType.LookAtPosition, lookAtPosition, null, -1f, forwardDirection, up, false);
        }

        // Token: 0x06000161 RID: 353 RVA: 0x00007B98 File Offset: 0x00005D98
        public static TweenerCore<Vector3, Path, PathOptions> SetLookAt(this TweenerCore<Vector3, Path, PathOptions> t,
            Vector3 lookAtPosition, bool stableZRotation)
        {
            return t.SetLookAt(OrientType.LookAtPosition, lookAtPosition, null, -1f, null, null, stableZRotation);
        }

        // Token: 0x06000162 RID: 354 RVA: 0x00007BC6 File Offset: 0x00005DC6
        public static TweenerCore<Vector3, Path, PathOptions> SetLookAt(this TweenerCore<Vector3, Path, PathOptions> t,
            Transform lookAtTransform, Vector3? forwardDirection = null, Vector3? up = null)
        {
            return t.SetLookAt(OrientType.LookAtTransform, Vector3.zero, lookAtTransform, -1f, forwardDirection, up,
                false);
        }

        // Token: 0x06000163 RID: 355 RVA: 0x00007BE0 File Offset: 0x00005DE0
        public static TweenerCore<Vector3, Path, PathOptions> SetLookAt(this TweenerCore<Vector3, Path, PathOptions> t,
            Transform lookAtTransform, bool stableZRotation)
        {
            return t.SetLookAt(OrientType.LookAtTransform, Vector3.zero, lookAtTransform, -1f, null, null,
                stableZRotation);
        }

        // Token: 0x06000164 RID: 356 RVA: 0x00007C12 File Offset: 0x00005E12
        public static TweenerCore<Vector3, Path, PathOptions> SetLookAt(this TweenerCore<Vector3, Path, PathOptions> t,
            float lookAhead, Vector3? forwardDirection = null, Vector3? up = null)
        {
            return t.SetLookAt(OrientType.ToPath, Vector3.zero, null, lookAhead, forwardDirection, up, false);
        }

        // Token: 0x06000165 RID: 357 RVA: 0x00007C28 File Offset: 0x00005E28
        public static TweenerCore<Vector3, Path, PathOptions> SetLookAt(this TweenerCore<Vector3, Path, PathOptions> t,
            float lookAhead, bool stableZRotation)
        {
            return t.SetLookAt(OrientType.ToPath, Vector3.zero, null, lookAhead, null, null, stableZRotation);
        }

        // Token: 0x06000166 RID: 358 RVA: 0x00007C58 File Offset: 0x00005E58
        private static TweenerCore<Vector3, Path, PathOptions> SetLookAt(this TweenerCore<Vector3, Path, PathOptions> t,
            OrientType orientType, Vector3 lookAtPosition, Transform lookAtTransform, float lookAhead,
            Vector3? forwardDirection = null, Vector3? up = null, bool stableZRotation = false)
        {
            if (t == null || !t.active)
            {
                return t;
            }

            t.plugOptions.orientType = orientType;
            switch (orientType)
            {
                case OrientType.ToPath:
                    if (lookAhead < 0.0001f)
                    {
                        lookAhead = 0.0001f;
                    }

                    t.plugOptions.lookAhead = lookAhead;
                    break;
                case OrientType.LookAtTransform:
                    t.plugOptions.lookAtTransform = lookAtTransform;
                    break;
                case OrientType.LookAtPosition:
                    t.plugOptions.lookAtPosition = lookAtPosition;
                    break;
            }

            t.plugOptions.lookAtPosition = lookAtPosition;
            t.plugOptions.stableZRotation = stableZRotation;
            t.SetPathForwardDirection(forwardDirection, up);
            return t;
        }

        // Token: 0x06000167 RID: 359 RVA: 0x00007CF4 File Offset: 0x00005EF4
        private static void SetPathForwardDirection(this TweenerCore<Vector3, Path, PathOptions> t,
            Vector3? forwardDirection = null, Vector3? up = null)
        {
            if (t == null || !t.active)
            {
                return;
            }

            bool hasCustomForwardDirection;
            if (forwardDirection != null)
            {
                Vector3? vector = forwardDirection;
                Vector3 zero = Vector3.zero;
                if (vector == null || (vector != null && vector.GetValueOrDefault() != zero))
                {
                    hasCustomForwardDirection = true;
                    goto IL_86;
                }
            }

            if (up != null)
            {
                Vector3? vector = up;
                Vector3 zero = Vector3.zero;
                hasCustomForwardDirection = (vector == null || (vector != null && vector.GetValueOrDefault() != zero));
            }
            else
            {
                hasCustomForwardDirection = false;
            }

            IL_86:
            t.plugOptions.hasCustomForwardDirection = hasCustomForwardDirection;
            if (t.plugOptions.hasCustomForwardDirection)
            {
                Vector3? vector = forwardDirection;
                Vector3 zero = Vector3.zero;
                if (vector != null && (vector == null || vector.GetValueOrDefault() == zero))
                {
                    forwardDirection = new Vector3?(Vector3.forward);
                }

                t.plugOptions.forward = Quaternion.LookRotation(
                    (forwardDirection == null) ? Vector3.forward : forwardDirection.Value,
                    (up == null) ? Vector3.up : up.Value);
            }
        }
    }
}