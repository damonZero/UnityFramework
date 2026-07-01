# Unity 游戏框架架构研究

**项目:** KJ Unity Framework
**研究日期:** 2026-06-26 / 实际实现更新: 2026-06-30
**置信度:** 中高 (基于业界成熟模式和开源框架实践)

> **⚠️ 本文档 §§2-9 为设计阶段草稿，使用 `IModule` / `ModuleManager` / `EventManager` 等早期概念。**
> **实际实现以代码为准：**
> - `IModule` → `ISystem` + `[CoreSystem]` + VContainer DI
> - `ModuleManager` → `SystemManager`
> - `EventManager` → MessagePipe + `[GameEvent]`
> - `ResourceManager` → `AssetSystem` (YooAsset 3.0 封装)
> - `ObjectPoolManager` → `Framework/Pool/ObjectPool<T>` + `Framework/Pool/Unity/GameObjectPool` + `Core/Pool/PoolService` (DI 桥接)
> - §6 (资源管理) 已重写对齐实际实现。本章节 §1 的整体架构方向仍有效。

---

## 1. 分层架构设计

### 1.1 四层架构模型

```
┌─────────────────────────────────────────────────────────────┐
│                      Project Layer                          │
│  (游戏业务逻辑、具体玩法、UI界面实现)                          │
├─────────────────────────────────────────────────────────────┤
│                      General Layer                          │
│  (通用游戏系统：红点、引导、音频、配置表)                       │
├─────────────────────────────────────────────────────────────┤
│                       Core Layer                            │
│  (框架核心：事件、网络、UI框架、资源管理、对象池)                │
├─────────────────────────────────────────────────────────────┤
│                       Boot Layer                            │
│  (启动入口、热更新、程序生命周期)                              │
└─────────────────────────────────────────────────────────────┘
```

### 1.2 各层职责边界

| 层级 | 职责 | 依赖方向 | 典型内容 |
|------|------|----------|----------|
| **Boot** | 程序入口、热更新引导、启动流程 | 无依赖，被所有层依赖 | `Entry.cs`、`AppLifetimeScope`、`BootstrapContext`、HybridCLR入口 |
| **Core** | 框架基础设施、引擎级Manager | 依赖Boot + Framework + Packages | `SystemManager`、`AssetSystem`、`NetManager`、`UIManager` |
| **General** | 通用游戏系统 | 依赖Core | `ConfigManager`(Luban)、`AudioManager`、`RedDotManager`、`GuideManager`、`ModelLifecycle` |
| **Project** | 具体游戏业务 | 依赖所有层 | 具体UI窗口、游戏逻辑、业务流程 |

### 1.3 Assembly Definition 分离

```
Assets/
├── Scripts/
│   ├── Boot/           → Boot.asmdef
│   ├── Core/           → Core.asmdef
│   ├── General/        → General.asmdef
│   └── Project/        → Project.asmdef
```

**依赖关系:**
- `Boot` → `VContainer`
- `Core` → `Boot`, `Framework.Pool`, `Framework.Cache`, `VContainer`, `MessagePipe`, `MessagePipe.VContainer`, `UniTask`, `YooAsset`
- `General` → `Boot`, `Core`, `VContainer`, `MessagePipe`, `MessagePipe.VContainer`
- `Project` → `Boot`, `General`, `VContainer`, `MessagePipe`, `MessagePipe.VContainer`

**优势:** 编译隔离、依赖清晰、防止循环引用、支持增量编译

---

## 2. 模块生命周期管理

### 2.1 IModule 接口设计

```csharp
public interface IModule
{
    /// <summary>
    /// 模块优先级，数值越小越先初始化
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 模块初始化，按Priority顺序调用
    /// </summary>
    void Init();

    /// <summary>
    /// 每帧更新
    /// </summary>
    void Update(float deltaTime);

    /// <summary>
    /// 每帧LateUpdate
    /// </summary>
    void LateUpdate(float deltaTime);

    /// <summary>
    /// 固定更新
    /// </summary>
    void FixedUpdate(float fixedDeltaTime);

    /// <summary>
    /// 模块关闭，按Priority逆序调用
    /// </summary>
    void Shutdown();
}
```

### 2.2 ModuleManager 实现

