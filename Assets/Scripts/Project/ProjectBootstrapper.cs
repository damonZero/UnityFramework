using Core;
using UnityEngine;
using VContainer;

namespace Project
{
    /// <summary>
    /// Project 层的容器接入点占位。
    /// 后续业务系统、Model、UseCase 都从这里接入容器。
    /// </summary>
    public class ProjectBootstrapper : MonoBehaviour, IAppBootstrapper
    {
        public void Configure(IContainerBuilder builder)
        {
            Debug.Log("[ProjectBootstrapper] Project 层容器注册已接入");
        }
    }
}
