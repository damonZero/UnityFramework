using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Framework.URPExtension
{
    /// <summary>CommandBuffer 通道：把一组 CommandBuffer 在指定时机执行。对应参考项目 Package/URPExtension/CommandBuffer/CommandBufferPass.cs。</summary>
    public class CommandBufferPass : ScriptableRenderPass
    {
        private readonly List<CommandBuffer> _cmdList;

        public CommandBufferPass(RenderPassEvent renderPassEvent)
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
            _cmdList.Add(cmd);
        }

        public void RemoveCommand(CommandBuffer cmd)
        {
            _cmdList.Remove(cmd);
        }
    }
}
