using System.IO;
using HybridCLR.Editor.Commands;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// Stage 1 — HybridCLR 生成。
    /// 生成 link.xml / AOTGenericReferences / 编译骨架。
    /// </summary>
    public static class StageGenerateAll
    {
        public static void Execute(BuildConfig config)
        {
            Debug.Log("[S1] GenerateAll: Starting...");

            PrebuildCommand.GenerateAll();

            // 不变量：link.xml 存在且非空
            // HybridCLR 的输出路径相对于 Application.dataPath（即 Assets/），而非项目根目录
            string linkXml = Path.Combine(Application.dataPath, "HybridCLRGenerate/link.xml");
            if (!File.Exists(linkXml) || new FileInfo(linkXml).Length == 0)
            {
                throw new BuildFailedException("S1_GenerateAll",
                    $"link.xml not found or empty at: {linkXml}");
            }
            Debug.Log("[S1] link.xml verified: " + linkXml);

            Debug.Log("[S1] GenerateAll: DONE");
        }
    }
}
