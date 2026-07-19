using System;
using System.Collections.Generic;
using System.IO;
using Boot.Editor.Build.Telemetry;
using Framework.BuildPipeline.Plan;
using HybridCLR.Editor.Commands;
using UnityEditor;
using UnityEngine;

namespace Boot.Editor.Build
{
    /// <summary>
    /// P2 Generate — HybridCLR GenerateAll + 代码生成。
    /// </summary>
    public class P2_GenerateStage : BuildStageBase
    {
        public override string Id => "P2.Generate";
        public override string DisplayName => "Generate HybridCLR (MethodBridge/link.xml)";
        public override int Order => 2;
        public override string Category => "HybridCLR";
        public override IReadOnlyList<string> DependsOn { get; } = new[] { "P1.Preflight" };
        public override BuildStagePolicy Policy =>
            BuildStagePolicy.Required | BuildStagePolicy.ProducesArtifacts;

        public override BuildStageInputs GetInputs(BuildContext context)
            => new BuildStageInputs()
                .WithSourcePaths(
                    "Assets/Scripts/Boot/",
                    "Assets/Scripts/Core/",
                    "Assets/Scripts/General/",
                    "Assets/Scripts/Project/",
                    "Assets/Framework/");

        public override BuildStageOutputs GetExpectedOutputs(BuildContext context)
            => new BuildStageOutputs()
                .WithRequiredFile("Assets/HybridCLRGenerate/link.xml");

        public override void Execute(BuildContext context)
        {
            Debug.Log("[P2] Generate: PrebuildCommand.GenerateAll()...");

            // 调用现有 HybridCLR 生成逻辑
            BuildTelemetry.Measure(
                "P2.GenerateAll",
                "HybridCLR",
                PrebuildCommand.GenerateAll);

            // 校验 link.xml
            string linkXmlPath = "Assets/HybridCLRGenerate/link.xml";
            if (!File.Exists(linkXmlPath) || new FileInfo(linkXmlPath).Length == 0)
            {
                throw new BuildFailedException(Id,
                    "link.xml was not generated or is empty");
            }

            Debug.Log("[P2] Generate: DONE");
        }

        public override void Verify(BuildContext context)
        {
            base.Verify(context);
            Debug.Log("[P2] ✓ link.xml present and non-empty");
        }
    }
}
