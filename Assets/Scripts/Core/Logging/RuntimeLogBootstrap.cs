using System;
using System.Collections.Generic;
using System.IO;
using Framework.Asset;
using Framework.Log;
using Framework.RuntimeLog;
using UnityEngine;

namespace Core.Logging
{
    public static class RuntimeLogBootstrap
    {
        public static RuntimeLogSession EnsureInstalled(IAssetRuntime assetRuntime = null)
        {
            return RuntimeLogManager.InstallIfNone(
                () => CreateSession(assetRuntime),
                installGameLogSink: true);
        }

        public static RuntimeLogSession CreateSession(IAssetRuntime assetRuntime = null)
        {
            var startTime = DateTimeOffset.UtcNow;
            var info = CreateSessionInfo(startTime, assetRuntime);
            return new RuntimeLogSession(new RuntimeLogSessionOptions
            {
                DirectoryPath = GetRuntimeLogDirectory(),
                MaintainLatest = Debug.isDebugBuild || Application.isEditor,
                MinimumLevel = GetDefaultFileMinimumLevel(),
                SessionInfo = info,
                UtcNow = () => DateTimeOffset.UtcNow,
                FrameProvider = () => Time.frameCount
            });
        }

        public static RuntimeLogSessionInfo CreateSessionInfo(DateTimeOffset startTimeUtc, IAssetRuntime assetRuntime = null)
        {
            var info = new RuntimeLogSessionInfo
            {
                StartTimeUtc = startTimeUtc,
                ProjectName = Application.productName,
                UnityVersion = Application.unityVersion,
                Platform = Application.platform.ToString(),
                ApplicationVersion = Application.version,
                BuildGuid = Application.buildGUID,
                GitCommit = GetGitCommit(),
                LogProfile = GameLog.Profile.Environment + ":" + GameLog.Profile.MinimumLevel
            };

            var config = Resources.Load<AssetConfig>("AssetConfig");
            if (config != null)
            {
                info.AssetPlayMode = config.Mode.ToString();
                info.AssetPackageName = config.PackageName;
            }

            if (assetRuntime != null && !string.IsNullOrWhiteSpace(assetRuntime.LastError))
                info.Context["assetRuntimeLastError"] = assetRuntime.LastError;

            return info;
        }

        public static void UpdateBootAssemblies(RuntimeLogSession session, IEnumerable<string> hotUpdateAssemblies, IEnumerable<string> aotMetadataAssemblies)
        {
            if (session == null)
                return;

            session.UpdateSessionInfo(info =>
            {
                Replace(info.HotUpdateAssemblies, hotUpdateAssemblies);
                Replace(info.AotMetadataAssemblies, aotMetadataAssemblies);
            });
        }

        public static string GetRuntimeLogDirectory()
        {
#if UNITY_EDITOR
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.Combine(projectRoot, "Logs", "Runtime");
#else
            return Path.Combine(Application.persistentDataPath, "Logs", "Runtime");
#endif
        }

        private static GameLogLevel GetDefaultFileMinimumLevel()
        {
            if (Application.isEditor || Debug.isDebugBuild)
                return GameLogLevel.Debug;

            return GameLogLevel.Error;
        }

        private static string GetGitCommit()
        {
            var value = Environment.GetEnvironmentVariable("GIT_COMMIT");
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            value = Environment.GetEnvironmentVariable("BUILD_VCS_NUMBER");
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        }

        private static void Replace(List<string> target, IEnumerable<string> source)
        {
            target.Clear();
            if (source == null)
                return;

            foreach (var item in source)
            {
                if (!string.IsNullOrWhiteSpace(item))
                    target.Add(item);
            }
        }
    }
}
