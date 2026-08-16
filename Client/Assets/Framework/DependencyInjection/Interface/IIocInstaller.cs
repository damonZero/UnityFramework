using System;
using VContainer.Unity;

namespace Framework.DependencyInjection
{
    /// <summary>
    /// 自定义IOC安装器接口，继承自VContainer的IInstaller
    ///
    /// 使用方式：
    /// 1. 实现此接口创建自定义安装器
    /// 2. 在安装器中必须使用DIUtil中的注册方法
    ///
    /// <example>
    /// 示例：
    /// <code>
    /// <![CDATA[
    /// public class XXXInstaller : IIocInstaller
    /// {
    ///     public void Install(IContainerBuilder builder)
    ///     {
    ///         // 必须使用DIUtil中的注册方法
    ///         builder.RegisterAll<LogicModuleMgr>();
    ///         builder.RegisterStrict<ILogicModule>(validType);
    ///     }
    /// }
    /// ]]>
    /// </code>
    /// </example>
    /// </summary>
    public interface IIocInstaller : IInstaller, IDisposable
    {
    }
}
