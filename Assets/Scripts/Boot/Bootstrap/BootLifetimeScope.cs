using System;
using System.Collections.Generic;
using Framework.Log;
using UnityEngine;
using VContainer;

namespace Boot
{
    public sealed class BootLifetimeScope : AppLifetimeScope
    {
        private static readonly string[] DefaultBootstrapStageTypeNames =
        {
            "Core.Bootstrap.CoreBootstrapStage, Core",
            "General.GeneralBootstrapStage, General",
            "Project.Bootstrap.ProjectBootstrapStage, Project"
        };

        [SerializeField]
        private string[] bootstrapStageTypeNames =
        {
            "Core.Bootstrap.CoreBootstrapStage, Core",
            "General.GeneralBootstrapStage, General",
            "Project.Bootstrap.ProjectBootstrapStage, Project"
        };

        protected override void Configure(IContainerBuilder builder)
        {
            var stageTypeNames = bootstrapStageTypeNames == null || bootstrapStageTypeNames.Length == 0
                ? DefaultBootstrapStageTypeNames
                : bootstrapStageTypeNames;

            var stages = new List<IBootstrapStage>(stageTypeNames.Length);
            foreach (var typeName in stageTypeNames)
            {
                stages.Add(CreateStage(typeName));
            }

            var context = new BootstrapContext(builder);
            context.ConfigureStages(stages);
        }

        private static IBootstrapStage CreateStage(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                throw new InvalidOperationException("[Boot] Bootstrap stage type name is empty.");

            var type = Type.GetType(typeName, throwOnError: false);
            if (type == null)
                throw new InvalidOperationException($"[Boot] Bootstrap stage type not found: {typeName}");

            if (!typeof(IBootstrapStage).IsAssignableFrom(type))
                throw new InvalidOperationException($"[Boot] Bootstrap stage type must implement IBootstrapStage: {type.FullName}");

            if (type.IsAbstract || type.IsInterface)
                throw new InvalidOperationException($"[Boot] Bootstrap stage type must be concrete: {type.FullName}");

            try
            {
                return (IBootstrapStage)Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                GameLog.Error($"[Boot] Failed to create bootstrap stage: {typeName} - {e}");
                throw;
            }
        }
    }
}
