using Boot;
using MessagePipe;
using UnityEngine;

namespace General
{
    public sealed class GeneralBootstrapStage : MonoBehaviour, IBootstrapStage
    {
        [SerializeField] private string nextBootstrapPrefabPath = string.Empty;

        public int Priority => 200;
        public string StageName => "General";

        public void Configure(BootstrapContext context)
        {
            var options = context.GetRequired<MessagePipeOptions>();
            context.Builder.RegisterBusinessLayer(options, typeof(GeneralBootstrapStage).Assembly);
            context.ConfigurePrefab(nextBootstrapPrefabPath);
        }
    }
}
