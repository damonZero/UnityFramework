using System;
using System.Collections.Generic;
using Core.Systems;
using Microsoft.Extensions.Logging;
using VContainer.Unity;

namespace General
{
    public sealed class ModelLifecycle : IPostStartable, IDisposable
    {
        private readonly List<IModel> _models;
        private readonly ICoreStartupStatus _coreStartupStatus;
        private readonly ILogger<ModelLifecycle> _logger;
        private bool _loaded;

        public ModelLifecycle(
            IEnumerable<IModel> models,
            ICoreStartupStatus coreStartupStatus,
            ILogger<ModelLifecycle> logger)
        {
            _models = models == null ? new List<IModel>() : new List<IModel>(models);
            _models.Sort(static (a, b) => a.Priority.CompareTo(b.Priority));
            _coreStartupStatus = coreStartupStatus ?? throw new ArgumentNullException(nameof(coreStartupStatus));
            _logger = logger;
        }

        public void LoadAll()
        {
            if (_loaded)
                return;

            foreach (var model in _models)
            {
                try
                {
                    model.Load();
                    ModelLifecycleLog.ModelLoaded(_logger, model.GetType().Name, model.Priority);
                }
                catch (Exception e)
                {
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

            for (var i = _models.Count - 1; i >= 0; i--)
            {
                try
                {
                    _models[i].Unload();
                    ModelLifecycleLog.ModelUnloaded(_logger, _models[i].GetType().Name);
                }
                catch (Exception e)
                {
                    ModelLifecycleLog.ModelUnloadFailed(_logger, _models[i].GetType().Name, e);
                }
            }

            _loaded = false;
        }

        public void Dispose()
        {
            UnloadAll();
        }
    }
}
