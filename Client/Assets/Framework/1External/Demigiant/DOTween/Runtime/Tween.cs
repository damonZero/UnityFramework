using System;
using DG.Tweening.Core;
using DG.Tweening.Core.Enums;

namespace DG.Tweening
{
    // Token: 0x02000019 RID: 25
    public abstract class Tween : ABSSequentiable
    {
        // Token: 0x17000003 RID: 3
        // (get) Token: 0x06000168 RID: 360 RVA: 0x00007E12 File Offset: 0x00006012
        // (set) Token: 0x06000169 RID: 361 RVA: 0x00007E1A File Offset: 0x0000601A
        public bool isRelative { get; internal set; }

        // Token: 0x17000004 RID: 4
        // (get) Token: 0x0600016A RID: 362 RVA: 0x00007E23 File Offset: 0x00006023
        // (set) Token: 0x0600016B RID: 363 RVA: 0x00007E2B File Offset: 0x0000602B
        public bool active { get; internal set; }

        // Token: 0x17000005 RID: 5
        // (get) Token: 0x0600016C RID: 364 RVA: 0x00007E34 File Offset: 0x00006034
        // (set) Token: 0x0600016D RID: 365 RVA: 0x00007E3D File Offset: 0x0000603D
        public float fullPosition
        {
            get { return this.Elapsed(true); }
            set { this.Goto(value, this.isPlaying); }
        }

        // Token: 0x17000006 RID: 6
        // (get) Token: 0x0600016E RID: 366 RVA: 0x00007E4C File Offset: 0x0000604C
        public bool hasLoops
        {
            get { return this.loops == -1 || this.loops > 1; }
        }

        // Token: 0x17000007 RID: 7
        // (get) Token: 0x0600016F RID: 367 RVA: 0x00007E62 File Offset: 0x00006062
        // (set) Token: 0x06000170 RID: 368 RVA: 0x00007E6A File Offset: 0x0000606A
        public bool playedOnce { get; private set; }

        // Token: 0x17000008 RID: 8
        // (get) Token: 0x06000171 RID: 369 RVA: 0x00007E73 File Offset: 0x00006073
        // (set) Token: 0x06000172 RID: 370 RVA: 0x00007E7B File Offset: 0x0000607B
        public float position { get; internal set; }

        // Token: 0x06000173 RID: 371 RVA: 0x00007E84 File Offset: 0x00006084
        internal virtual void Reset()
        {
            this.timeScale = 1f;
            this.isBackwards = false;
            this.id = null;
            this.stringId = null;
            this.intId = -999;
            this.isIndependentUpdate = false;
            this.onStart = (this.onPlay = (this.onRewind =
                (this.onUpdate = (this.onComplete = (this.onStepComplete = (this.onKill = null))))));
            this.onWaypointChange = null;
            this.debugTargetId = null;
            this.target = null;
            this.isFrom = false;
            this.isBlendable = false;
            this.isSpeedBased = false;
            this.duration = 0f;
            this.loops = 1;
            this.delay = 0f;
            this.isRelative = false;
            this.customEase = null;
            this.isSequenced = false;
            this.sequenceParent = null;
            this.specialStartupMode = SpecialStartupMode.None;
            this.creationLocked = (this.startupDone = (this.playedOnce = false));
            this.position = (this.fullDuration = (float) (this.completedLoops = 0));
            this.isPlaying = (this.isComplete = false);
            this.elapsedDelay = 0f;
            this.delayComplete = true;
            this.miscInt = -1;
        }

        // Token: 0x06000174 RID: 372
        internal abstract bool Validate();

        // Token: 0x06000175 RID: 373 RVA: 0x00007FC6 File Offset: 0x000061C6
        internal virtual float UpdateDelay(float elapsed)
        {
            return 0f;
        }

        // Token: 0x06000176 RID: 374
        internal abstract bool Startup();

        // Token: 0x06000177 RID: 375
        internal abstract bool ApplyTween(float prevPosition, int prevCompletedLoops, int newCompletedSteps,
            bool useInversePosition, UpdateMode updateMode, UpdateNotice updateNotice);

