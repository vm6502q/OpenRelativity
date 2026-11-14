using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace OpenRelativity
{
    /// <summary>
    /// The Universal Render Pipeline equivalent of "Graphics.Blit(source, destination, material)" in "OnRenderImage()":
    /// this draws the camera's color image through a material, and the result becomes the camera's color image.
    /// (The material's shader should use the vertex shader and "_BlitTexture" from the render pipeline core "Blit.hlsl".)
    ///
    /// A component enqueues this, for its camera, with <see cref="Enqueue"/>, from "RenderPipelineManager.beginCameraRendering".
    /// That keeps image effects as components on cameras, as they were for the built-in render pipeline,
    /// without any renderer feature to set up on the render pipeline asset.
    /// </summary>
    public class FullScreenMaterialPass : ScriptableRenderPass
    {
        private readonly string blitPassName;
        private Material material;
        private RTHandle captureTarget;

        public FullScreenMaterialPass(string name)
        {
            blitPassName = name;
            // "OnRenderImage()" ran after everything else in the camera's image was drawn.
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            // We read the camera's color image as a texture, so the camera can't draw directly to the screen.
            requiresIntermediateTexture = true;
        }

        /// <summary>
        /// Queue this pass for the next (imminent) render of a camera.
        /// </summary>
        /// <param name="camera">The camera, as passed to "RenderPipelineManager.beginCameraRendering"</param>
        /// <param name="blitMaterial">The material to draw the camera's image through</param>
        /// <param name="capture">If not null, the result is also copied into this render texture</param>
        /// <returns>False, if the camera isn't rendered by the Universal Render Pipeline</returns>
        public bool Enqueue(Camera camera, Material blitMaterial, RTHandle capture = null)
        {
            if (blitMaterial == null || !(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
            {
                return false;
            }

            ScriptableRenderer renderer = camera.GetUniversalAdditionalCameraData().scriptableRenderer;
            if (renderer == null)
            {
                return false;
            }

            material = blitMaterial;
            captureTarget = capture;
            renderer.EnqueuePass(this);

            return true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (material == null)
            {
                return;
            }

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer)
            {
                // (We can't sample the back buffer. "requiresIntermediateTexture" should prevent this.)
                return;
            }

            TextureHandle source = resourceData.activeColorTexture;

            TextureDesc destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = "_CameraColor" + blitPassName;
            destinationDesc.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

            RenderGraphUtils.BlitMaterialParameters blitParameters = new RenderGraphUtils.BlitMaterialParameters(source, destination, material, 0);
            renderGraph.AddBlitPass(blitParameters, passName: blitPassName);

            // The rest of the frame continues from our output, without a copy back.
            resourceData.cameraColor = destination;

            if (captureTarget != null && captureTarget.rt != null)
            {
                TextureHandle capture = renderGraph.ImportTexture(captureTarget);
                renderGraph.AddBlitPass(destination, capture, Vector2.one, Vector2.zero, passName: blitPassName + " Capture");
            }
        }
    }
}