```csharp
public class ModuleManager : MonoBehaviour
{
    private readonly List<IModule> _modules = new();
    private readonly Dictionary<Type, IModule> _moduleMap = new();

    public T GetModule<T>() where T : class, IModule
    {
        return _moduleMap.TryGetValue(typeof(T), out var module) ? module as T : null;
    }

    public void Register(IModule module)
    {
        _modules.Add(module);
        _moduleMap[module.GetType()] = module;
    }

    void Awake()
    {
        // 按优先级排序后初始化
        _modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        foreach (var module in _modules)
        {
            module.Init();
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        foreach (var module in _modules)
        {
            module.Update(dt);
        }
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        foreach (var module in _modules)
        {
            module.LateUpdate(dt);
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        foreach (var module in _modules)
        {
            module.FixedUpdate(dt);
        }
    }

    void OnDestroy()
    {
        // 逆序关闭，确保依赖关系正确
        for (int i = _modules.Count - 1; i >= 0; i--)
        {
            _modules[i].Shutdown();
        }
        _modules.Clear();
        _moduleMap.Clear();
    }
}
```

### 2.3 模块优先级规划

| 优先级 | 模块 | 说明 |
|--------|------|------|
| -999 | AssetInitSystem | YooAsset 底层初始化 |
| 50 | PoolService | Framework 委托注入，池注册到 DI |
| 100 | AssetSystem | 资源加载是其他模块的基础 |
| 200 | UIManager | UI框架需要资源和事件支持 |
| 300 | NetManager | 网络模块需要事件系统支持 |
| 400 | ConfigManager | 配置表需要资源加载支持 |
| 500 | AudioManager | 音频需要资源加载支持 |
| 600 | RedDotManager | 红点需要事件系统支持 |
| 700 | GuideManager | 引导需要UI框架支持 |

---

## 3. 事件系统设计

### 3.1 EventManager 核心接口

```csharp
public class EventManager : IModule
{
    // 事件ID类型
    private delegate void EventHandler(IEventArgs args);

    // 带优先级的事件处理器
    private class HandlerInfo
    {
        public EventHandler Handler;
        public int Priority;
        public object Owner; // 用于清理
    }

    // 事件字典: EventId -> List<HandlerInfo>
    private readonly Dictionary<int, List<HandlerInfo>> _eventHandlers = new();

    /// <summary>
    /// 订阅事件
    /// </summary>
    public void Subscribe(int eventId, Action<IEventArgs> handler, int priority = 0, object owner = null);

    /// <summary>
    /// 取消订阅
    /// </summary>
    public void Unsubscribe(int eventId, Action<IEventArgs> handler);

    /// <summary>
    /// 取消某个Owner的所有订阅
    /// </summary>
    public void UnsubscribeAll(object owner);

    /// <summary>
    /// 同步触发事件
    /// </summary>
    public void Fire(int eventId, IEventArgs args = null);

    /// <summary>
    /// 延迟到下一帧触发
    /// </summary>
    public void FireDelay(int eventId, IEventArgs args = null);

    /// <summary>
    /// 触发事件直到某个处理器返回true（中断传播）
    /// </summary>
    public bool FireUntil(int eventId, IEventArgs args = null);
}
```

### 3.2 事件系统特性

| 特性 | 实现方式 | 用途 |
|------|----------|------|
| **同步触发** | `Fire()` 直接调用 | 即时响应，如UI更新 |
| **异步触发** | `FireDelay()` 延迟到下一帧 | 避免同帧递归、性能优化 |
| **优先级** | `SortedDictionary` 或排序List | UI层级响应顺序、输入处理 |
| **Owner管理** | 订阅时绑定Owner对象 | 模块卸载时自动清理、防止内存泄漏 |
| **中断传播** | `FireUntil()` 返回bool | UI事件冒泡、输入事件处理 |

### 3.3 事件ID设计

```csharp
// 事件ID使用枚举或常量类
public static class EventID
{
    // 网络事件
    public const int OnConnect = 10001;
    public const int OnDisconnect = 10002;
    public const int OnMessage = 10003;

    // UI事件
    public const int OnWindowOpen = 20001;
    public const int OnWindowClose = 20002;

    // 游戏事件
    public const int OnLoginSuccess = 30001;
    public const int OnLevelUp = 30002;
}
```

---

## 4. 网络层设计

### 4.1 架构分层

