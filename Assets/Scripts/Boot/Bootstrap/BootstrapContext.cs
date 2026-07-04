using System;
using System.Collections.Generic;
using Framework.Log;
using VContainer;

namespace Boot
{
    public sealed class BootstrapContext
    {
        private readonly Dictionary<Type, object> _values = new();
        private readonly HashSet<Type> _configuredStageTypes = new();

        public BootstrapContext(IContainerBuilder builder)
        {
            Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public IContainerBuilder Builder { get; }

        public void Set<T>(T value) where T : class
        {
            _values[typeof(T)] = value;
        }

        public bool TryGet<T>(out T value) where T : class
        {
            if (_values.TryGetValue(typeof(T), out var raw) && raw is T typed)
            {
                value = typed;
                return true;
            }

            value = null;
            return false;
        }

        public T GetRequired<T>() where T : class
        {
            if (TryGet<T>(out var value))
                return value;

            throw new InvalidOperationException($"Bootstrap value is missing: {typeof(T).FullName}");
        }

        public void ConfigureStages(IEnumerable<IBootstrapStage> stages)
        {
            if (stages == null)
                throw new ArgumentNullException(nameof(stages));

            var orderedStages = new List<IBootstrapStage>();
            foreach (var stage in stages)
            {
                if (stage == null)
                    continue;
                orderedStages.Add(stage);
            }
            orderedStages.Sort(static (a, b) => a.Priority.CompareTo(b.Priority));

            if (orderedStages.Count == 0)
                throw new InvalidOperationException("[Boot] Bootstrap stage list is empty.");

            foreach (var stage in orderedStages)
            {
                var type = stage.GetType();
                if (!_configuredStageTypes.Add(type))
                    throw new InvalidOperationException($"Bootstrap stage configured more than once: {type.FullName}");

                GameLog.Info($"[Boot] Configure stage: {stage.StageName} (Priority={stage.Priority})");
                stage.Configure(this);
            }
        }
    }
}
