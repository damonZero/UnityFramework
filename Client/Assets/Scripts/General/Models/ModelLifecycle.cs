using System;
using System.Collections.Generic;
using Core.Systems;
using Microsoft.Extensions.Logging;
using VContainer;
using VContainer.Unity;

namespace General
{
    /// <summary>
    /// 模型生命周期管理器。由 VContainer <see cref="IPostStartable"/> 在 Core 启动成功后驱动。
    /// 按 scoped 类型契约（<see cref="IReadOnlyList{Type}"/>）只管理本层程序集扫描出的模型，
    /// 运行期通过 <see cref="IObjectResolver"/> 惰性解析实例，保留构造函数 DI。
    /// 用 Type[] 而非 <see cref="IEnumerable{IModel}"/>：VContainer 子 scope 解析集合会聚合父 scope
    /// 的 IModel 注册，导致 Project 的 ModelLifecycle 拿到 General 模型重复加载（分层启动计划 §0.2）。
    /// 单个模型 Load 失败不中断其他模型，但汇总失败并暴露 <see cref="IModelStartupStatus"/>，
    /// 供上层决定是否继续启动。
    /// </summary>
    public sealed class ModelLifecycle : IPostStartable, IDisposable, IModelStartupStatus
    {
        private readonly IReadOnlyList<Type> _modelTypes;
        private readonly IObjectResolver _resolver;
        private readonly ICoreStartupStatus _coreStartupStatus;
        private readonly ILogger<ModelLifecycle> _logger;
        private readonly List<IModel> _loadedModels = new();
        private readonly List<string> _failedModelNames = new();
        private bool _loaded;

        public bool IsLoaded => _loaded && _failedModelNames.Count == 0;
        public bool HasFailures => _failedModelNames.Count > 0;
        public IReadOnlyList<string> FailedModelNames => _failedModelNames;

        public ModelLifecycle(
            IReadOnlyList<Type> modelTypes,
            IObjectResolver resolver,
            ICoreStartupStatus coreStartupStatus,
            ILogger<ModelLifecycle> logger)
        {
            _modelTypes = modelTypes ?? Array.Empty<Type>();
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _coreStartupStatus = coreStartupStatus ?? throw new ArgumentNullException(nameof(coreStartupStatus));
            _logger = logger;
        }

        public void LoadAll()
        {
            if (_loaded)
                return;

            _loadedModels.Clear();
            _failedModelNames.Clear();

            // 先把所有类型解析为实例并按 Priority 排序，再依次 Load。
            // IReadOnlyList<Type> 契约下 Type 自身无 Priority，必须解析实例后按实例排序。
            var resolved = new List<IModel>(_modelTypes.Count);
            foreach (var type in _modelTypes)
            {
                try
                {
                    resolved.Add((IModel)_resolver.Resolve(type));
                }
                catch (Exception e)
                {
                    _failedModelNames.Add(type.Name);
                    ModelLifecycleLog.ModelResolveFailed(_logger, type.Name, e);
                }
            }

            resolved.Sort(static (a, b) => a.Priority.CompareTo(b.Priority));

            foreach (var model in resolved)
            {
                try
                {
                    model.Load();
                    _loadedModels.Add(model);
                    ModelLifecycleLog.ModelLoaded(_logger, model.GetType().Name, model.Priority);
                }
                catch (Exception e)
                {
                    _failedModelNames.Add(model.GetType().Name);
                    ModelLifecycleLog.ModelLoadFailed(_logger, model.GetType().Name, e);
                }
            }

            _loaded = true;
        }

        public void PostStart()
        {
            if (!_coreStartupStatus.IsStarted || _coreStartupStatus.HasInitFailures)
            {
                var failedSystems = _coreStartupStatus.FailedSystemNames.Count == 0
                    ? "<none>"
                    : string.Join(", ", _coreStartupStatus.FailedSystemNames);
                ModelLifecycleLog.CoreStartupFailed(_logger, failedSystems);
                return;
            }

            LoadAll();
        }

        public void UnloadAll()
        {
            if (!_loaded)
                return;

            for (var i = _loadedModels.Count - 1; i >= 0; i--)
            {
                try
                {
                    _loadedModels[i].Unload();
                    ModelLifecycleLog.ModelUnloaded(_logger, _loadedModels[i].GetType().Name);
                }
                catch (Exception e)
                {
                    ModelLifecycleLog.ModelUnloadFailed(_logger, _loadedModels[i].GetType().Name, e);
                }
            }

            _loadedModels.Clear();
            _failedModelNames.Clear();
            _loaded = false;
        }

        public void Dispose()
        {
            UnloadAll();
        }
    }
}
