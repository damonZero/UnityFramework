using System;

namespace Framework.RuntimeLog
{
    public static class RuntimeLogPhaseResolver
    {
        public const string DefaultPhase = "Runtime";

        public static string Resolve(string module, string category, string message)
        {
            module ??= string.Empty;
            category ??= string.Empty;
            message ??= string.Empty;

            if (StartsWith(module, "Boot") || StartsWith(category, "Boot") || message.Contains("[Boot]"))
                return "Boot";

            if (Contains(module, "HybridCLR") || Contains(category, "HybridCLR") || Contains(message, "HybridCLR"))
                return "HybridCLR";

            if (Contains(category, "SystemManager") || Contains(message, "[SystemManager]"))
                return "Core.Init";

            if (Contains(category, "ModelLifecycle") || Contains(message, "[ModelLifecycle]"))
                return "ModelLifecycle";

            if (StartsWith(module, "Project") || StartsWith(category, "Project"))
                return "Project";

            if (StartsWith(module, "General") || StartsWith(category, "General"))
                return "General";

            // 精确匹配资源模块（module == "Framework.Asset" 或 "Core.Asset*"），避免裸 "Asset" 误匹配
            // Project.AssetXxx / General.AssetXxx / AssetShared 等；须在通用 Core 判定之前。
            if (module == "Framework.Asset" || StartsWith(module, "Core.Asset")
                || Contains(category, "AssetSystem") || Contains(message, "[AssetSystem]"))
                return "Core.Asset";

            if (StartsWith(module, "Core") || StartsWith(category, "Core"))
                return "Core";

            return DefaultPhase;
        }

        private static bool StartsWith(string value, string prefix) =>
            value.StartsWith(prefix, StringComparison.Ordinal);

        private static bool Contains(string value, string pattern) =>
            value.IndexOf(pattern, StringComparison.Ordinal) >= 0;
    }
}