```
┌─────────────────────────────────────────┐
│           Message Handler Layer          │
│  (具体消息处理器：LoginHandler等)          │
├─────────────────────────────────────────┤
│           Message Router                 │
│  (消息ID -> Handler映射分发)              │
├─────────────────────────────────────────┤
│           Session Layer                  │
│  (会话管理：连接状态、心跳、重连)           │
├─────────────────────────────────────────┤
│           Transport Layer                │
│  (底层传输：TCP/UDP/WebSocket)            │
└─────────────────────────────────────────┘
```

### 4.2 NetManager 设计

```csharp
public class NetManager : IModule
{
    private readonly Dictionary<int, Session> _sessions = new();
    private Session _mainSession; // 主连接

    /// <summary>
    /// 创建新会话
    /// </summary>
    public Session CreateSession(string host, int port, NetworkType type = NetworkType.TCP);

    /// <summary>
    /// 关闭会话
    /// </summary>
    public void CloseSession(int sessionId);

    /// <summary>
    /// 获取会话
    /// </summary>
    public Session GetSession(int sessionId);

    /// <summary>
    /// 注册消息处理器
    /// </summary>
    public void RegisterHandler(int msgId, Action<Session, IMessage> handler);

    /// <summary>
    /// 发送消息
    /// </summary>
    public void Send(int msgId, IMessage message, Session session = null);
}
```

### 4.3 Session 设计

```csharp
public class Session
{
    public int SessionId { get; }
    public SessionState State { get; }
    public DateTime LastHeartbeat { get; }

    // 连接管理
    public void Connect(string host, int port);
    public void Disconnect();
    public void Reconnect();

    // 消息收发
    public void Send(int msgId, IMessage message);
    public void OnReceive(byte[] data);

    // 心跳
    public void StartHeartbeat(float interval = 5f);
    public void StopHeartbeat();
}

public enum SessionState
{
    Disconnected,
    Connecting,
    Connected,
    Authenticating,
    Authenticated,
    Reconnecting
}
```

### 4.4 Protobuf 消息设计

```csharp
// 消息基类
public interface IMessage
{
    int MsgId { get; }
    byte[] Serialize();
    void Deserialize(byte[] data);
}

// Protobuf消息包装
public class ProtoMessage<T> : IMessage where T : Google.Protobuf.IMessage<T>, new()
{
    public int MsgId { get; }
    private T _data;

    public byte[] Serialize()
    {
        using var stream = new MemoryStream();
        _data.WriteTo(stream);
        return stream.ToArray();
    }

    public void Deserialize(byte[] data)
    {
        _data = new T();
        _data.MergeFrom(data);
    }
}

// 消息路由
public class MessageRouter
{
    // MsgId -> Handler列表
    private readonly Dictionary<int, List<Action<Session, IMessage>>> _handlers = new();

    public void Route(Session session, int msgId, byte[] data)
    {
        if (_handlers.TryGetValue(msgId, out var handlers))
        {
            var message = MessageFactory.Create(msgId);
            message.Deserialize(data);

            foreach (var handler in handlers)
            {
                handler(session, message);
            }
        }
    }
}
```

---

## 5. UI 框架设计

### 5.1 UIManager 核心接口

```csharp
public class UIManager : IModule
{
    // UI层级
    private readonly Dictionary<UILayer, Transform> _layers = new();

    // 已打开的窗口
    private readonly Dictionary<string, UIWindow> _openWindows = new();

    // 窗口栈（用于返回逻辑）
    private readonly Stack<UIWindow> _windowStack = new();

    /// <summary>
    /// 打开窗口
    /// </summary>
    public T OpenWindow<T>(object userData = null) where T : UIWindow;

    /// <summary>
    /// 关闭窗口
    /// </summary>
    public void CloseWindow(UIWindow window);

    /// <summary>
    /// 关闭所有窗口
    /// </summary>
    public void CloseAll();

    /// <summary>
    /// 获取已打开的窗口
    /// </summary>
    public T GetWindow<T>() where T : UIWindow;

    /// <summary>
    /// 窗口是否打开
    /// </summary>
    public bool IsWindowOpen<T>() where T : UIWindow;
}

public enum UILayer
{
    Background,  // 背景层
    Normal,      // 普通窗口层
    Popup,       // 弹窗层
    Top,         // 顶层（Toast等）
    Loading,     // 加载层
    System       // 系统层（SDK等）
}
```

### 5.2 UIWindow 基类设计

