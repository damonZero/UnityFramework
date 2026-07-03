using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;

namespace Boot
{
    public sealed class BootstrapContext
    {
        private readonly Dictionary<Type, object> _values = new();
        private readonly HashSet<string> _configuredPrefabPaths = new();
        private readonly List<GameObject> _stageInstances = new();

        public BootstrapContext(IContainerBuilder builder, Transform stageRoot)
        {
            Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            StageRoot = stageRoot;
        }

        public IContainerBuilder Builder { get; }
        public Transform StageRoot { get; }

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

        public void ConfigurePrefab(string prefabPath)
        {
            if (string.IsNullOrWhiteSpace(prefabPath))
                return;

            if (!_configuredPrefabPaths.Add(prefabPath))
                throw new InvalidOperationException($"Bootstrap prefab configured more than once: {prefabPath}");

            var prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Bootstrap prefab not found in Resources: {prefabPath}");

            var instance = UnityEngine.Object.Instantiate(prefab, StageRoot);
            instance.name = prefab.name;
            _stageInstances.Add(instance);

            var stages = instance.GetComponentsInChildren<IBootstrapStage>(true)
                .OrderBy(stage => stage.Priority)
                .ToArray();

            if (stages.Length == 0)
                throw new InvalidOperationException($"Bootstrap prefab has no IBootstrapStage: {prefabPath}");

            foreach (var stage in stages)
            {
                Debug.Log($"[Boot] Configure stage: {stage.StageName} (Priority={stage.Priority})");
                stage.Configure(this);
            }
        }
    }
}
