using VContainer.Unity;

namespace Framework.DependencyInjection
{
    /// <summary>
    /// 需要外部注入的依赖
    /// </summary>
    public static class Dependencies
    {
        public static LifetimeScope Scope { get; set; }
    }
}
