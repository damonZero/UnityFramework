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
            // 关键：不走 ILogger.Log(logLevel, eventId, state, exception, formatter) ——
            // 那个重载在 ZLoggerLogger 内部会回调 Logger.Log<TState>，触发 AOT 泛型实例化。
            // 直接展开 formatter + state 为字符串，调用非泛型 ILogger.Log(logLevel, string)。
            if (!_logger.IsEnabled(logLevel))
                return;

            string message = formatter != null
                ? formatter(state, exception)
                : state?.ToString() ?? string.Empty;

            _logger.Log(logLevel, eventId, message, exception, (m, ex) => m.ToString());
        }

        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);
    }
}
