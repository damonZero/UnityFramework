using System;
using System.Collections.Generic;
using UnityEngine;

namespace KJ.Core
{
    /// <summary>
    /// 模块管理器，驱动所有 IModule 的生命周期。
    /// 按 Priority 升序 Init，降序 Shutdown。
    /// </summary>
    public class ModuleManager : MonoBehaviour
    {
        private readonly List<IModule> _modules = new List<IModule>();
        private bool _initialized;

        /// <summary>
        /// 注册模块。必须在 InitAll 之前调用。
        /// </summary>
        public void Register(IModule module)
        {
            if (module == null)
            {
                Debug.LogError("[ModuleManager] Register: module is null");
                return;
            }

            if (_initialized)
            {
                Debug.LogError("[ModuleManager] Cannot register after InitAll");
                return;
            }

            _modules.Add(module);
            Debug.Log($"[ModuleManager] Registered: {module.GetType().Name} (Priority={module.Priority})");
        }

        /// <summary>
        /// 按优先级升序初始化所有模块。
        /// </summary>
        public void InitAll()
        {
            // 按 Priority 升序排序
            _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            Debug.Log($"[ModuleManager] InitAll: {_modules.Count} modules");
            for (int i = 0; i < _modules.Count; i++)
            {
                var m = _modules[i];
                Debug.Log($"[ModuleManager]   Init[{i}]: {m.GetType().Name} (Priority={m.Priority})");
                m.Init();
            }

            _initialized = true;
        }

        /// <summary>
        /// 按优先级降序关闭所有模块。
        /// </summary>
        public void ShutdownAll()
        {
            Debug.Log($"[ModuleManager] ShutdownAll: {_modules.Count} modules");
            for (int i = _modules.Count - 1; i >= 0; i--)
            {
                var m = _modules[i];
                Debug.Log($"[ModuleManager]   Shutdown[{i}]: {m.GetType().Name}");
                m.Shutdown();
            }

            _modules.Clear();
            _initialized = false;
        }

        /// <summary>
        /// 获取指定类型的模块实例。
        /// </summary>
        public T Get<T>() where T : class, IModule
        {
            for (int i = 0; i < _modules.Count; i++)
            {
                if (_modules[i] is T match)
                    return match;
            }

            return null;
        }

        /// <summary>
        /// 已注册模块数量。
        /// </summary>
        public int Count => _modules.Count;

        private void OnDestroy()
        {
            if (_initialized)
            {
                ShutdownAll();
            }
        }
    }
}
