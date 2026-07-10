using System;
using System.Collections.Generic;

namespace Framework.BuildPipeline.Reports
{
    /// <summary>
    /// 产物条目 —— 记录构建产物路径、大小、哈希。
    /// </summary>
    [Serializable]
    public sealed class BuildArtifactManifest
    {
        public List<BuildArtifactEntry> Entries = new List<BuildArtifactEntry>();

        /// <summary>添加产物条目</summary>
        public void Add(string path, string description, long sizeBytes, string sha256 = "")
        {
            Entries.Add(new BuildArtifactEntry
            {
                Path = path,
                Description = description,
                SizeBytes = sizeBytes,
                Sha256 = sha256,
            });
        }

        /// <summary>添加文件产物（路径 + 描述，计算大小和哈希）</summary>
        public void AddFromFile(string path, string description = "")
        {
            Entries.Add(BuildArtifactEntry.FromFile(path, description));
        }
    }

    [Serializable]
    public sealed class BuildArtifactEntry
    {
        public string Path;
        public string Description;
        public long SizeBytes;
        public string Sha256;

        public static BuildArtifactEntry FromFile(string filePath, string description = "")
        {
            var entry = new BuildArtifactEntry
            {
                Path = filePath,
                Description = description,
            };

            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    var info = new System.IO.FileInfo(filePath);
                    entry.SizeBytes = info.Length;
                }
            }
            catch
            {
                // 计算失败时 SizeBytes 保持 0
            }

            return entry;
        }
    }
}
