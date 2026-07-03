using System;
using UnityEngine;
using VContainer;

namespace Boot
{
    public sealed class BootLifetimeScope : AppLifetimeScope
    {
        [SerializeField] private string nextBootstrapPrefabPath = string.Empty;

        protected override void Configure(IContainerBuilder builder)
        {
            if (string.IsNullOrWhiteSpace(nextBootstrapPrefabPath))
                throw new InvalidOperationException("[Boot] Next bootstrap prefab path is empty.");

            var context = new BootstrapContext(builder, transform);
            context.ConfigurePrefab(nextBootstrapPrefabPath);
        }
    }
}
