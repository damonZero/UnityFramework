using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Boot.Editor.Build
{
    public sealed class PlayerBuildPrivatePathValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
        {
            var invalidPaths = new List<string>();
            CollectInvalidBuildScenes(invalidPaths);
            CollectInvalidAssetFolder("Assets/Resources", invalidPaths);
            CollectInvalidAssetFolder("Assets/StreamingAssets", invalidPaths);

            if (invalidPaths.Count == 0)
                return;

            invalidPaths.Sort(StringComparer.Ordinal);
            throw new BuildFailedException("S6_PreprocessBuild",
                "Paths with a segment starting with '_' must not enter the Player build:\n" +
                string.Join("\n", invalidPaths));
        }

        private static void CollectInvalidBuildScenes(List<string> invalidPaths)
        {
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene == null || !scene.enabled || string.IsNullOrEmpty(scene.path))
                    continue;

                if (HasPrivatePathSegment(scene.path))
                    invalidPaths.Add(scene.path);
            }
        }

        private static void CollectInvalidAssetFolder(string assetFolder, List<string> invalidPaths)
        {
            if (!AssetDatabase.IsValidFolder(assetFolder))
                return;

            var guids = AssetDatabase.FindAssets(string.Empty, new[] { assetFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path) && HasPrivatePathSegment(path))
                    invalidPaths.Add(path);
            }
        }

        private static bool HasPrivatePathSegment(string assetPath)
        {
            return assetPath
                .Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(segment => segment.StartsWith("_", StringComparison.Ordinal));
        }
    }
}
