using System.Reflection;
using General;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace Project
{
    /// <summary>
    /// Project layer container hook. It only depends on General and external DI packages.
    /// </summary>
    public class ProjectBootstrapper : MonoBehaviour
    {
        public void Configure(IContainerBuilder builder, MessagePipeOptions options)
        {
            builder.RegisterBusinessLayer(options, Assembly.GetExecutingAssembly());
            Debug.Log("[ProjectBootstrapper] Project 层容器注册已接入");
        }
    }
}