```csharp
public abstract class UIWindow : MonoBehaviour
{
    public string WindowName { get; }
    public UILayer Layer { get; }
    public bool IsOpen { get; private set; }

    // 窗口生命周期
    protected virtual void OnInit() { }
    protected virtual void OnOpen(object userData) { }
    protected virtual void OnClose() { }
    protected virtual void OnPause() { }  // 被其他窗口覆盖
    protected virtual void OnResume() { } // 从覆盖恢复
    protected virtual void OnUpdate(float deltaTime) { }

    // 内部调用
    internal void Init() => OnInit();
    internal void Open(object userData)
    {
        gameObject.SetActive(true);
        IsOpen = true;
        OnOpen(userData);
    }
    internal void Close()
    {
        IsOpen = false;
        OnClose();
        gameObject.SetActive(false);
    }
}
```

### 5.3 UI层级结构

```
UICanvas (ScreenSpace - Overlay)
├── BackgroundLayer (sortingOrder: 0)
│   └── LoginBackground, MainBackground
├── NormalLayer (sortingOrder: 100)
│   └── MainWindow, BagWindow, ShopWindow
├── PopupLayer (sortingOrder: 200)
│   └── ConfirmDialog, SettingsPopup
├── TopLayer (sortingOrder: 300)
│   └── Toast, Tips
├── LoadingLayer (sortingOrder: 400)
│   └── LoadingScreen, ProgressBar
└── SystemLayer (sortingOrder: 500)
    └── SDKLogin, Privacy Agreement
```

### 5.4 窗口打开模式

```csharp
public enum WindowMode
{
    /// <summary>普通打开，不关闭其他窗口</summary>
    Normal,

    /// <summary>单例模式，打开前关闭同层级其他窗口</summary>
    Single,

    /// <summary>隐藏其他窗口（不关闭）</summary>
    HideOthers,

    /// <summary>覆盖在栈顶</summary>
    Overlay
}
```

---

## 6. 资源管理设计

底层引擎: **YooAsset 3.0** (8300+ GitHub Stars, 5年持续维护)，通过 UPM git URL 安装。

封装层: `Core/Asset/AssetSystem`，YooAsset 类型完全限定在 Core assembly，上层代码通过 `IAssetSystem` 接口使用。

### 6.1 AssetSystem 核心接口

```csharp
// 对上层暴露的接口 (Core.Asset.IAssetSystem)
public interface IAssetSystem
{
    // 异步加载并返回带生命周期的句柄（调用者负责 Dispose）
    UniTask<AssetHandle<T>> LoadAssetHandleAsync<T>(string path) where T : Object;

    // 异步加载，系统内部缓存 Handle，Release(path) 释放
    UniTask<T> LoadAssetAsync<T>(string path) where T : Object;

    // 加载并实例化 (Handle + GameObject 联合生命周期)
    UniTask<AssetInstanceHandle> InstantiateAsync(string path, Transform parent = null);

    // 场景加载（返回可激活/卸载的句柄，串行化同 path 的 load/unload）
    UniTask<AssetSceneHandle> LoadSceneAsync(string path, LoadSceneMode mode, Action<float> onProgress);

    // YooAsset 下载器（供更新管线使用）
    ResourceDownloaderOperation CreateDownloader(string tag = null);
    ResourceDownloaderOperation CreateDownloader(string[] tags);

    // 按类型释放缓存
    void Release<T>(string path) where T : Object;
    void Release(string path);

    // 底层 UnloadUnusedAssets
    void UnloadUnused();
}
```

### 6.2 句柄设计

```csharp
public sealed class AssetHandle<T> : IDisposable where T : Object
{
    public T Asset { get; }
    public bool IsDone { get; }
    public float Progress { get; }
    public bool IsValid { get; }
    public string Error { get; }
    public void Dispose();         // Release YooAsset handle + remove from owned set
}

public sealed class AssetInstanceHandle : IDisposable 
{
    public GameObject Instance { get; }           // 实例化的 GameObject
    public AssetHandle<GameObject> SourceHandle { get; }  // 源 prefab handle
    public void Dispose();                        // Destroy(GameObject) + Dispose(Handle)
}

public sealed class AssetSceneHandle : IDisposable
{
    public string SceneName { get; }
    public Scene Scene { get; }
    public bool ActivateScene();
    public UniTask UnloadAsync();                 // 异步卸载，串行化
    public void Dispose();                        // Dispose 时 fire-and-forget 卸载
}
```

### 6.3 缓存架构