        // Token: 0x06000178 RID: 376 RVA: 0x00007FD0 File Offset: 0x000061D0
        internal static bool DoGoto(Tween t, float toPosition, int toCompletedLoops, UpdateMode updateMode)
        {
            if (!t.startupDone && !t.Startup())
            {
                return true;
            }

            if (!t.playedOnce && updateMode == UpdateMode.Update)
            {
                t.playedOnce = true;
                if (t.onStart != null)
                {
                    Tween.OnTweenCallback(t.onStart, t);
                    if (!t.active)
                    {
                        return true;
                    }
                }

                if (t.onPlay != null)
                {
                    Tween.OnTweenCallback(t.onPlay, t);
                    if (!t.active)
                    {
                        return true;
                    }
                }
            }

            float position = t.position;
            int num = t.completedLoops;
            t.completedLoops = toCompletedLoops;
            bool flag = t.position <= 0f && num <= 0;
            bool flag2 = t.isComplete;
            if (t.loops != -1)
            {
                t.isComplete = (t.completedLoops == t.loops);
            }

            int num2 = 0;
            if (updateMode == UpdateMode.Update)
            {
                if (t.isBackwards)
                {
                    num2 = ((t.completedLoops < num)
                        ? (num - t.completedLoops)
                        : ((toPosition <= 0f && !flag) ? 1 : 0));
                    if (flag2)
                    {
                        num2--;
                    }
                }
                else
                {
                    num2 = ((t.completedLoops > num) ? (t.completedLoops - num) : 0);
                }
            }
            else if (t.tweenType == TweenType.Sequence)
            {
                num2 = num - toCompletedLoops;
                if (num2 < 0)
                {
                    num2 = -num2;
                }
            }

            t.position = toPosition;
            if (t.position > t.duration)
            {
                t.position = t.duration;
            }
            else if (t.position <= 0f)
            {
                if (t.completedLoops > 0 || t.isComplete)
                {
                    t.position = t.duration;
                }
                else
                {
                    t.position = 0f;
                }
            }

            bool flag3 = t.isPlaying;
            if (t.isPlaying)
            {
                if (!t.isBackwards)
                {
                    t.isPlaying = !t.isComplete;
                }
                else
                {
                    t.isPlaying = (t.completedLoops != 0 || t.position > 0f);
                }
            }

            bool useInversePosition = t.hasLoops && t.loopType == LoopType.Yoyo && ((t.position < t.duration)
                ? (t.completedLoops % 2 != 0)
                : (t.completedLoops % 2 == 0));
            UpdateNotice updateNotice =
                (!flag && ((t.loopType == LoopType.Restart && t.completedLoops != num &&
                            (t.loops == -1 || t.completedLoops < t.loops)) ||
                           (t.position <= 0f && t.completedLoops <= 0)))
                    ? UpdateNotice.RewindStep
                    : UpdateNotice.None;
            if (t.ApplyTween(position, num, num2, useInversePosition, updateMode, updateNotice))
            {
                return true;
            }

            if (t.onUpdate != null && updateMode != UpdateMode.IgnoreOnUpdate)
            {
                Tween.OnTweenCallback(t.onUpdate, t);
            }

            if (t.position <= 0f && t.completedLoops <= 0 && !flag && t.onRewind != null)
            {
                Tween.OnTweenCallback(t.onRewind, t);
            }

            if (num2 > 0 && updateMode == UpdateMode.Update && t.onStepComplete != null)
            {
                for (int i = 0; i < num2; i++)
                {
                    Tween.OnTweenCallback(t.onStepComplete, t);
                    if (!t.active)
                    {
                        break;
                    }
                }
            }

            if (t.isComplete && !flag2 && updateMode != UpdateMode.IgnoreOnComplete && t.onComplete != null)
            {
                Tween.OnTweenCallback(t.onComplete, t);
            }

            if (!t.isPlaying && flag3 && (!t.isComplete || !t.autoKill) && t.onPause != null)
            {
                Tween.OnTweenCallback(t.onPause, t);
            }

            return t.autoKill && t.isComplete;
        }

        // Token: 0x06000179 RID: 377 RVA: 0x00008328 File Offset: 0x00006528
        internal static bool OnTweenCallback(TweenCallback callback, Tween t)
        {
            if (DOTween.useSafeMode)
            {
                try
                {
                    callback();
                    return true;
                }
                catch (Exception ex)
                {
                    // if (Debugger.logPriority >= 1)
                    // {
                    // }

                    //Try住的Error直接打印，非常重要，避免业务层懵逼
                    Debugger.LogError(string.Format(
                        "An error inside a tween callback was silently taken care of ({0}) ► {1}\n\n{2}\n\n",
                        ex.TargetSite, ex.Message, ex.StackTrace));
                    DOTween.safeModeReport.Add(SafeModeReport.SafeModeReportType.Callback);
                    return false;
                }
            }

            callback();
            return true;
        }

