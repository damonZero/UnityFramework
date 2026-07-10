using System.Collections.Generic;

namespace Boot.Editor.Build
{
    /// <summary>
    /// 构建知识库 —— 常见构建错误与修复建议的映射。
    /// 供 BuildAnalyzer 和 AI 诊断使用。
    /// </summary>
    public static class BuildKnowledgeBase
    {
        /// <summary>已知错误模式与修复建议</summary>
        public static readonly Dictionary<string, string> KnownErrors = new Dictionary<string, string>
        {
            ["HybridCLR runtime not installed"] =
                "Run KJ/HybridCLR/Install HybridCLR Runtime in Unity Editor.",
            ["Boot scene not in BuildSettings"] =
                "Run KJ/HybridCLR/Prepare Boot Scene to add the boot scene.",
            ["IL2CPP required"] =
                "Go to PlayerSettings → Other Settings → Scripting Backend → select IL2CPP.",
            ["AssetConfig not found"] =
                "Create an AssetConfig asset at Assets/Resources/AssetConfig.asset.",
            ["Gradle build failed"] =
                "Check that Android SDK/NDK/JDK paths are correct in Preferences → External Tools.",
            ["JDK directory is not set or invalid"] =
                "Install Android Build Support module via Unity Hub.",
            ["EditorFileSystem only supports the Unity Editor"] =
                "AssetConfig.Mode must be Offline (1) for Player builds. Verify P5 ApplyConfig ran.",
            ["System.UriFormatException"] =
                "YooAsset BuiltinFileSystem URI error — check BootLoader.CreateDefaultBuiltinFileSystemParameters() uses parameterless overload.",
            ["CompileDllCommand failed"] =
                "Check Console for compilation errors in your hot-update assemblies.",
            ["YooAsset build failed"] =
                "Check that YooAsset collector rules are valid and all resource paths exist.",
            ["adb not found"] =
                "Set ANDROID_SDK_ROOT or ANDROID_HOME environment variable, or install Android SDK platform-tools.",
            ["No online Android device"] =
                "Connect an Android device via USB or start an emulator (e.g. MuMu).",
            ["boot.log not produced"] =
                "The Player may have crashed before initializing AOT logging. Check adb logcat for Unity:V logs.",
            ["KeyStoreNotFoundException"] =
                "Configure Android Keystore in BuildProfile or PlayerSettings → Publishing Settings.",
        };

        /// <summary>根据错误消息查找已知修复建议</summary>
        public static string LookupFix(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage)) return null;

            foreach (var kv in KnownErrors)
            {
                if (errorMessage.Contains(kv.Key))
                    return kv.Value;
            }
            return null;
        }
    }
}