```
┌──────────────────────────────────────────────────────┐
│                    AssetSystem                        │
├──────────────────────┬───────────────────────────────┤
│  _assetHandles        │  _ownedAssetHandles            │
│  Dictionary<Key,Handle>│  HashSet<AssetHandle>         │
│                       │                               │
│  LoadAssetAsync → 缓存 │  LoadAssetHandleAsync → 调用者管理  │
│  Release(path) → 释放   │  AssetHandle.Dispose → 自动移除   │
├──────────────────────┼───────────────────────────────┤
│  _sceneHandles        │  _ownedSceneHandles            │
│  Dictionary<string,Handle>│ HashSet<SceneHandle>          │
│                       │                               │
│  _sceneUnloadTasks    │  ← 串行化同 path 的 unload/load│
│  Dictionary<string,UniTask>│                           │
└──────────────────────┴───────────────────────────────┘
```

**缓存键**: `AssetCacheKey` (path + Type) — 同时按路径和类型区分，避免 `LoadAssetAsync<GameObject>("a.prefab")` 与 `LoadAssetAsync<Object>("a.prefab")` 混淆。

**并发保护**: `SemaphoreSlim(1,1)` — 因为 UniTask await 在主线程回归，实际无多线程竞争，但作为防御性设计。

### 6.4 PlayMode 配置

通过 `AssetConfig` ScriptableObject 配置，加载时 `Resources.Load<AssetConfig>("AssetConfig")`。

| 模式 | YooAsset 对应 | 场景 |
|------|-------------|------|
| EditorSimulate | EditorFileSystem | 开发期，虚拟文件系统，不打 AB |
| Offline | BuiltinFileSystem | 单机发布，AB 打入 StreamingAssets |
| Host | Builtin + Sandbox | 线上热更，内置 AB + CDN 下载 |

Host 模式下 CDN URL 通过 `CdnRemoteService : IRemoteService` 实现，下载超时和并发数由 AssetConfig 下发到 `FileSystemParameters`。

---

## 7. 对象池系统设计

### 7.1 ObjectPoolManager 核心接口

```csharp
public class ObjectPoolManager : IModule
{
    // 池字典: Prefab路径 -> ObjectPool
    private readonly Dictionary<string, IObjectPool> _pools = new();

    /// <summary>
    /// 获取对象
    /// </summary>
    public T Get<T>(string prefabPath) where T : Component;

    /// <summary>
    /// 回收对象
    /// </summary>
    public void Release<T>(T obj) where T : Component;

    /// <summary>
    /// 预热池
    /// </summary>
    public void Warmup(string prefabPath, int count);

    /// <summary>
    /// 清空池
    /// </summary>
    public void Clear(string prefabPath);

    /// <summary>
    /// 清空所有池
    /// </summary>
    public void ClearAll();
}
```

### 7.2 对象池实现

```csharp
public class ObjectPool<T> : IObjectPool where T : Component
{
    private readonly Queue<T> _available = new();
    private readonly HashSet<T> _inUse = new();
    private readonly T _prefab;
    private readonly Transform _parent;

    public int CountAll { get; private set; }
    public int CountActive => _inUse.Count;
    public int CountInactive => _available.Count;

    public T Get()
    {
        T obj;
        if (_available.Count > 0)
        {
            obj = _available.Dequeue();
        }
        else
        {
            obj = Object.Instantiate(_prefab, _parent);
            CountAll++;
        }
        _inUse.Add(obj);
        obj.gameObject.SetActive(true);
        return obj;
    }

    public void Release(T obj)
    {
        if (_inUse.Remove(obj))
        {
            obj.gameObject.SetActive(false);
            _available.Enqueue(obj);
        }
    }
}
```

---

## 8. 通用游戏系统设计

### 8.1 红点系统 (RedDotManager)

```csharp
public class RedDotManager : IModule
{
    // 红点节点树
    private readonly Dictionary<string, RedDotNode> _nodes = new();

    /// <summary>
    /// 设置红点数量
    /// </summary>
    public void SetCount(string path, int count);

    /// <summary>
    /// 获取红点数量（包含子节点）
    /// </summary>
    public int GetCount(string path);

    /// <summary>
    /// 监听红点变化
    /// </summary>
    public void Listen(string path, Action<int> callback, object owner = null);

    /// <summary>
    /// 取消监听
    /// </summary>
    public void Unlisten(string path, Action<int> callback);
}

public class RedDotNode
{
    public string Path { get; }
    public int SelfCount { get; set; }      // 自身红点数
    public int TotalCount { get; set; }     // 总红点数（含子节点）
    public RedDotNode Parent { get; set; }
    public List<RedDotNode> Children { get; } = new();

    public event Action<int> OnCountChanged;
}
```

