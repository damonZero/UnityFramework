using System;
using Framework.BuildPipeline.Diagnostics;
using Framework.BuildPipeline.Environment;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Formal/Audit 环境泄露检查 —— 确保发布包不含调试符号、GM、Debug UI、Development Build。
    /// </summary>
    public static class FormalLeakageVerifier
    {
        /// <summary>
        /// 对当前构建环境执行 Formal 泄露检查。
        /// 不适用的环境（Dev/QA/Profiling）直接跳过。
        /// </summary>
        public static void Verify(BuildContext context, BuildTarget buildTarget)
        {
            var profile = context.Profile;
            if (profile == null) return;

            var env = profile.Environment;
            if (env != BuildEnvironment.Formal && env != BuildEnvironment.Audit)
                return;

            BuildLogger.Info("[FormalLeakage] Verifying production release constraints...");

            bool hasIssue = false;

            // 1. Development Build 必须为 false
            if (EditorUserBuildSettings.development)
            {
                hasIssue = true;
                context.AddIssue(BuildIssue.Error(
                    BuildErrorCodes.FormalDebugBuildLeak, "P7.Verify",
                    "Development Build is enabled in production build"));
            }

            // 2. Script Debugging 必须为 false
            if (EditorUserBuildSettings.allowDebugging)
            {
                hasIssue = true;
                context.AddIssue(BuildIssue.Error(
                    BuildErrorCodes.FormalScriptDebuggingLeak, "P7.Verify",
                    "Script Debugging is enabled in production build"));
            }

            // 3. IL2CPP
            var targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var backend = PlayerSettings.GetScriptingBackend(targetGroup);
            if (backend != ScriptingImplementation.IL2CPP)
            {
                hasIssue = true;
                context.AddIssue(BuildIssue.Error(
                    BuildErrorCodes.PreIL2CPPRequired, "P7.Verify",
                    $"ScriptingBackend is {backend}, must be IL2CPP"));
            }

            // 4. Scripting Define Symbols: 禁止 KJ_GM_ENABLED / KJ_DEBUG_UI
            var namedTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
            if (defines.Contains("KJ_GM_ENABLED"))
            {
                hasIssue = true;
                context.AddIssue(BuildIssue.Error(
                    BuildErrorCodes.FormalGmEnabled, "P7.Verify",
                    "KJ_GM_ENABLED define found in production build"));
            }
            if (defines.Contains("KJ_DEBUG_UI"))
            {
                hasIssue = true;
                context.AddIssue(BuildIssue.Error(
                    BuildErrorCodes.FormalDebugUiEnabled, "P7.Verify",
                    "KJ_DEBUG_UI define found in production build"));
            }

            // 5. 禁止 Development 相关的 define
            if (defines.Contains("KJ_DEV"))
            {
                hasIssue = true;
                context.AddIssue(BuildIssue.Error(
                    BuildErrorCodes.FormalDefineLeak, "P7.Verify",
                    "KJ_DEV define found in production build"));
            }

            if (hasIssue)
            {
                throw new BuildFailedException("P7.Verify",
                    "Formal leakage verification failed — see issues for details");
            }

            BuildLogger.Info("[FormalLeakage] ✓ All production release constraints passed");
        }
    }
}
