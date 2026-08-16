using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Framework.URPExtension
{
    /// <summary>GPU Instancing 的 CommandBuffer 通道。对应参考项目 Package/URPExtension/CommandBuffer/InstancingRenderPass.cs。</summary>
    public class InstancingRenderPass : ScriptableRenderPass
    {
        private const string ProfilerTag = nameof(InstancingRenderPass);

        private readonly List<CommandBuffer> _cmdList;

        public InstancingRenderPass(RenderPassEvent renderPassEvent)
        {
            this.renderPassEvent = renderPassEvent;
            _cmdList = new List<CommandBuffer>();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            foreach (var cmd in _cmdList)
            {
                context.ExecuteCommandBuffer(cmd);
            }
        }

        public void AddCommand(CommandBuffer cmd)
        {
            cmd.name = ProfilerTag;
            _cmdList.Add(cmd);
        }

        public void ClearCommand()
        {
            _cmdList.Clear();
        }

        public void RemoveCommand(CommandBuffer cmd)
        {
            _cmdList.Remove(cmd);
        }
    }
}
