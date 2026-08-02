using System;
using System.Collections.Generic;
using Core.Systems;
using General;
using Framework.TestKit.Probes;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using VContainer;
using VContainer.Diagnostics;

namespace Tests.EditMode
{
    [TestFixture]
    public sealed class ModelLifecycleTests
    {
        private FakeCoreStartupStatus _coreStartupStatus;
        private NullLogger<ModelLifecycle> _logger;
        private CallProbe _probe;
        private TestResolver _resolver;

        [SetUp]
        public void Setup()
        {
            _coreStartupStatus = new FakeCoreStartupStatus { IsStarted = true, HasInitFailures = false };
            _logger = new NullLogger<ModelLifecycle>();
            _probe = new CallProbe();
            _resolver = new TestResolver();
        }

        [Test]
        public void LoadAllLoadsModelsOrderedByPriority()
        {
            // Arrange
            // IReadOnlyList<Type> 契约下，每个模型类型唯一（同类型多实例无法用 Type 区分）。
            var modelA = new TestModelA(_probe);
            var modelB = new TestModelB(_probe);
            var modelC = new TestModelC(_probe);
            _resolver.Add(modelA).Add(modelB).Add(modelC);

            var lifecycle = CreateLifecycle(typeof(TestModelA), typeof(TestModelB), typeof(TestModelC));

            // Act
            lifecycle.PostStart(); // PostStart calls LoadAll if Core is healthy

            // Assert
            _probe.AssertSequence("ModelB.Load", "ModelA.Load", "ModelC.Load");
        }

        [Test]
        public void UnloadAllUnloadsModelsInReverseOrder()
        {
            // Arrange
            var modelA = new TestModelA(_probe);
            var modelB = new TestModelB(_probe);
            var modelC = new TestModelC(_probe);
            _resolver.Add(modelA).Add(modelB).Add(modelC);

            var lifecycle = CreateLifecycle(typeof(TestModelA), typeof(TestModelB), typeof(TestModelC));
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
            var modelA = new TestModelA(_probe);
            _resolver.Add(modelA);

            var lifecycle = CreateLifecycle(typeof(TestModelA));

            // Act
            lifecycle.PostStart();

            // Assert
            Assert.AreEqual(0, _probe.Count, "Models must not load if Core is not started.");

            // Arrange 2: Core has startup failures
            _probe.Clear();
            _coreStartupStatus.IsStarted = true;
            _coreStartupStatus.HasInitFailures = true;

            var lifecycle2 = CreateLifecycle(typeof(TestModelA));

            // Act 2
            lifecycle2.PostStart();

            // Assert 2
            Assert.AreEqual(0, _probe.Count, "Models must not load if Core has init failures.");
        }

        [Test]
        public void ModelLoadFailureDoesNotBlockOtherModels()
        {
            // Arrange
            var modelA = new TestModelA(_probe);
            var modelB = new BrokenModel(_probe);
            var modelC = new TestModelC(_probe);
            _resolver.Add(modelA).Add(modelB).Add(modelC);

            var lifecycle = CreateLifecycle(typeof(TestModelA), typeof(BrokenModel), typeof(TestModelC));

            // Act
            lifecycle.PostStart();

            // Assert
            _probe.AssertSequence("ModelA.Load", "ModelC.Load"); // ModelA and ModelC are loaded successfully
            Assert.That(lifecycle.HasFailures, Is.True);
            Assert.That(lifecycle.IsLoaded, Is.False);
            Assert.That(lifecycle.FailedModelNames, Does.Contain("BrokenModel"));
        }

        private ModelLifecycle CreateLifecycle(params Type[] types)
        {
            return new ModelLifecycle(
                types,
                _resolver,
                _coreStartupStatus,
                _logger
            );
        }

        // --- Helpers ---

        private sealed class TestResolver : IObjectResolver
        {
            private readonly Dictionary<Type, object> _map = new();

            public TestResolver Add(IModel model)
            {
                _map[model.GetType()] = model;
                return this;
            }

            public object ApplicationOrigin => null;
            public DiagnosticsCollector Diagnostics { get; set; }

            public object Resolve(Type type, object key = null) => _map[type];
            public object Resolve(Registration registration) => _map[registration.ImplementationType];
            public bool TryResolve(Type type, out object resolved, object key = null)
            {
                if (_map.TryGetValue(type, out var value))
                {
                    resolved = value;
                    return true;
                }

                resolved = null;
                return false;
            }

            public bool TryGetRegistration(Type type, out Registration registration, object key = null)
            {
                registration = null;
                return false;
            }

            public IScopedObjectResolver CreateScope(Action<IContainerBuilder> installation = null)
                => throw new NotSupportedException();
            public void Inject(object instance) { }
            public void Dispose() { }
        }

        private sealed class NullLogger<T> : ILogger<T>
        {
            public IDisposable BeginScope<TState>(TState state) => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
        }

        private abstract class NamedModel : IModel
        {
            private readonly CallProbe _probe;

            protected NamedModel(CallProbe probe)
            {
                _probe = probe;
            }

            public abstract int Priority { get; }

            public void Load() => _probe.Record($"{Name}.Load");
            public void Unload() => _probe.Record($"{Name}.Unload");

            protected abstract string Name { get; }
        }

        private sealed class TestModelA : NamedModel
        {
            public TestModelA(CallProbe probe) : base(probe) { }
            public override int Priority => 10;
            protected override string Name => "ModelA";
        }

        private sealed class TestModelB : NamedModel
        {
            public TestModelB(CallProbe probe) : base(probe) { }
            public override int Priority => 5;
            protected override string Name => "ModelB";
        }

        private sealed class TestModelC : NamedModel
        {
            public TestModelC(CallProbe probe) : base(probe) { }
            public override int Priority => 20;
            protected override string Name => "ModelC";
        }

        private sealed class BrokenModel : IModel
        {
            private readonly CallProbe _probe;

            public BrokenModel(CallProbe probe)
            {
                _probe = probe;
            }

            public int Priority => 15;

            public void Load()
            {
                throw new InvalidOperationException("Simulated business model loading exception.");
            }

            public void Unload() => _probe.Record("BrokenModel.Unload");
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
