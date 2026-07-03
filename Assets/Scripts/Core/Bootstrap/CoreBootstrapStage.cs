using Boot;
using MessagePipe;
using UnityEngine;

namespace Core.Architecture
{
    public sealed class CoreBootstrapStage : MonoBehaviour, IBootstrapStage
    {
        [SerializeField] private string nextBootstrapPrefabPath = string.Empty;

        public int Priority => 100;
        public string StageName => "Core";

        public void Configure(BootstrapContext context)
        {
            var options = context.Builder.RegisterCoreServices();
            context.Set(options);
            context.ConfigurePrefab(nextBootstrapPrefabPath);
        }
    }
}
