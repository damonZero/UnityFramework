using Boot;
using MessagePipe;
using UnityEngine;

namespace Project
{
    public sealed class ProjectBootstrapStage : MonoBehaviour, IBootstrapStage
    {
        [SerializeField] private string nextBootstrapPrefabPath = string.Empty;

        public int Priority => 300;
        public string StageName => "Project";

        public void Configure(BootstrapContext context)
        {
            var options = context.GetRequired<MessagePipeOptions>();
            var bootstrapper = GetComponent<ProjectBootstrapper>() ?? gameObject.AddComponent<ProjectBootstrapper>();
            bootstrapper.Configure(context.Builder, options);
            context.ConfigurePrefab(nextBootstrapPrefabPath);
        }
    }
}
