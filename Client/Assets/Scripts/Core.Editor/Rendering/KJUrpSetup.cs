using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Core.Editor.Rendering
{
    /// <summary>
    /// URP 渲染管线一键设置：创建默认 UniversalRenderPipelineAsset + UniversalRendererData，
    /// 并赋给 GraphicsSettings.defaultRenderPipeline。
    /// 对应参考项目 GameRes/Config/Boot/UrpSettings/ 下的管线资产（KJ 首版用默认单 Renderer，
    /// 37 的 UI Split / Decal / 自定义 RenderFeature 等进阶配置按需后补）。
    /// </summary>
    public static class KJUrpSetup
    {
        private const string Dir = "Assets/GameRes/Config/Boot/UrpSettings";
        private const string RendererPath = Dir + "/MainCameraRenderer.asset";
        private const string PipelinePath = Dir + "/URPPipelineAsset.asset";

        [MenuItem("KJ/Rendering/设置 URP 管线")]
        public static void Setup()
        {
            if (!Directory.Exists(Dir))
            {
                Directory.CreateDirectory(Dir);
            }

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererPath);
            }

            var pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipelineAsset == null)
            {
                pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipelineAsset, PipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipelineAsset;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[KJUrpSetup] URP 管线已设置：{PipelinePath} → GraphicsSettings");
        }
    }
}
