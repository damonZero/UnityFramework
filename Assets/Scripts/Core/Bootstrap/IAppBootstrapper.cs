using VContainer;

namespace KJ.Core
{
    /// <summary>
    /// 启动桥接接口：由 Boot 层发现并执行，业务层负责把自己的注册逻辑接入容器。
    /// </summary>
    public interface IAppBootstrapper
    {
        void Configure(IContainerBuilder builder);
    }
}
