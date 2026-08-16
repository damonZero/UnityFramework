using System;
using Framework.Timer;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public sealed class TimerTests
    {
        [TearDown]
        public void TearDown()
        {
            TimerDependencies.LogError = null;
        }

        [Test]
        public void ScheduleOnceFiresAfterDelay()
        {
            var mgr = new TimerManager();
            var fired = 0;
            mgr.ScheduleOnce(1f, () => fired++);

            mgr.Tick(0.5f);
            Assert.AreEqual(0, fired, "未到 delay 不应触发");

            mgr.Tick(0.5f);
            Assert.AreEqual(1, fired, "累计到达 delay 应触发一次");
        }

        [Test]
        public void ScheduleOnceFiresOnlyOnce()
        {
            var mgr = new TimerManager();
            var fired = 0;
            mgr.ScheduleOnce(0.1f, () => fired++);

            mgr.Tick(1f);
            mgr.Tick(1f);

            Assert.AreEqual(1, fired, "一次性计时器只应触发一次");
            Assert.AreEqual(0, mgr.ActiveCount);
        }

        [Test]
        public void ScheduleLoopFiresRepeatedly()
        {
            var mgr = new TimerManager();
            var fired = 0;
            mgr.ScheduleLoop(1f, () => fired++);

            mgr.Tick(1f);
            mgr.Tick(1f);
            mgr.Tick(1f);

            Assert.AreEqual(3, fired, "循环计时器应每次 interval 触发一次");
        }

        [Test]
        public void ScheduleLoopHonorsInitialDelay()
        {
            var mgr = new TimerManager();
            var fired = 0;
            mgr.ScheduleLoop(1f, () => fired++, initialDelay: 2f);

            mgr.Tick(1f);
            Assert.AreEqual(0, fired, "initialDelay 内不应触发");

            mgr.Tick(1f);
            Assert.AreEqual(1, fired, "经过 initialDelay 后第一次触发");
        }

        [Test]
        public void PauseAndResume()
        {
            var mgr = new TimerManager();
            var fired = 0;
            var handle = mgr.ScheduleOnce(1f, () => fired++);

            mgr.Tick(0.5f);
            handle.Pause();
            mgr.Tick(1f);
            Assert.AreEqual(0, fired, "暂停期间不应推进剩余时间");

            handle.Resume();
            mgr.Tick(0.5f);
            Assert.AreEqual(1, fired, "恢复后按剩余时间触发");
        }

        [Test]
        public void CancelPreventsFire()
        {
            var mgr = new TimerManager();
            var fired = 0;
            var handle = mgr.ScheduleOnce(1f, () => fired++);

            handle.Cancel();
            mgr.Tick(5f);

            Assert.AreEqual(0, fired, "取消后不应触发");
            Assert.IsFalse(handle.IsValid);
            Assert.AreEqual(0, mgr.ActiveCount, "取消的计时器应在 Tick 时回收");
        }

        [Test]
        public void TimeScaleZeroPausesAll()
        {
            var mgr = new TimerManager { TimeScale = 0f };
            var fired = 0;
            mgr.ScheduleOnce(0.1f, () => fired++);

            mgr.Tick(1f);
            Assert.AreEqual(0, fired, "TimeScale=0 时不应触发");

            mgr.TimeScale = 1f;
            mgr.Tick(0.1f);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void CallbackExceptionDoesNotBreakTick()
        {
            var mgr = new TimerManager();
            var logged = 0;
            TimerDependencies.LogError = _ => logged++;

            var ok = 0;
            mgr.ScheduleOnce(0.1f, () => throw new InvalidOperationException("boom"));
            mgr.ScheduleOnce(0.1f, () => ok++);

            Assert.DoesNotThrow(() => mgr.Tick(0.2f));
            Assert.AreEqual(1, logged, "异常应交给日志钩子");
            Assert.AreEqual(1, ok, "异常回调不应阻塞其他计时器");
        }

        [Test]
        public void StaleHandleAfterNodeReuseIsInvalid()
        {
            var mgr = new TimerManager();
            var h1 = mgr.ScheduleOnce(10f, () => { });
            h1.Cancel();
            mgr.Tick(0.01f); // Compact 回收节点

            var fired = false;
            var h2 = mgr.ScheduleOnce(0.1f, () => fired = true); // 复用同一节点

            Assert.IsFalse(h1.IsValid, "旧句柄在节点复用后应失效");
            Assert.IsTrue(h2.IsValid);

            h1.Cancel(); // 应是无操作，不能误取消 h2
            mgr.Tick(0.1f);
            Assert.IsTrue(fired, "旧句柄不应误取消复用后的新计时器");
        }

        [Test]
        public void ClearCancelsAll()
        {
            var mgr = new TimerManager();
            var fired = 0;
            mgr.ScheduleOnce(1f, () => fired++);
            mgr.ScheduleLoop(1f, () => fired++);

            mgr.Clear();

            Assert.AreEqual(0, mgr.ActiveCount);
            mgr.Tick(10f);
            Assert.AreEqual(0, fired, "Clear 后不应再触发任何计时器");
        }

        [Test]
        public void ZeroDelayFiresNextTick()
        {
            var mgr = new TimerManager();
            var fired = 0;
            mgr.ScheduleOnce(0f, () => fired++);

            Assert.AreEqual(0, fired, "调度时不立即触发");
            mgr.Tick(0.016f);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void ScheduleThrowsOnNullCallback()
        {
            var mgr = new TimerManager();
            Assert.Throws<ArgumentNullException>(() => mgr.ScheduleOnce(1f, null!));
            Assert.Throws<ArgumentNullException>(() => mgr.ScheduleLoop(1f, null!));
        }
    }
}
