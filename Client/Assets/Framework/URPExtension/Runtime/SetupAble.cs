using UnityEngine.Rendering.Universal;

namespace Framework.URPExtension
{
    /// <summary>Pass 可自配置标记：<see cref="CustomRenderFeature"/> 在 Enqueue 前调用 Setup。对应参考项目 Package/URPExtension/SetupAble.cs。</summary>
    public interface ISetupAble
    {
        bool Setup(ref RenderingData renderingData);
    }
}
