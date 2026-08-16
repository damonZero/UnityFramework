using System;
using System.Runtime.CompilerServices;
using Framework.Log;
using Object = UnityEngine.Object;

namespace Framework.View.Navigation
{
    /// <summary>
    /// 导航内部日志封装，转发到 KJ 的 GameLog 门面。
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// 导航调试开关分类
        /// </summary>
        public const string DEBUG_CATEGORY = "Navigation";

        internal const string Module = "Framework.View.Navigation";

        public static void Debug(string log, Object context = null, [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "")
        {
            GameLog.Debug(log, module: Module);
        }

        /// <summary>
        /// 提示性报错打印，引起注意并修改
        /// </summary>
        public static void Error(string hint, Object obj = null)
        {
            GameLog.Error(hint, module: Module);
        }

        /// <summary>
        /// 记录异常
        /// </summary>
        public static void Exception(Exception e, Object obj = null)
        {
            GameLog.Exception(e, "Navigation exception", module: Module);
        }
    }
}
