namespace KJ.Core
{
    /// <summary>
    /// 框架模块基础接口。
    /// 所有管理器/服务必须实现此接口，由 ModuleManager 驱动生命周期。
    /// </summary>
    public interface IModule
    {
        /// <summary>
        /// 模块优先级，数值越小越先初始化，越大越先关闭。
        /// 建议范围: 100~999
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 模块初始化，由 ModuleManager.InitAll() 调用。
        /// </summary>
        void Init();

        /// <summary>
        /// 模块关闭，由 ModuleManager.ShutdownAll() 调用。
        /// </summary>
        void Shutdown();
    }
}
