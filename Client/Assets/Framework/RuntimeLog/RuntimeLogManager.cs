using System;
using Framework.Log;

namespace Framework.RuntimeLog
{
    public static class RuntimeLogManager
    {
        private static readonly object Gate = new();
        private static RuntimeLogSession _current;

        public static RuntimeLogSession Current
        {
            get
            {
                lock (Gate)
                    return _current;
            }
        }

        public static RuntimeLogSession Install(RuntimeLogSession session, bool installGameLogSink)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            lock (Gate)
            {
                if (_current != null && !ReferenceEquals(_current, session))
                {
                    // 修复：先清空 GameLog.Sink（它可能仍指向即将被 Dispose 的旧 session），
                    // 否则下方 CanInstallGameLogSink 会读到已 Dispose 的旧 sink 而误判为「不可安装」，
                    // 导致新 session 永不接线、日志静默丢失。与 DisposeCurrent 的清理逻辑保持一致。
                    if (ReferenceEquals(GameLog.Sink, _current))
                        GameLog.Sink = null;
                    _current.Dispose();
                }

                _current = session;

                if (installGameLogSink && CanInstallGameLogSink(session))
                    GameLog.Sink = session;
            }

            return session;
        }

        public static RuntimeLogSession InstallIfNone(Func<RuntimeLogSession> factory, bool installGameLogSink)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            lock (Gate)
            {
                if (_current != null)
                {
                    if (installGameLogSink && CanInstallGameLogSink(_current))
                        GameLog.Sink = _current;

                    return _current;
                }
            }

            return Install(factory(), installGameLogSink);
        }

        public static void Flush()
        {
            Current?.Flush();
        }

        public static void DisposeCurrent(RuntimeLogSession session = null)
        {
            RuntimeLogSession current;
            lock (Gate)
            {
                if (session != null && !ReferenceEquals(_current, session))
                    return;

                current = _current;
                _current = null;
            }

            if (current == null)
                return;

            if (ReferenceEquals(GameLog.Sink, current))
                GameLog.Sink = null;

            current.Dispose();
        }

        private static bool CanInstallGameLogSink(RuntimeLogSession session)
        {
            var sink = GameLog.Sink;
            return sink == null || ReferenceEquals(sink, session);
        }
    }
}
