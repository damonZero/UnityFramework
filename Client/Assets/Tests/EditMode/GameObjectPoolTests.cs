using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Framework.Pool;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.EditMode
{
    [TestFixture]
    public sealed class GameObjectPoolTests
    {
        [TearDown]
        public void TearDown()
        {
            PoolDependencies.LoadAssetAsync = null;
            PoolDependencies.ReleaseAssetByPath = null;
            PoolDependencies.LoadGates.Clear();
        }

        private static void FakeLoadCube()
        {
            PoolDependencies.LoadAssetAsync = (path, parent) => UniTask.FromResult(GameObject.CreatePrimitive(PrimitiveType.Cube));
            PoolDependencies.ReleaseAssetByPath = _ => { };
        }

        // A-D1 / C-2：Recycle 超过 maxIdlePerPrefab 的实例应被 Destroy 而非入栈。
        // 注：本工程 Unity Test Framework 版本不识别 `async Task` + `await UniTask` 测试（报
        // "Method has non-void return value"）。统一用 void + .GetAwaiter().GetResult() 同步执行，
        // 与 CallingFromBackgroundThreadThrows 保持一致。
        [Test]
        public void RecycleOverMaxIdleDestroysExcessInstances()
        {
            FakeLoadCube();

            const int maxIdle = 3;
            var pool = new GameObjectPool(null, 64, maxIdlePerPrefab: maxIdle);
            const string prefabPath = "cube";

            var instances = new List<GameObject>();
            for (var i = 0; i < 5; i++)
            {
                instances.Add(pool.GetAsync(prefabPath).GetAwaiter().GetResult());
            }

            foreach (var inst in instances)
            {
                pool.Recycle(inst);
            }

            Assert.AreEqual(maxIdle, pool.GetIdleCount(prefabPath));
        }

        // A-B1：GetAsync 弹出已被 Unity 销毁的实例后，注册表不应残留 null 条目。
        [Test]
        public void GetAsyncConsumingDestroyedInstanceDoesNotLeakNullRegistryEntry()
        {
            FakeLoadCube();

            var pool = new GameObjectPool(null, 64);
            const string prefabPath = "cube";

            var a = pool.GetAsync(prefabPath).GetAwaiter().GetResult();
            pool.Recycle(a);

            // 模拟 Unity 销毁该 idle 实例（EditMode 下用 DestroyImmediate 立即可见为假 null）。
            Object.DestroyImmediate(a);

            var b = pool.GetAsync(prefabPath).GetAwaiter().GetResult();

            Assert.IsNotNull(b, "应返回新实例而非 null");
            Assert.AreNotSame(a, b, "不应复用一个已被销毁的实例");
            Assert.AreEqual(1, pool.GetActiveCount(prefabPath));

            // 反射检查合并后的 _states：被销毁的 null 条目必须已被清理。
            // （PrefabPoolState 为 internal，跨程序集只能通过反射读取其字段，避免引用内部类型。）
            var field = typeof(GameObjectPool).GetField("_states", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "反射读取 _states 失败");
            var states = (IDictionary)field.GetValue(pool);

            Assert.IsTrue(states.Contains(prefabPath), "新实例应已登记");
            var stateObj = states[prefabPath];
            // PrefabPoolState.Instances 是 public 字段，反射需同时指定 Public | NonPublic，否则 GetField 返回 null。
            var instancesField = stateObj.GetType().GetField("Instances", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(instancesField, "反射读取 Instances 失败");
            var instancesSet = (System.Collections.Generic.ICollection<GameObject>)instancesField.GetValue(stateObj);

            // B1 意图：被销毁的实例不应作为 null 条目残留在注册表中。
            // 直接断言集合中没有 Unity-null 条目（不依赖 Unity 销毁对象的哈希稳定性，比精确计数更鲁棒，见 Review M1）。
            bool hasNullEntry = false;
            foreach (GameObject x in instancesSet)
            {
                if (x == null)
                {
                    hasNullEntry = true;
                }
            }

            Assert.IsFalse(hasNullEntry, "被销毁的 null 条目应已被 UnregisterInstance 清理，注册表不应残留 null");
        }

        // C-2：显式注入 CapacityInstancePolicy 控制库存上限。
        [Test]
        public void CapacityInstancePolicyControlsRetention()
        {
            FakeLoadCube();

            var policy = new CapacityInstancePolicy(2);
            var pool = new GameObjectPool(null, 64, recyclePolicy: policy);
            const string prefabPath = "cube";

            var instances = new List<GameObject>();
            for (var i = 0; i < 4; i++)
            {
                instances.Add(pool.GetAsync(prefabPath).GetAwaiter().GetResult());
            }

            foreach (var inst in instances)
            {
                pool.Recycle(inst);
            }

            Assert.AreEqual(2, pool.GetIdleCount(prefabPath));
        }

        // C-3 反向索引防双回收：同一实例重复 Recycle 不重复入库。
        [Test]
        public void ReverseIndexPreventsDoubleRecycle()
        {
            FakeLoadCube();

            var pool = new GameObjectPool(null, 64);
            const string prefabPath = "cube";

            var inst = pool.GetAsync(prefabPath).GetAwaiter().GetResult();
            pool.Recycle(inst);
            pool.Recycle(inst); // 双回收应被忽略

            Assert.AreEqual(1, pool.GetIdleCount(prefabPath), "双回收不应重复入库");
        }

        // C-3 常驻保护：persistent 路径实例永不被容量淘汰。
        [Test]
        public void PersistentPathNeverEvictedByCapacity()
        {
            FakeLoadCube();

            const int maxIdle = 2;
            var pool = new GameObjectPool(null, 64, maxIdlePerPrefab: maxIdle);
            pool.MarkPersistent("cube");

            var instances = new List<GameObject>();
            for (var i = 0; i < 5; i++)
            {
                instances.Add(pool.GetAsync("cube").GetAwaiter().GetResult());
            }

            foreach (var inst in instances)
            {
                pool.Recycle(inst);
            }

            Assert.AreEqual(5, pool.GetIdleCount("cube"), "常驻路径实例应全部保留，不受容量上限影响");
        }

        // C-4 [MainThread] 运行时断言：子线程调用 GetAsync 应抛 InvalidOperationException。
        [Test]
        public void CallingFromBackgroundThreadThrows()
        {
            FakeLoadCube();
            var pool = new GameObjectPool(null, 64);

            Exception captured = null;
            var task = Task.Run(() =>
            {
                try
                {
                    // 构造在主线程，GetAsync 入口断言会在子线程抛出。
                    pool.GetAsync("cube").GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    captured = e;
                }
            });
            task.Wait();

            var unwrapped = captured;
            while (unwrapped != null && !(unwrapped is InvalidOperationException) && unwrapped.InnerException != null)
            {
                unwrapped = unwrapped.InnerException;
            }

            Assert.IsInstanceOf<InvalidOperationException>(unwrapped, "子线程调用应触发 [MainThread] 断言");
        }

        // C-3 反向索引污染（错误归还到非注册路径）：销毁实例时应补足注册路径的活跃计数，
        // 否则 GetActiveCount(registeredPath) 会虚高 1（整体 Review 发现的 Bug-1）。
        [Test]
        public void WrongPathRecycleDoesNotLeakActiveCount()
        {
            FakeLoadCube();

            var pool = new GameObjectPool(null, 64);
            const string registeredPath = "cubeA";
            const string wrongPath = "cubeB";

            var inst = pool.GetAsync(registeredPath).GetAwaiter().GetResult();
            Assert.IsNotNull(inst, "应成功获取实例");
            Assert.AreEqual(1, pool.GetActiveCount(registeredPath), "获取后注册路径活跃计数应为 1");

            // 模拟错误归还：通过反射把实例的 tag 指向另一个路径（PoolInstanceTag 是 internal）。
            var tagType = typeof(GameObjectPool).Assembly.GetType("Framework.Pool.PoolInstanceTag");
            Assert.IsNotNull(tagType, "反射获取 PoolInstanceTag 类型失败");
            var tagComponent = inst.GetComponent(tagType);
            Assert.IsNotNull(tagComponent, "实例应持有 PoolInstanceTag");
            var prefabPathField = tagType.GetField("PrefabPath", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(prefabPathField, "反射读取 PoolInstanceTag.PrefabPath 失败");
            prefabPathField.SetValue(tagComponent, wrongPath);

            // 污染检测会打 LogError，NUnit 默认视为测试失败，需显式声明为预期日志。
            LogAssert.Expect(LogType.Error, $"[GameObjectPool] 实例被错误归还至路径 '{wrongPath}'，但其注册路径为 '{registeredPath}'（反向索引污染，已销毁不入库）。");

            pool.Recycle(inst);

            // 修复后：错误归还销毁实例时，应补足 registeredPath 的活跃计数（减回 0）。
            Assert.AreEqual(0, pool.GetActiveCount(registeredPath), "反向索引污染销毁后，注册路径活跃计数不应泄漏");
        }

        [Test]
        public void InstanceFromAnotherPoolIsRejectedAndDoesNotChangeCounts()
        {
            FakeLoadCube();

            var ownerPool = new GameObjectPool(null, 64);
            var otherPool = new GameObjectPool(null, 64);
            const string prefabPath = "cube";

            var inst = ownerPool.GetAsync(prefabPath).GetAwaiter().GetResult();
            Assert.AreEqual(1, ownerPool.GetActiveCount(prefabPath));
            Assert.AreEqual(0, otherPool.GetActiveCount(prefabPath));

            LogAssert.Expect(LogType.Error, $"[GameObjectPool] 实例 '{inst.name}' 不属于当前对象池，PrefabPath='{prefabPath}'（已拒绝入库）。");

            otherPool.Recycle(inst);

            Assert.AreEqual(1, ownerPool.GetActiveCount(prefabPath), "外部池拒收后，原池活跃计数不应被改动。");
            Assert.AreEqual(0, otherPool.GetActiveCount(prefabPath), "外部池不能为未登记实例创建负数或污染计数。");
            Assert.AreEqual(0, otherPool.GetIdleCount(prefabPath), "外部池不能把未登记实例放入 idle 库存。");

            ownerPool.Recycle(inst);
            Assert.AreEqual(1, ownerPool.GetIdleCount(prefabPath));
            Assert.AreEqual(0, ownerPool.GetActiveCount(prefabPath));
        }

        [Test]
        public void ClearReleasesCachedPrefabsThroughBoundedStoreClear()
        {
            var released = new List<string>();
            PoolDependencies.LoadAssetAsync = (path, parent) => UniTask.FromResult(GameObject.CreatePrimitive(PrimitiveType.Cube));
            PoolDependencies.ReleaseAssetByPath = path => released.Add(path);

            var pool = new GameObjectPool(null, 64);
            const string prefabPath = "cube";
            var inst = pool.GetAsync(prefabPath).GetAwaiter().GetResult();
            pool.Recycle(inst);

            pool.Clear();

            CollectionAssert.AreEqual(new[] { prefabPath }, released, "Clear should release each cached prefab exactly once.");
            Assert.IsFalse(PoolDependencies.LoadGates.ContainsKey(prefabPath), "Clear should remove the prefab load gate.");
        }
    }
}
