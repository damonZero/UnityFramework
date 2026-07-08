using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Framework.Cache;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public sealed class BoundedStoreTests
    {
        // A-B2：Put 覆盖旧值时，旧值必须触发 onEvicted（与 Remove / 淘汰路径一致）。
        [Test]
        public void PutOverwriteInvokesOnEvictedForOldValue()
        {
            var evicted = new List<(string Key, string Value)>();
            var store = new BoundedStore<string, string>(10, new LruPolicy<string>(), (k, v) => evicted.Add((k, v)));

            store.Put("a", "1");
            store.Put("a", "2");

            Assert.AreEqual(1, evicted.Count, "覆盖旧值应触发一次 onEvicted");
            Assert.AreEqual("a", evicted[0].Key);
            Assert.AreEqual("1", evicted[0].Value);
            Assert.IsTrue(store.TryGet("a", out var current));
            Assert.AreEqual("2", current);
            Assert.AreEqual(1, store.Count, "覆盖不应增加条目数");
        }

        // B-2 single-flight：同 key 并发 GetOrAdd 仅执行一次 factory，结果复用。
        [Test]
        public void GetOrAddSameKeyConcurrentRunsFactoryOnce()
        {
            var store = new BoundedStore<string, object>(0, new LruPolicy<string>());
            int calls = 0;
            var tasks = new List<Task>();

            for (var i = 0; i < 50; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    store.GetOrAdd("k", _ =>
                    {
                        Interlocked.Increment(ref calls);
                        return new object();
                    });
                }));
            }

            Task.WaitAll(tasks.ToArray());

            Assert.AreEqual(1, calls, "single-flight：factory 应仅执行一次");
            Assert.IsTrue(store.TryGet("k", out var value));
            Assert.IsNotNull(value);
            Assert.AreEqual(1, store.Count);
        }

        // LRU：容量 2，插入 a/b/c 后 a 被淘汰。
        [Test]
        public void LruPolicyEvictsLeastRecentlyUsed()
        {
            var store = new BoundedStore<string, string>(2, new LruPolicy<string>());
            store.Put("a", "1");
            store.Put("b", "2");
            store.Put("c", "3"); // 超出容量，淘汰 a

            Assert.IsFalse(store.TryGet("a", out _), "a 应被 LRU 淘汰");
            Assert.IsTrue(store.TryGet("b", out _));
            Assert.IsTrue(store.TryGet("c", out _));
            Assert.AreEqual(2, store.Count);
        }

        // TTL：过期后 TrySelectEvictionCandidate 返回过期项。
        [Test]
        public void TtlPolicySelectsExpiredEntry()
        {
            var policy = new TtlPolicy<string>(TimeSpan.FromMilliseconds(30));
            policy.OnAdded("k");

            Assert.IsFalse(policy.TrySelectEvictionCandidate(out _), "未过期前不应淘汰");

            Thread.Sleep(60);
            Assert.IsTrue(policy.TrySelectEvictionCandidate(out var key));
            Assert.AreEqual("k", key);
        }

        // Capacity：仅保留 N 个（配合 store 容量上限）。
        [Test]
        public void CapacityPolicyKeepsOnlyNEntries()
        {
            var evicted = 0;
            var store = new BoundedStore<string, string>(2, new CapacityPolicy<string>(), (_, _) => evicted++);
            store.Put("a", "1");
            store.Put("b", "2");
            store.Put("c", "3");

            Assert.AreEqual(2, store.Count);
            Assert.AreEqual(1, evicted);
        }

        // Composite：子策略 OnAdded / OnRemoved 扇出。
        [Test]
        public void CompositePolicyFansOutToAllSubPolicies()
        {
            var p1 = new RecordingPolicy();
            var p2 = new RecordingPolicy();
            var composite = new CompositePolicy<string>(p1, p2);

            composite.OnAdded("x");
            composite.OnRemoved("x");

            Assert.AreEqual(1, p1.AddedCount);
            Assert.AreEqual(1, p1.RemovedCount);
            Assert.AreEqual(1, p2.AddedCount);
            Assert.AreEqual(1, p2.RemovedCount);
        }

        private sealed class RecordingPolicy : IStoreEvictionPolicy<string>
        {
            public int AddedCount;
            public int RemovedCount;

            public void OnAdded(string key) => AddedCount++;
            public void OnAccessed(string key) { }
            public void OnRemoved(string key) => RemovedCount++;
            public void Clear() { }
            public bool TrySelectEvictionCandidate(out string key)
            {
                key = default;
                return false;
            }
        }
    }
}
