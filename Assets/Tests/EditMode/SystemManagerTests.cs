using System;
using Core.Systems;
using Core.Systems.Events;
using Framework.TestKit.Probes;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public sealed class SystemManagerTests
    {
        private RecordingPublisher<AppStartedEvent> _startedPublisher;
        private RecordingPublisher<AppShuttingDownEvent> _shuttingDownPublisher;
        private NullLogger<SystemManager> _logger;
        private CallProbe _probe;
        [SetUp]
        public void Setup()
        {
            _startedPublisher = new RecordingPublisher<AppStartedEvent>();
            _shuttingDownPublisher = new RecordingPublisher<AppShuttingDownEvent>();
            _logger = new NullLogger<SystemManager>();
            _probe = new CallProbe();
        }

        [Test]
        public void InitAllSortsAndInitializesSystemsByPriority()
        {
            // Arrange
            var sysA = new TestSystemA("SysA", 10, _probe);
            var sysB = new TestSystemB("SysB", 5, _probe);
            var sysC = new TestSystemC("SysC", 20, _probe);

            var manager = new SystemManager(
                new ISystem[] { sysA, sysB, sysC },
                _startedPublisher,
                _shuttingDownPublisher,
                _logger
            );

            // Act
            manager.InitAll();

            // Assert
            Assert.IsTrue(manager.Initialized);
            Assert.IsFalse(manager.HasInitFailures);
            _probe.AssertSequence("SysB.Init", "SysA.Init", "SysC.Init");
            Assert.AreEqual(1, _startedPublisher.PublishedEvents.Count, "AppStartedEvent should be published once.");
        }

        [Test]
        public void ShutdownAllExecutesInReverseOrderOfInitialization()
        {
            // Arrange
            var sysA = new TestSystemA("SysA", 10, _probe);
            var sysB = new TestSystemB("SysB", 5, _probe);
            var sysC = new TestSystemC("SysC", 20, _probe);

            var manager = new SystemManager(
                new ISystem[] { sysA, sysB, sysC },
                _startedPublisher,
                _shuttingDownPublisher,
                _logger
            );

            manager.InitAll();
            _probe.Clear();

            // Act
            manager.ShutdownAll();

            // Assert
            Assert.IsFalse(manager.Initialized);
            _probe.AssertSequence("SysC.Shutdown", "SysA.Shutdown", "SysB.Shutdown");
            Assert.AreEqual(1, _shuttingDownPublisher.PublishedEvents.Count, "AppShuttingDownEvent should be published once.");
        }

        [Test]
        public void TickingDispatchesToTickableSystems()
        {
            // Arrange
            var sysA = new TestTickableSystem("SysA", 5, _probe);
            var sysB = new TestSystemB("SysB", 10, _probe); // Non-tickable

            var manager = new SystemManager(
                new ISystem[] { sysA, sysB },
                _startedPublisher,
                _shuttingDownPublisher,
                _logger
            );

            manager.InitAll();
            _probe.Clear();

            // Act
            manager.Tick();
            manager.LateTick();
            manager.FixedTick();

            // Assert
            Assert.AreEqual(3, _probe.Calls.Count);
            Assert.IsTrue(_probe.Calls[0].StartsWith("SysA.Update"));
            Assert.IsTrue(_probe.Calls[1].StartsWith("SysA.LateUpdate"));
            Assert.IsTrue(_probe.Calls[2].StartsWith("SysA.FixedUpdate"));
        }

        [Test]
        public void InitFailuresAreTrackedAndDoNotCrashEntireInitialization()
        {
            // Arrange
            var sysA = new TestSystemA("SysA", 5, _probe);
            var sysB = new BrokenSystem("SysB", 10);
            var sysC = new TestSystemC("SysC", 15, _probe);

            var manager = new SystemManager(
                new ISystem[] { sysA, sysB, sysC },
                _startedPublisher,
                _shuttingDownPublisher,
                _logger
            );

            // Act
            manager.InitAll();

            // Assert
            Assert.IsFalse(manager.Initialized);
            Assert.IsTrue(manager.HasInitFailures);
            CollectionAssert.Contains(manager.FailedSystemNames, "BrokenSystem");
            _probe.AssertSequence("SysA.Init", "SysC.Init"); // SysA and SysC still initialized
            Assert.AreEqual(0, _startedPublisher.PublishedEvents.Count, "AppStartedEvent must NOT be published if there are failures.");
        }

        // --- Helpers ---

        private sealed class TestSystemA : TestSystemBase { public TestSystemA(string name, int priority, CallProbe probe) : base(name, priority, probe) { } }
        private sealed class TestSystemB : TestSystemBase { public TestSystemB(string name, int priority, CallProbe probe) : base(name, priority, probe) { } }
        private sealed class TestSystemC : TestSystemBase { public TestSystemC(string name, int priority, CallProbe probe) : base(name, priority, probe) { } }

        private class TestSystemBase : ISystem
        {
            private readonly string _name;
            private readonly CallProbe _probe;

            public TestSystemBase(string name, int priority, CallProbe probe)
            {
                _name = name;
                Priority = priority;
                _probe = probe;
            }

            public int Priority { get; }

            public void Init() => _probe.Record($"{_name}.Init");
            public void Shutdown() => _probe.Record($"{_name}.Shutdown");
        }

        private sealed class TestTickableSystem : ITickableSystem
        {
            private readonly string _name;
            private readonly CallProbe _probe;

            public TestTickableSystem(string name, int priority, CallProbe probe)
            {
                _name = name;
                Priority = priority;
                _probe = probe;
            }

            public int Priority { get; }

            public void Init() => _probe.Record($"{_name}.Init");
            public void Shutdown() => _probe.Record($"{_name}.Shutdown");

            public void Update(float deltaTime) => _probe.Record($"{_name}.Update({deltaTime:F2})");
            public void LateUpdate(float deltaTime) => _probe.Record($"{_name}.LateUpdate({deltaTime:F2})");
            public void FixedUpdate(float fixedDeltaTime) => _probe.Record($"{_name}.FixedUpdate({fixedDeltaTime:F2})");
        }

        private sealed class BrokenSystem : ISystem
        {
            public BrokenSystem(string name, int priority)
            {
                Priority = priority;
            }

            public int Priority { get; }

            public void Init()
            {
                throw new InvalidOperationException("Simulated system failure.");
            }

            public void Shutdown() { }
        }

        private sealed class NullLogger<T> : ILogger<T>
        {
            public IDisposable BeginScope<TState>(TState state) => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
        }
    }
}
