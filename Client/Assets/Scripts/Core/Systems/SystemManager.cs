using System;
using System.Collections.Generic;
using Core.Systems.Events;
using MessagePipe;
using Microsoft.Extensions.Logging;
using UnityEngine;
using VContainer.Unity;

namespace Core.Systems
{
    /// <summary>
    /// 系统管理器 — 统一管理所有 ISystem 的生命周期。
    /// 由 VContainer 注入系统列表，Boot 不再手动注册。
    /// </summary>
    public class SystemManager : IStartable, ITickable, ILateTickable, IFixedTickable, IDisposable, ICoreStartupStatus
    {
        public bool Initialized { get; private set; }
        public bool IsStarted => Initialized;
        public bool HasInitFailures => _failedSystemNames.Count > 0;
        public IReadOnlyList<string> FailedSystemNames => _failedSystemNames;

        private readonly List<ISystem> _systems = new();
        private readonly List<ISystem> _initializedSystems = new();
        private readonly HashSet<Type> _initializedTypes = new();
        private readonly List<ITickableSystem> _tickableSystems = new();
        private readonly HashSet<ITickableSystem> _tickFailed = new();
        private readonly List<string> _failedSystemNames = new();
        private readonly Dictionary<Type, ISystem> _systemMap = new();
        private readonly IPublisher<AppStartedEvent> _appStartedPublisher;
        private readonly IPublisher<AppShuttingDownEvent> _appShuttingDownPublisher;
        private readonly ILogger<SystemManager> _logger;

        public SystemManager(
            IEnumerable<ISystem> systems,
            IPublisher<AppStartedEvent> appStartedPublisher,
            IPublisher<AppShuttingDownEvent> appShuttingDownPublisher,
            ILogger<SystemManager> logger)
        {
            _appStartedPublisher = appStartedPublisher;
            _appShuttingDownPublisher = appShuttingDownPublisher;
            _logger = logger;

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
                SystemManagerLog.AlreadyInitialized(_logger, system.GetType().Name);
                return this;
            }

            var type = system.GetType();
            if (_systemMap.ContainsKey(type))
            {
                SystemManagerLog.SystemAlreadyRegistered(_logger, type.Name);
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
                SystemManagerLog.AlreadyInitializedSkip(_logger);
                return;
            }

            _systems.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            _tickableSystems.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            _failedSystemNames.Clear();

            SystemManagerLog.InitStart(_logger, _systems.Count);

            for (var i = 0; i < _systems.Count; i++)
            {
                var sys = _systems[i];

                // 部分失败后重入：已成功初始化的系统跳过，避免重复 Init。
                if (_initializedTypes.Contains(sys.GetType()))
                    continue;

                try
                {
                    sys.Init();
                    _initializedTypes.Add(sys.GetType());
                    _initializedSystems.Add(sys);
                    SystemManagerLog.InitProgress(_logger, i + 1, _systems.Count, sys.GetType().Name, sys.Priority);
                }
                catch (Exception e)
                {
                    _failedSystemNames.Add(sys.GetType().Name);
                    SystemManagerLog.InitFailed(_logger, sys.GetType().Name, e);

                    // Init 中途抛异常的系统可能已部分初始化，尽力回收，避免资源泄漏。
                    try { sys.Shutdown(); }
                    catch { }
                }
            }

            Initialized = !HasInitFailures;
            if (Initialized)
            {
                _appStartedPublisher.Publish(new AppStartedEvent());
                SystemManagerLog.InitComplete(_logger);
            }
            else
            {
                SystemManagerLog.InitCompleteWithFailures(_logger, string.Join(", ", _failedSystemNames));
            }
        }

        public void ShutdownAll()
        {
            if (!Initialized && _systems.Count == 0)
                return;

            if (Initialized)
                _appShuttingDownPublisher.Publish(new AppShuttingDownEvent());

            SystemManagerLog.ShutdownStart(_logger);

            for (var i = _initializedSystems.Count - 1; i >= 0; i--)
            {
                var sys = _initializedSystems[i];
                try
                {
                    sys.Shutdown();
                    SystemManagerLog.ShutdownProgress(_logger, sys.GetType().Name);
                }
                catch (Exception e)
                {
                    SystemManagerLog.ShutdownFailed(_logger, sys.GetType().Name, e);
                }
            }

            _systems.Clear();
            _initializedSystems.Clear();
            _initializedTypes.Clear();
            _tickableSystems.Clear();
            _tickFailed.Clear();
            _systemMap.Clear();
            _failedSystemNames.Clear();
            Initialized = false;

            SystemManagerLog.ShutdownComplete(_logger);
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
                try { t.Update(dt); }
                catch (Exception e) { ReportTickFailure(t, e); }
            }
        }

        public void LateTick()
        {
            if (!Initialized) return;
            var dt = Time.deltaTime;
            foreach (var t in _tickableSystems)
            {
                try { t.LateUpdate(dt); }
                catch (Exception e) { ReportTickFailure(t, e); }
            }
        }

        public void FixedTick()
        {
            if (!Initialized) return;
            var dt = Time.fixedDeltaTime;
            foreach (var t in _tickableSystems)
            {
                try { t.FixedUpdate(dt); }
                catch (Exception e) { ReportTickFailure(t, e); }
            }
        }

        private void ReportTickFailure(ITickableSystem t, Exception e)
        {
            // 单系统 Tick 抛异常不得中断其它系统/整帧。首次失败记录一次，
            // 避免同一系统每帧抛异常刷屏 JSONL。
            if (_tickFailed.Add(t))
                SystemManagerLog.TickFailed(_logger, t.GetType().Name, e);
        }

        public void Dispose()
        {
            ShutdownAll();
        }
    }
}
