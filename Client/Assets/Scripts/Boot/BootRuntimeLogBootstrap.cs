using System;
using System.IO;
using System.Linq;
using Framework.Asset;
using Framework.Log;
using Framework.RuntimeLog;
using UnityEngine;

namespace Boot
{
    public static class BootRuntimeLogBootstrap
    {
        public static RuntimeLogSession EnsureInstalled(BootStartupSettings settings)
        {
            return RuntimeLogManager.InstallIfNone(
                () => CreateSession(settings),
                installGameLogSink: true);
        }

        private static RuntimeLogSession CreateSession(BootStartupSettings settings)
        {
            var startTime = DateTimeOffset.UtcNow;
            var info = new RuntimeLogSessionInfo
            {
                StartTimeUtc = startTime,
                ProjectName = Application.productName,
                UnityVersion = Application.unityVersion,
                Platform = Application.platform.ToString(),
                ApplicationVersion = Application.version,
                BuildGuid = Application.buildGUID,
                LogProfile = GameLog.Profile.Environment + ":" + GameLog.Profile.MinimumLevel
            };

            var config = Resources.Load<AssetConfig>("AssetConfig");
            if (config != null)
            {
                info.AssetPlayMode = config.Mode.ToString();
                info.AssetPackageName = config.PackageName;
            }

            if (settings != null)
            {
                info.HotUpdateAssemblies.AddRange(settings.HotUpdateAssemblies
                    .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.AssemblyName))
                    .Select(entry => entry.AssemblyName));
                info.AotMetadataAssemblies.AddRange(settings.AotMetadataAssemblies
                    .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.AssemblyName))
                    .Select(entry => entry.AssemblyName));
            }

            return new RuntimeLogSession(new RuntimeLogSessionOptions
            {
                DirectoryPath = GetRuntimeLogDirectory(),
                MaintainLatest = Application.isEditor || Debug.isDebugBuild,
                MinimumLevel = Application.isEditor || Debug.isDebugBuild ? GameLogLevel.Debug : GameLogLevel.Error,
                SessionInfo = info,
                UtcNow = () => DateTimeOffset.UtcNow,
                FrameProvider = () => Time.frameCount
            });
        }

        private static string GetRuntimeLogDirectory()
        {
#if UNITY_EDITOR
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.Combine(projectRoot, "Logs", "Runtime");
#else
            return Path.Combine(Application.persistentDataPath, "Logs", "Runtime");
#endif
        }
    }
}
