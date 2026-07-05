using Framework.Asset;

namespace Project.Bootstrap
{
    public static class ProjectStartup
    {
        private static ProjectLifetimeScope _rootScope;

        public static void Start(IAssetRuntime bootAssetRuntime = null)
        {
            if (_rootScope != null)
                return;

            var root = new UnityEngine.GameObject("ProjectLifetimeScope");
            UnityEngine.Object.DontDestroyOnLoad(root);
            ProjectLifetimeScope.PendingBootAssetRuntime = bootAssetRuntime;
            _rootScope = root.AddComponent<ProjectLifetimeScope>();
        }
    }
}