        // Token: 0x0600017A RID: 378 RVA: 0x00008398 File Offset: 0x00006598
        internal static bool OnTweenCallback<T>(TweenCallback<T> callback, Tween t, T param)
        {
            if (DOTween.useSafeMode)
            {
                try
                {
                    callback(param);
                    return true;
                }
                catch (Exception ex)
                {
                    Debugger.LogError(string.Format(
                        "An error inside a tween callback was silently taken care of ({0}) ► {1}\n\n{2}", ex.TargetSite,
                        ex.Message, ex.StackTrace));
                    DOTween.safeModeReport.Add(SafeModeReport.SafeModeReportType.Callback);
                    return false;
                }
            }

            callback(param);
            return true;
        }

        // Token: 0x04000096 RID: 150
        public float timeScale;

        // Token: 0x04000097 RID: 151
        public bool isBackwards;

        // Token: 0x04000098 RID: 152
        public object id;

        // Token: 0x04000099 RID: 153
        public string stringId;

        // Token: 0x0400009A RID: 154
        public int intId = -999;

        // Token: 0x0400009B RID: 155
        public object target;

        /// <summary>
        /// 创建Tween 动画的调用堆栈
        /// </summary>
        public string creatTraceback;

        // Token: 0x0400009C RID: 156
        internal UpdateType updateType;

        // Token: 0x0400009D RID: 157
        internal bool isIndependentUpdate;

        // Token: 0x0400009E RID: 158
        public TweenCallback onPlay;

        // Token: 0x0400009F RID: 159
        public TweenCallback onPause;

        // Token: 0x040000A0 RID: 160
        public TweenCallback onRewind;

        // Token: 0x040000A1 RID: 161
        public TweenCallback onUpdate;

        // Token: 0x040000A2 RID: 162
        public TweenCallback onStepComplete;

        // Token: 0x040000A3 RID: 163
        public TweenCallback onComplete;

        // Token: 0x040000A4 RID: 164
        public TweenCallback onKill;

        // Token: 0x040000A5 RID: 165
        public TweenCallback<int> onWaypointChange;

        // Token: 0x040000A6 RID: 166
        public bool isFrom;

        // Token: 0x040000A7 RID: 167
        internal bool isBlendable;

        // Token: 0x040000A8 RID: 168
        internal bool isRecyclable;

        // Token: 0x040000A9 RID: 169
        internal bool isSpeedBased;

        // Token: 0x040000AA RID: 170
        internal bool autoKill;

        // Token: 0x040000AB RID: 171
        internal float duration;

        // Token: 0x040000AC RID: 172
        internal int loops;

        // Token: 0x040000AD RID: 173
        internal LoopType loopType;

        // Token: 0x040000AE RID: 174
        internal float delay;

        // Token: 0x040000B0 RID: 176
        internal Ease easeType;

        // Token: 0x040000B1 RID: 177
        internal EaseFunction customEase;

        // Token: 0x040000B2 RID: 178
        public float easeOvershootOrAmplitude;

        // Token: 0x040000B3 RID: 179
        public float easePeriod;

        // Token: 0x040000B4 RID: 180
        public string debugTargetId;

        // Token: 0x040000B5 RID: 181
        internal Type typeofT1;

        // Token: 0x040000B6 RID: 182
        internal Type typeofT2;

        // Token: 0x040000B7 RID: 183
        internal Type typeofTPlugOptions;

        // Token: 0x040000B9 RID: 185
        internal bool isSequenced;

        // Token: 0x040000BA RID: 186
        internal Sequence sequenceParent;

        // Token: 0x040000BB RID: 187
        internal int activeId = -1;

        // Token: 0x040000BC RID: 188
        internal SpecialStartupMode specialStartupMode;

        // Token: 0x040000BD RID: 189
        internal bool creationLocked;

        // Token: 0x040000BE RID: 190
        internal bool startupDone;

        // Token: 0x040000C1 RID: 193
        internal float fullDuration;

        // Token: 0x040000C2 RID: 194
        internal int completedLoops;

        // Token: 0x040000C3 RID: 195
        public bool isPlaying;

        // Token: 0x040000C4 RID: 196
        internal bool isComplete;

        // Token: 0x040000C5 RID: 197
        internal float elapsedDelay;

        // Token: 0x040000C6 RID: 198
        internal bool delayComplete = true;

        // Token: 0x040000C7 RID: 199
        internal int miscInt = -1;
    }
}