using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Framework.URPExtension
{
    /// <summary>
    /// URP RenderFeature 注册门面：经 <see cref="Show"/>/<see cref="Hide"/> 注册/卸载 ScriptableRenderPass，
    /// 并按相机 cullingMask 决定是否执行。对应参考项目 Package/URPExtension/CustomRenderFeature.cs。
    /// </summary>
    public class CustomRenderFeature : ScriptableRendererFeature
    {
        private static readonly List<ScriptableRenderPass> _renderList = new();
        private static readonly Dictionary<ScriptableRenderPass, int> _cameraDict = new();

        public override void Create() { }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            foreach (var pass in _renderList)
            {
                if (!CheckShowPass(pass, renderingData.cameraData)) continue;

                if (pass is ISetupAble dd)
                {
                    if (dd.Setup(ref renderingData))
                        renderer.EnqueuePass(pass);
                    continue;
                }

                renderer.EnqueuePass(pass);
            }
        }

        private static bool CheckShowPass(ScriptableRenderPass pass, CameraData cameraData)
        {
            if (cameraData.isSceneViewCamera) return true;
            if (pass is InstancingRenderPass && cameraData.camera.cameraType == CameraType.Preview) return false;

            if (!_cameraDict.TryGetValue(pass, out var layer)) return false;
            var cullingMask = cameraData.camera.cullingMask;
            return cullingMask == -1 || ((cullingMask >> layer) & 1) > 0;
        }

        /// <summary>注册一个 Pass，layer 决定其在哪些相机（按 cullingMask）执行。</summary>
        public static void Show(ScriptableRenderPass pass, int layer)
        {
            if (pass == null) return;
            _renderList.Add(pass);
            _cameraDict[pass] = layer;
        }

        /// <summary>卸载一个 Pass。</summary>
        public static void Hide(ScriptableRenderPass pass)
        {
            if (pass == null) return;
            _renderList.Remove(pass);
            _cameraDict.Remove(pass);
        }

        /// <summary>清空所有注册的 Pass（软重启/关闭时调用，避免 readonly 集合泄漏）。</summary>
        public static void ShutDown()
        {
            _renderList.Clear();
            _cameraDict.Clear();
        }
    }
}
