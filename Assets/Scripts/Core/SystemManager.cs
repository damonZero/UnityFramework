using System;
using System.Collections.Generic;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace Core.Architecture
{
    /// <summary>
    /// 系统管理器 — 统一管理所有 ISystem 的生命周期。
    /// 由 VContainer 注入系统列表，Boot 不再手动注册。
    /// </summary>
    public class SystemManager : IStartable, ITickable, ILateTickable, IFixedTickable, IDisposable
    {
        public bool Initialized { get; private set; }

        private readonly List<ISystem> _systems = new();
        private readonly List<ITickableSystem> _tickableSystems = new();
        private readonly Dictionary<Type, ISystem> _systemMap = new();
        private readonly IPublisher<AppStartedEvent> _appStartedPublisher;
        private readonly IPublisher<AppShuttingDownEvent> _appShuttingDownPublisher;

        public SystemManager(
            IEnumerable<ISystem> systems,
            IPublisher<AppStartedEvent> appStartedPublisher,
            IPublisher<AppShuttingDownEvent> appShuttingDownPublisher)
        {
            _appStartedPublisher = appStartedPublisher;
            _appShuttingDownPublisher = appShuttingDownPublisher;

            if (systems == null)
                return;

            foreach (var system in systems)
            {
                Register(system);
            }
        }

        public SystemManager Register<T>() where T : ISystem, new()
        {
            return Register(new T());
        }

        public SystemManager Register(ISystem system)
        {
            if (system == null)
                throw new ArgumentNullException(nameof(system));

            if (Initialized)
            {
                Debug.LogWarning($"[SystemManager] 已初始化，禁止再次注册: {system.GetType().Name}");
                return this;
            }

            var type = system.GetType();
            if (_systemMap.ContainsKey(type))
            {
                Debug.LogWarning($"[SystemManager] 系统已注册，跳过: {type.Name}");
                return this;
            }

            _systems.Add(system);
            _systemMap[type] = system;

            if (system is ITickableSystem tickable)
                _tickableSystems.Add(tickable);

            return this;
        }

        public T GetSystem<T>() where T : class, ISystem
        {
            return _systemMap.TryGetValue(typeof(T), out var sys) ? sys as T : null;
        }

        public void InitAll()
        {
            if (Initialized)
            {
                Debug.LogWarning("[SystemManager] 已初始化，跳过");
                return;
            }

            _systems.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            Debug.Log($"[SystemManager] 开始初始化 {_systems.Count} 个系统");

            for (var i = 0; i < _systems.Count; i++)
            {
                var sys = _systems[i];
                try
                {
                    sys.Init();
                    Debug.Log($"[SystemManager] Init [{i + 1}/{_systems.Count}] {sys.GetType().Name} (Priority={sys.Priority})");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SystemManager] Init 失败: {sys.GetType().Name}\n{e}");
                }
            }

            Initialized = true;
            _appStartedPublisher.Publish(new AppStartedEvent());
            Debug.Log("[SystemManager] 全部初始化完成");
        }

        public void ShutdownAll()
        {
            if (!Initialized && _systems.Count == 0)
                return;

            if (Initialized)
                _appShuttingDownPublisher.Publish(new AppShuttingDownEvent());

            Debug.Log("[SystemManager] 开始关闭系统");

            for (var i = _systems.Count - 1; i >= 0; i--)
            {
                var sys = _systems[i];
                try
                {
                    sys.Shutdown();
                    Debug.Log($"[SystemManager] Shutdown {sys.GetType().Name}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SystemManager] Shutdown 失败: {sys.GetType().Name}\n{e}");
                }
            }

            _systems.Clear();
            _tickableSystems.Clear();
            _systemMap.Clear();
            Initialized = false;

            Debug.Log("[SystemManager] 全部关闭完成");
        }

        public void Start()
        {
            InitAll();
        }

        public void Tick()
        {
            if (!Initialized) return;
            var dt = Time.deltaTime;
            foreach (var t in _tickableSystems)
            {
                t.Update(dt);
            }
        }

        public void LateTick()
        {
            if (!Initialized) return;
            var dt = Time.deltaTime;
            foreach (var t in _tickableSystems)
            {
                t.LateUpdate(dt);
            }
        }

        public void FixedTick()
        {
            if (!Initialized) return;
            var dt = Time.fixedDeltaTime;
            foreach (var t in _tickableSystems)
            {
                t.FixedUpdate(dt);
            }
        }

        public void Dispose()
        {
            ShutdownAll();
        }
    }
}
