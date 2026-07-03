using System;
using System.Collections.Generic;
using UnityEngine;

namespace General
{
    public sealed class ModelLifecycle : IDisposable
    {
        private readonly List<IModel> _models;
        private bool _loaded;

        public ModelLifecycle(IEnumerable<IModel> models)
        {
            _models = models == null ? new List<IModel>() : new List<IModel>(models);
            _models.Sort(static (a, b) => a.Priority.CompareTo(b.Priority));
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
                    Debug.Log($"[ModelLifecycle] Load {model.GetType().Name} (Priority={model.Priority})");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ModelLifecycle] Load failed: {model.GetType().Name}\n{e}");
                }
            }

            _loaded = true;
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
                    Debug.Log($"[ModelLifecycle] Unload {_models[i].GetType().Name}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ModelLifecycle] Unload failed: {_models[i].GetType().Name}\n{e}");
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
