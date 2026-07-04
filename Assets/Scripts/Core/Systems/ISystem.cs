namespace Core.Systems
{
    /// <summary>
    /// 系统生命周期接口 — 所有 Core 层系统必须实现
    /// </summary>
    public interface ISystem
    {
        /// <summary>
        /// 初始化优先级，越小越先 Init
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 系统初始化
        /// </summary>
        void Init();

        /// <summary>
        /// 系统关闭（逆序执行）
        /// </summary>
        void Shutdown();
    }

    /// <summary>
    /// 可驱动的系统接口 — 需要每帧更新的系统实现此接口
    /// </summary>
    public interface ITickableSystem : ISystem
    {
        void Update(float deltaTime);
        void LateUpdate(float deltaTime);
        void FixedUpdate(float fixedDeltaTime);
    }
}
