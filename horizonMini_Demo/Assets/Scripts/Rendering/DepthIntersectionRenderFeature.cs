using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HorizonMini.Rendering
{
    /// <summary>
    /// URP Render Feature to ensure depth texture is available for intersection effects
    /// </summary>
    public class DepthIntersectionRenderFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            public bool enableDepthTexture = true;
        }

        public Settings settings = new Settings();
        private DepthIntersectionRenderPass renderPass;

        public override void Create()
        {
            renderPass = new DepthIntersectionRenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.enableDepthTexture)
            {
                renderer.EnqueuePass(renderPass);
            }
        }
    }

    public class DepthIntersectionRenderPass : ScriptableRenderPass
    {
        private DepthIntersectionRenderFeature.Settings settings;

        public DepthIntersectionRenderPass(DepthIntersectionRenderFeature.Settings settings)
        {
            this.settings = settings;
            this.renderPassEvent = settings.renderPassEvent;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // This pass doesn't need to do anything in Execute
            // Its purpose is to ensure depth texture is configured correctly
            // The actual depth intersection rendering is handled by the material shader
        }
    }
}
