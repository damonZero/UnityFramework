using System.Collections;
using Cysharp.Threading.Tasks;
using Framework.View.Navigation;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    [TestFixture]
    public sealed class NavigateContainerTests
    {
        // 回归：NavigateContainer.PostChangeStateAsync 在「Clear && Cache.Visible()」早退时，
        // 修复前会让 PendingState 卡在 Clear（PreChangeStateAsync 已置 Clear），后续 Open/Close
        // 排队等待 120 帧后超时抛 NavigationException（死锁）。修复后应复位 PendingState。
        [UnityTest]
        public IEnumerator Clear_WithClearMemory_DoesNotLeavePendingStateStuck()
        {
            var container = new NavigateContainer();
            // ClearMemory：DoClear 后 CurState=ClearMemory(2)，Cache.Visible() 为 true，命中早退分支。
            container.Cache.ClearType = NavigationClearType.ClearMemory;

            yield return Await(container.Clear());

            Assert.AreEqual(NavigationStateType.None, container.PendingState,
                "Clear 在 Cache.Visible() 早退后必须复位 PendingState，否则状态机死锁");
            Assert.AreEqual(NavigationStateType.None, container.CurrentState,
                "早退语义为「只清理内存不改变状态」，CurrentState 应保持 None");
        }

        // 常规路径：默认 EntranceRecover 清理后 CurState 越过 ClearMemory，Visible() 为 false，
        // 走 base.PostChangeStateAsync，CurrentState 应正常转为 Clear。
        [UnityTest]
        public IEnumerator Clear_WithDefaultType_TransitionsToClearState()
        {
            var container = new NavigateContainer();

            yield return Await(container.Clear());

            Assert.AreEqual(NavigationStateType.Clear, container.CurrentState);
            Assert.AreEqual(NavigationStateType.None, container.PendingState);
        }

        private static IEnumerator Await(UniTask<bool> task)
        {
            int frames = 0;
            while (task.Status == UniTaskStatus.Pending && frames++ < 120)
                yield return null;

            if (task.Status == UniTaskStatus.Faulted)
                task.GetAwaiter().GetResult(); // 让异常浮出，便于定位
        }
    }
}
