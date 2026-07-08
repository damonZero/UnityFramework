using System.IO;
using System.Linq;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Stage 2 — 编译热更 DLL + AOT metadata DLL。
    /// 输出到 HybridCLRData/HotUpdateDlls/ 和 HybridCLRData/AssembliesPostIl2CppStrip/
    /// </summary>
    public static class StageCompile
    {
        public static void Execute(BuildConfig config)
        {
            Debug.Log("[S2] Compile: Starting...");

            // 获取热更程序集数量（要校验 10 个）
            int expectedCount = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved.Count;
            Debug.Log($"[S2] Expected hot update assemblies: {expectedCount}");

            // 获取输出目录
            string hotUpdateDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(config.Platform);
            string aotMetadataDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(config.Platform);

            // 幂等清理：先清空旧产物
            if (Directory.Exists(hotUpdateDir))
            {
                foreach (string f in Directory.GetFiles(hotUpdateDir, "*.dll"))
                    File.Delete(f);
                Debug.Log($"[S2] Cleaned: {hotUpdateDir}");
            }
            if (Directory.Exists(aotMetadataDir))
            {
                foreach (string f in Directory.GetFiles(aotMetadataDir, "*.dll"))
                    File.Delete(f);
                Debug.Log($"[S2] Cleaned: {aotMetadataDir}");
            }

            // 编译热更 DLL
            CompileDllCommand.CompileDll(config.Platform, config.Development);

            // 验证热更 DLL 产物
            var hotUpdateDlls = Directory.GetFiles(hotUpdateDir, "*.dll");
            Debug.Log($"[S2] Hot update DLLs produced: {hotUpdateDlls.Length}");
            if (hotUpdateDlls.Length == 0)
            {
                throw new BuildFailedException("S2_Compile",
                    $"No DLLs produced in {hotUpdateDir}. CompileDll may have failed silently.");
            }

            // 生成 AOT metadata DLL
            StripAOTDllCommand.GenerateStripedAOTDlls(config.Platform);

            var aotDlls = Directory.GetFiles(aotMetadataDir, "*.dll");
            Debug.Log($"[S2] AOT metadata DLLs produced: {aotDlls.Length}");
            if (aotDlls.Length == 0)
            {
                throw new BuildFailedException("S2_Compile",
                    $"No AOT metadata DLLs produced in {aotMetadataDir}.");
            }

            Debug.Log("[S2] Compile: DONE");
        }
    }
}
