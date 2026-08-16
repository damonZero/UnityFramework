// ********************************************************************
//   作者：WangXing-汪兴
//   创建时间：2018/06/04
// ********************************************************************

using System.Runtime.CompilerServices;
using Framework.Log;

namespace Framework.View
{
    /// <summary>
    /// View 包内部日志封装，转发到 KJ 的 GameLog 门面。
    /// 保留 Log.Debug/Info/Error 签名，使 ViewBase 等调用点零改动。
    /// </summary>
    internal static class Log
    {
        internal const string Module = "Framework.View";

        internal static void Debug(string message, UnityEngine.Object context = null,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "")
        {
            GameLog.Debug(message, module: Module);
        }

        internal static void Info(string message)
        {
            GameLog.Info(message, module: Module);
        }

        internal static void Error(string message, UnityEngine.Object context = null,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "")
        {
            GameLog.Error(message, module: Module);
        }
    }
}
