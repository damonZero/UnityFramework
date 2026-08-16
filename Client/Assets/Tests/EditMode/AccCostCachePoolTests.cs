using Framework.ViewCache;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public sealed class AccCostCachePoolTests
    {
        // 回归：新 key 首次 TryPut 时，修复前会把同一 value 入栈两次（_replicaLimit > 1 时），
        // 导致 TryGet 把同一实例返回两次（对应 REVIEW 2.10 High）。
        [Test]
        public void TryPut_PushesValueOnlyOnceForNewKey()
        {
            var pool = new AccCostCachePool<int, object>(capacity: 10, replicaLimit: 2);
            var value = new object();

            Assert.That(pool.TryPut(1, value, 1), Is.True);

            Assert.That(pool.TryGet(1, out var first), Is.True);
            Assert.That(first, Is.SameAs(value));
            Assert.That(pool.TryGet(1, out _), Is.False, "同一 key 首次 TryPut 只应缓存一个 value");
        }
    }
}
