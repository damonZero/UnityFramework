using System;
using Microsoft.Extensions.Logging;

namespace Core.Logging
{
    /// <summary>
    /// 简易 ILogger&lt;T&gt; 实现 —— 将泛型 Log&lt;TState&gt; 转为非泛型 string 调用。
    /// 避开 AOT 侧 Microsoft.Extensions.Logging.Logger.Log&lt;TState&gt; 的泛型实例化需求。
    /// </summary>
    internal sealed class SimpleLogger<T> : ILogger<T>
    {
        private readonly ILogger _logger;
        private readonly string _category;

        public SimpleLogger(ILoggerFactory factory)
        {
            _category = typeof(T).FullName ?? typeof(T).Name;
            _logger = factory.CreateLogger(_category);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception exception, Func<TState, Exception, string> formatter)
        {
            // 关键：不把原始 TState 透传给 ILogger.Log<TState> —— 那会在 ZLoggerLogger 内部
            // 触发对任意 TState 的 AOT 泛型实例化。这里先把 formatter + state 展开成 string，
            // 再以 string 作为 TState 调用 Log<string>，避开原始泛型实例化。
            if (!_logger.IsEnabled(logLevel))
                return;

            string message = formatter != null
                ? formatter(state, exception)
                : state?.ToString() ?? string.Empty;

            _logger.Log(logLevel, eventId, message ?? string.Empty, exception, (m, _) => m);
        }

        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);
    }
}