**红点路径示例:**
```
Root
├── Main_Bag          (背包)
│   ├── Main_Bag_Equip  (装备)
│   └── Main_Bag_Item   (道具)
├── Main_Shop         (商店)
│   ├── Main_Shop_Daily (每日商店)
│   └── Main_Shop_Gift  (礼包)
└── Main_Mail         (邮件)
```

### 8.2 引导系统 (GuideManager)

```csharp
public class GuideManager : IModule
{
    // 引导配置
    private readonly Dictionary<int, GuideConfig> _configs = new();

    // 引导状态
    private readonly Dictionary<int, GuideState> _states = new();

    // 当前执行中的引导
    private GuideRunner _currentGuide;

    /// <summary>
    /// 开始引导
    /// </summary>
    public void StartGuide(int guideId);

    /// <summary>
    /// 完成引导步骤
    /// </summary>
    public void CompleteStep(int guideId, int stepId);

    /// <summary>
    /// 引导是否完成
    /// </summary>
    public bool IsGuideComplete(int guideId);

    /// <summary>
    /// 跳过引导
    /// </summary>
    public void SkipGuide(int guideId);
}

public class GuideStep
{
    public int StepId { get; }
    public GuideAction Action { get; }      // 动作类型
    public string TargetUI { get; }         // 目标UI路径
    public string HighlightPath { get; }    // 高亮区域
    public string DialogText { get; }       // 对话文本
    public Vector2 FingerPos { get; }       // 手指位置
}
```

### 8.3 音频管理 (AudioManager)

```csharp
public class AudioManager : IModule
{
    // 音频源池
    private readonly ObjectPool<AudioSource> _audioSourcePool = new();

    // BGM源
    private AudioSource _bgmSource;

    // 音量控制
    public float BGMVolume { get; set; }
    public float SFXVolume { get; set; }
    public float VoiceVolume { get; set; }

    /// <summary>
    /// 播放BGM
    /// </summary>
    public void PlayBGM(string clipPath, bool loop = true, float fadeTime = 0.5f);

    /// <summary>
    /// 停止BGM
    /// </summary>
    public void StopBGM(float fadeTime = 0.5f);

    /// <summary>
    /// 播放音效
    /// </summary>
    public AudioSource PlaySFX(string clipPath, float volume = 1f);

    /// <summary>
    /// 播放3D音效
    /// </summary>
    public AudioSource PlaySFX3D(string clipPath, Vector3 position, float volume = 1f);

    /// <summary>
    /// 播放语音
    /// </summary>
    public void PlayVoice(string clipPath);
}
```

---

## 9. 组件依赖关系图

```
┌─────────────────────────────────────────────────────────────────┐
│                        Boot Layer                                │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │ Entry.cs     │  │ HybridCLR   │  │ Bootstrap   │              │
│  │ (入口)       │  │ (热更新)     │  │ (启动链)     │              │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘              │
└─────────┼────────────────┼────────────────┼─────────────────────┘
          │                │                │
          ▼                ▼                ▼
┌─────────────────────────────────────────────────────────────────┐
│                        Core Layer                                │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │ AssetSystem  │  │ SystemMgr  │  │ NetManager  │              │
│  │ (资源管理)    │  │ (系统管理)   │  │ (网络管理)   │              │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘              │
│         │                │                │                      │
│         ▼                ▼                ▼                      │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │ UIManager    │  │ PoolService │  │ TimerMgr    │              │
│  │ (UI框架)     │  │ (池桥接)     │  │ (定时器)     │              │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
└─────────────────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────────┐
│                       General Layer                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │ ConfigManager│  │ AudioManager│  │ RedDotManager│              │
│  │ (配置表)     │  │ (音频)       │  │ (红点)       │              │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
│  ┌─────────────┐  ┌─────────────┐                               │
│  │ GuideManager │  │ Localization │                               │
│  │ (引导)       │  │ (本地化)      │                               │
│  └─────────────┘  └─────────────┘                               │
└─────────────────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────────┐
│                       Project Layer                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │ LoginWindow  │  │ MainWindow  │  │ GameLogic   │              │
│  │ (登录界面)    │  │ (主界面)     │  │ (游戏逻辑)   │              │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
└─────────────────────────────────────────────────────────────────┘
```

