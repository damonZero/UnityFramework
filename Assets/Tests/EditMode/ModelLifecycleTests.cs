using System;
using System.Collections.Generic;
using Core.Systems;
using General;
using Framework.TestKit.Probes;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public sealed class ModelLifecycleTests
    {
        private FakeCoreStartupStatus _coreStartupStatus;
        private NullLogger<ModelLifecycle> _logger;
        private CallProbe _probe;

        [SetUp]
        public void Setup()
        {
            _coreStartupStatus = new FakeCoreStartupStatus { IsStarted = true, HasInitFailures = false };
            _logger = new NullLogger<ModelLifecycle>();
            _probe = new CallProbe();
        }

        [Test]
        public void LoadAllLoadsModelsOrderedByPriority()
        {
            // Arrange
            var modelA = new TestModel("ModelA", 10, _probe);
            var modelB = new TestModel("ModelB", 5, _probe);
            var modelC = new TestModel("ModelC", 20, _probe);

            var lifecycle = new ModelLifecycle(
                new IModel[] { modelA, modelB, modelC },
                _coreStartupStatus,
                _logger
            );

            // Act
            lifecycle.PostStart(); // PostStart calls LoadAll if Core is healthy

            // Assert
            _probe.AssertSequence("ModelB.Load", "ModelA.Load", "ModelC.Load");
        }

        [Test]
        public void UnloadAllUnloadsModelsInReverseOrder()
        {
            // Arrange
            var modelA = new TestModel("ModelA", 10, _probe);
            var modelB = new TestModel("ModelB", 5, _probe);
            var modelC = new TestModel("ModelC", 20, _probe);

            var lifecycle = new ModelLifecycle(
                new IModel[] { modelA, modelB, modelC },
                _coreStartupStatus,
                _logger
            );

            lifecycle.PostStart();
            _probe.Clear();

            // Act
            lifecycle.Dispose(); // Dispose calls UnloadAll

            // Assert
            _probe.AssertSequence("ModelC.Unload", "ModelA.Unload", "ModelB.Unload");
        }

        [Test]
        public void CoreStartupFailureBlocksModelLoading()
        {
            // Arrange
            _coreStartupStatus.IsStarted = false; // Core not started
            var modelA = new TestModel("ModelA", 5, _probe);

            var lifecycle = new ModelLifecycle(
                new IModel[] { modelA },
                _coreStartupStatus,
                _logger
            );

            // Act
            lifecycle.PostStart();

            // Assert
            Assert.AreEqual(0, _probe.Count, "Models must not load if Core is not started.");

            // Arrange 2: Core has startup failures
            _probe.Clear();
            _coreStartupStatus.IsStarted = true;
            _coreStartupStatus.HasInitFailures = true;

            var lifecycle2 = new ModelLifecycle(
                new IModel[] { modelA },
                _coreStartupStatus,
                _logger
            );

            // Act 2
            lifecycle2.PostStart();

            // Assert 2
            Assert.AreEqual(0, _probe.Count, "Models must not load if Core has init failures.");
        }

        [Test]
        public void ModelLoadFailureDoesNotBlockOtherModels()
        {
            // Arrange
            var modelA = new TestModel("ModelA", 5, _probe);
            var modelB = new BrokenModel("ModelB", 10);
            var modelC = new TestModel("ModelC", 15, _probe);

            var lifecycle = new ModelLifecycle(
                new IModel[] { modelA, modelB, modelC },
                _coreStartupStatus,
                _logger
            );

            // Act
            lifecycle.PostStart();

            // Assert
            _probe.AssertSequence("ModelA.Load", "ModelC.Load"); // ModelA and ModelC are loaded successfully
        }

        // --- Helpers ---



        private sealed class NullLogger<T> : ILogger<T>
        {
            public IDisposable BeginScope<TState>(TState state) => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
        }

        private sealed class TestModel : IModel
        {
            private readonly string _name;
            private readonly CallProbe _probe;

            public TestModel(string name, int priority, CallProbe probe)
            {
                _name = name;
                Priority = priority;
                _probe = probe;
            }

            public int Priority { get; }

            public void Load() => _probe.Record($"{_name}.Load");
            public void Unload() => _probe.Record($"{_name}.Unload");
        }

        private sealed class BrokenModel : IModel
        {
            public BrokenModel(string name, int priority)
            {
                Priority = priority;
            }

            public int Priority { get; }

            public void Load()
            {
                throw new InvalidOperationException("Simulated business model loading exception.");
            }

            public void Unload() { }
        }

        private sealed class FakeCoreStartupStatus : ICoreStartupStatus
        {
            public bool IsStarted { get; set; }
            public bool HasInitFailures { get; set; }
            public List<string> FailedSystemNames { get; } = new();
            IReadOnlyList<string> ICoreStartupStatus.FailedSystemNames => FailedSystemNames;
        }
    }
}
