using System;
using System.Collections.Generic;
using Framework.Pool;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public sealed class PoolTests
    {
        [Test]
        public void ObjectPoolRentReturnsFactoryItemAndPushesToStackOnReturn()
        {
            // Arrange
            var count = 0;
            var pool = new ObjectPool<TestItem>(
                factory: () => new TestItem { Id = ++count },
                reset: item => item.ResetCount++,
                maxIdle: 2
            );

            // Act 1: Rent from empty pool
            var item1 = pool.Rent();
            var item2 = pool.Rent();

            // Assert 1
            Assert.AreEqual(1, item1.Id);
            Assert.AreEqual(2, item2.Id);
            Assert.AreEqual(0, pool.GetStatistics().IdleCount);

            // Act 2: Return item
            pool.Return(item1);

            // Assert 2
            Assert.AreEqual(1, item1.ResetCount, "Reset callback must be invoked on return.");
            Assert.AreEqual(1, pool.GetStatistics().IdleCount);

            // Act 3: Rent again (should reuse item1)
            var reused = pool.Rent();
            Assert.AreSame(item1, reused, "Object pool must reuse returned item (LIFO).");
            Assert.AreEqual(0, pool.GetStatistics().IdleCount);
        }

        [Test]
        public void ObjectPoolPreloadInstantiatesFactoryItems()
        {
            // Arrange & Act
            var pool = new ObjectPool<TestItem>(
                factory: () => new TestItem(),
                maxIdle: 5,
                preload: 3
            );

            // Assert
            var stats = pool.GetStatistics();
            Assert.AreEqual(3, stats.IdleCount);
            Assert.AreEqual(3, stats.CreatedCount);
        }

        [Test]
        public void ObjectPoolMaxIdleDiscardsItemsOverCapacity()
        {
            // Arrange
            var pool = new ObjectPool<TestItem>(
                factory: () => new TestItem(),
                maxIdle: 2
            );

            var item1 = pool.Rent();
            var item2 = pool.Rent();
            var item3 = pool.Rent();

            // Act: Return 3 items to pool with maxIdle = 2
            pool.Return(item1);
            pool.Return(item2);
            pool.Return(item3); // This should be discarded

            // Assert
            Assert.AreEqual(2, pool.GetStatistics().IdleCount, "Pool idle stack must not exceed maxIdle.");
        }

        [Test]
        public void ObjectPoolRentLeaseAutoReturnsOnDispose()
        {
            // Arrange
            var pool = new ObjectPool<TestItem>(
                factory: () => new TestItem(),
                maxIdle: 2
            );

            TestItem itemRef;

            // Act
            using (var lease = pool.RentLease())
            {
                itemRef = lease.Value;
                Assert.IsNotNull(itemRef);
                Assert.AreEqual(0, pool.GetStatistics().IdleCount);
            } // auto-return happens here

            // Assert
            Assert.AreEqual(1, pool.GetStatistics().IdleCount, "Item must be returned to pool after lease is disposed.");
        }

        [Test]
        public void ObjectPoolIgnoresDuplicateReturnOfSameInstance()
        {
            var pool = new ObjectPool<TestItem>(
                factory: () => new TestItem(),
                maxIdle: 4
            );
            var item = pool.Rent();

            pool.Return(item);
            pool.Return(item);

            Assert.AreEqual(1, pool.GetStatistics().IdleCount, "Duplicate returns must not enqueue the same object twice.");
            var first = pool.Rent();
            var second = pool.Rent();
            Assert.AreSame(item, first);
            Assert.AreNotSame(item, second, "After duplicate return, the same instance must not be rented twice.");
        }

        [Test]
        public void CollectionPoolValueCopyDoubleDisposeDoesNotDuplicateIdleEntry()
        {
#if UNITY_ASSERTIONS
            PooledList<TestItem> copy;
            using (var list = CollectionPool.RentList<TestItem>())
            {
                var rented = list.Value;
                copy = list;
                copy.Dispose();
                list.Dispose();

                using var next = CollectionPool.RentList<TestItem>();
                Assert.AreSame(rented, next.Value);

                using var another = CollectionPool.RentList<TestItem>();
                Assert.AreNotSame(rented, another.Value, "Double-disposing a copied pooled struct must not enqueue the same List twice.");
            }
#else
            Assert.Pass("CollectionPool duplicate-return detection is enabled in Unity assertions builds.");
#endif
        }

        [Test]
        public void CollectionPoolRentListReturnsClearedListOnDispose()
        {
            // Arrange & Act
            TestItem item = new TestItem();
            using (var list = CollectionPool.RentList<TestItem>())
            {
                list.Value.Add(item);
                Assert.AreEqual(1, list.Value.Count);
            } // auto-return clears list

            // Rent list again, it should be empty
            using (var list2 = CollectionPool.RentList<TestItem>())
            {
                Assert.AreEqual(0, list2.Value.Count, "Returned collection must be cleared.");
            }
        }

        [Test]
        public void TypePoolGetOrCreateLazilyRegistersPoolAndReturnsItems()
        {
            // Arrange & Act
            var pool = TypePool.GetOrCreate<TestItem>(maxIdle: 5);
            var item = pool.Rent();
            
            // Assert
            Assert.IsNotNull(item);
            pool.Return(item);
            Assert.AreEqual(1, pool.GetStatistics().IdleCount);

            // Verify TryGet resolves it
            Assert.IsTrue(TypePool.TryGet<TestItem>(out var resolvedPool));
            Assert.AreSame(pool, resolvedPool);
        }

        // --- Helpers ---

        private sealed class TestItem
        {
            public int Id { get; set; }
            public int ResetCount { get; set; }
        }
    }
}