---

## 10. 数据流设计

### 10.1 网络消息流

```
Server
  ↓ [TCP/Protobuf]
NetManager.Receive()
  ↓
MessageRouter.Route()
  ↓
MessageHandler.Handle()
  ↓
EventManager.Fire(OnXxxMessage)
  ↓
UIManager.UpdateWindow()
  ↓
Player sees update
```

### 10.2 用户操作流

```
Player clicks button
  ↓
UIWindow.OnButtonClick()
  ↓
EventManager.Fire(OnXxxAction)
  ↓
BusinessLogic.Handle()
  ↓
NetManager.Send(ReqXxx)
  ↓
Server processes
```

### 10.3 资源加载流

```
UI needs resource
  ↓
AssetSystem.LoadAssetAsync()
  ↓
YooAsset 3.0
  ↓
Cache check → Load if needed
  ↓
Callback with asset
  ↓
UI uses asset
```

---

## 11. 体系依赖关系

详见 [ROADMAP.md](../ROADMAP.md) 依赖关系速查。

```
AssetSystem ← ISystem
Timer ← ISystem
ObjectPool ← AssetSystem
UIManager ← ISystem + AssetSystem
ConfigManager (Luban) ← AssetSystem
AudioManager ← AssetSystem + ObjectPool
NetManager ← ISystem + MessagePipe
RedDot ← MessagePipe
Guide ← MessagePipe + UIManager + ConfigManager
Localization ← ConfigManager + AssetSystem
HybridCLR ← AssetSystem + Boot
```

---

## 12. 设计模式总结

| 模式 | 应用场景 | 示例 |
|------|----------|------|
| **DI/IoC** | 模块注册与生命周期 | VContainer + [CoreSystem] + ISystem |
| **发布/订阅** | 模块通信 | MessagePipe + [GameEvent] |
| **对象池** | 频繁创建/销毁对象 | Framework/Pool/ObjectPool<T> + GameObjectPool |
| **状态机** | 游戏流程、会话状态 | Bootstrap Stage 链, SessionState |
| **命令模式** | 引导系统、输入处理 | GuideStep |
| **策略模式** | PlayMode 切换、缓存淘汰 | AssetConfig.Mode → YooAsset PlayMode; ICacheEvictionPolicy → LRU |
| **工厂模式** | Handle 创建 | AssetSystem → AssetHandle / AssetSceneHandle |

---

## 13. 性能优化建议

### 13.1 内存优化
- 使用对象池减少GC
- YooAsset 3.0 管理 AssetBundle 生命周期
- 及时释放不用的 AssetHandle
- 避免在Update中分配内存

### 13.2 网络优化
- 消息合并发送
- 心跳包优化（可变间隔）
- 断线重连机制
- 消息压缩

### 13.3 UI优化
- UI窗口复用
- 图集合并
- 按需加载UI资源
- 避免频繁SetActive

---

## 置信度评估

| 方面 | 置信度 | 说明 |
|------|--------|------|
| 分层架构 | 高 | 业界成熟模式，参考GameFramework等开源框架 |
| 模块生命周期 | 高 | 标准Unity模式，大量开源实现可参考 |
| 事件系统 | 高 | 观察者模式成熟，优先级/Owner管理是常见需求 |
| 网络层 | 中高 | Protobuf成熟，但具体会话管理需要根据项目调整 |
| UI框架 | 高 | UGUI封装是常见需求，层级管理有成熟方案 |
| 资源管理 | 高 | YooAsset 3.0 已集成，参见 [ROADMAP.md](../ROADMAP.md) |
| 对象池 | 高 | Unity 2021+有官方实现，模式成熟 |
| 红点系统 | 中 | 需要根据具体业务调整树结构 |
| 引导系统 | 中 | 需要根据具体UI结构调整 |

---

## 参考资源

- **GameFramework (EllanJiang)** - [GitHub](https://github.com/EllanJiang/GameFramework) - 业界成熟的Unity框架实现
- **UnityGameFramework** - [GitHub](https://github.com/EllanJiang/UnityGameFramework) - GameFramework的Unity封装
- **QFramework** - Unity MVC框架，包含完整的UI/事件/资源管理
- **Unity官方文档** - Addressables、ObjectPool等官方实现
- **HybridCLR** - [GitHub](https://github.com/focus-creative-games/hybridclr) - C#热更新方案

---

*研究完成日期: 2026-06-26*
