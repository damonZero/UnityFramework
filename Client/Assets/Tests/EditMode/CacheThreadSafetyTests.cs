using System;
using System.Threading;
using System.Threading.Tasks;
using Framework.Cache;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public sealed class CacheThreadSafetyTests
    {
        [Test]
        public void EvictionCallbackIsInvokedOutsideLock()
        {
            // Arrange
            // Create a cache of capacity 1, meaning adding a second item will evict the first.
            // In the eviction callback, we spawn a Task that accesses the cache.
            // If the callback is executed INSIDE the lock, the Task will block trying to acquire the lock,
            // resulting in a deadlock or timeout.
            // If the callback is executed OUTSIDE the lock, the Task will succeed immediately.
            BoundedStore<string, string> cache = null;
            var evictionCalled = false;
            var otherThreadAccessSucceeded = false;
            Exception otherThreadException = null;

            cache = new BoundedStore<string, string>(1, new LruPolicy<string>(), (key, val) =>
            {
                evictionCalled = true;

                // Spawn a task to access the cache concurrently.
                var task = Task.Run(() =>
                {
                    try
                    {
                        // Try to get another item from the cache. This requires acquiring the cache lock.
                        return cache.TryGet("key2", out _);
                    }
                    catch (Exception ex)
                    {
                        otherThreadException = ex;
                        return false;
                    }
                });

                // Wait for the task with a short timeout.
                // If it blocks (inside lock), this will timeout.
                // If it doesn't block (outside lock), it will complete instantly.
                if (task.Wait(TimeSpan.FromSeconds(2)))
                {
                    otherThreadAccessSucceeded = task.Result;
                }
            });

            // Act
            cache.Put("key1", "val1");
            cache.Put("key2", "val2"); // This triggers eviction of key1

            // Assert
            Assert.IsTrue(evictionCalled, "Eviction callback should be invoked.");
            Assert.IsNull(otherThreadException, $"Concurrent thread threw an exception: {otherThreadException}");
            Assert.IsTrue(otherThreadAccessSucceeded, "Concurrent thread should successfully access the cache during eviction because lock is released.");
        }

        [Test]
        public void GetOrAddFactoryRunsOutsideLock()
        {
            BoundedStore<string, string> cache = null;
            var factoryCalled = false;
            var concurrentAccessSucceeded = false;

            cache = new BoundedStore<string, string>(5, new LruPolicy<string>());

            // Act
            cache.GetOrAdd("key1", key =>
            {
                factoryCalled = true;

                // Inside the factory, try to query another cache key concurrently
                var task = Task.Run(() => cache.TryGet("other_key", out _));

                if (task.Wait(TimeSpan.FromSeconds(2)))
                {
                    concurrentAccessSucceeded = true;
                }

                return "val1";
            });

            // Assert
            Assert.IsTrue(factoryCalled);
            Assert.IsTrue(concurrentAccessSucceeded, "Cache should not be locked while running GetOrAdd factory.");
        }

        [Test]
        public void GetOrAddEvictsOutsideLock()
        {
            BoundedStore<string, string> cache = null;
            var evictionCalled = false;
            var otherThreadAccessSucceeded = false;

            cache = new BoundedStore<string, string>(1, new LruPolicy<string>(), (key, val) =>
            {
                evictionCalled = true;
                var task = Task.Run(() => cache.TryGet("key2", out _));
                if (task.Wait(TimeSpan.FromSeconds(2)))
                {
                    otherThreadAccessSucceeded = task.Result;
                }
            });

            cache.Put("key1", "val1");
            cache.GetOrAdd("key2", _ => "val2"); // Trigger eviction

            Assert.IsTrue(evictionCalled);
            Assert.IsTrue(otherThreadAccessSucceeded, "Lock must be released when eviction callback runs from GetOrAdd.");
        }

        [Test]
        public void GetOrAddDiscardsDuplicateOutsideLock()
        {
            BoundedStore<string, string> cache = null;
            var evictionCalledOnDiscard = false;
            string evictedKey = null;
            string evictedValue = null;

            cache = new BoundedStore<string, string>(5, new LruPolicy<string>(), (key, val) =>
            {
                evictionCalledOnDiscard = true;
                evictedKey = key;
                evictedValue = val;
            });

            // Put initial item to let us run concurrently
            cache.Put("other_key", "other_val");

            var factoryBlockGate = new ManualResetEventSlim(false);
            var threadPutDoneGate = new ManualResetEventSlim(false);

            var task = Task.Run(() =>
            {
                return cache.GetOrAdd("target_key", key =>
                {
                    // Block in factory to let main thread put a value
                    factoryBlockGate.Set();
                    threadPutDoneGate.Wait();
                    return "discarded_val";
                });
            });

            // Wait for factory to start running
            factoryBlockGate.Wait();

            // Main thread inserts the key in the meantime
            cache.Put("target_key", "winning_val");

            // Let the factory finish
            threadPutDoneGate.Set();

            var resultValue = task.GetAwaiter().GetResult();

            // Assert
            Assert.AreEqual("winning_val", resultValue, "GetOrAdd should return the winning value from the double check.");
            Assert.IsTrue(evictionCalledOnDiscard, "The discarded value should be cleaned up by calling _onEvicted.");
            Assert.AreEqual("target_key", evictedKey);
            Assert.AreEqual("discarded_val", evictedValue);
        }
    }
}
