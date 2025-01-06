using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class XGBAAGeometryPassFeature : ScriptableRendererFeature
{
	DrawObjectsPass drawObjectsPass;
	public Material overrideMaterial;
	public LayerMask layerMask = 0; // Add layer mask

	public override void Create()
	{
		// Create the render pass that draws the objects, and pass in the override material
		drawObjectsPass = new DrawObjectsPass(overrideMaterial, layerMask); // Pass layer mask

		// Insert render passes after URP's post-processing render pass
		drawObjectsPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		// Add the render pass to the URP rendering loop
		renderer.EnqueuePass(drawObjectsPass);
	}

	class DrawObjectsPass : ScriptableRenderPass
	{
		private Material materialToUse;
		private LayerMask layerMask; // Add layer mask

		public DrawObjectsPass(Material overrideMaterial, LayerMask layerMask) // Add layer mask parameter
		{
			// Set the pass's local copy of the override material and layer mask
			materialToUse = overrideMaterial;
			this.layerMask = layerMask;
		}

		private class PassData
		{
			// Create a field to store the list of objects to draw
			public RendererListHandle rendererListHandle;
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
		{
			using (var builder = renderGraph.AddRasterRenderPass<PassData>("Redraw objects", out var passData))
			{
				// Get the data needed to create the list of objects to draw
				UniversalRenderingData renderingData = frameContext.Get<UniversalRenderingData>();
				UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();
				UniversalLightData lightData = frameContext.Get<UniversalLightData>();
				SortingCriteria sortFlags = cameraData.defaultOpaqueSortFlags;
				RenderQueueRange renderQueueRange = RenderQueueRange.opaque;
				FilteringSettings filterSettings = new FilteringSettings(renderQueueRange, layerMask); // Use layer mask

				// Redraw only objects that have their LightMode tag set to UniversalForward 
				ShaderTagId shadersToOverride = new ShaderTagId("UniversalForward");

				// Create drawing settings
				DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(shadersToOverride, renderingData, cameraData, lightData, sortFlags);

				// Add the override material to the drawing settings
				drawSettings.overrideMaterial = materialToUse;

				// Create the list of objects to draw
				var rendererListParameters = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);

				// Convert the list to a list handle that the render graph system can use
				passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParameters);

				// Set the render target as the color and depth textures of the active camera texture
				UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();
				builder.UseRendererList(passData.rendererListHandle);
				builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
				builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);

				builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
			}
		}

		static void ExecutePass(PassData data, RasterGraphContext context)
		{
			// Clear the render target to black
			context.cmd.ClearRenderTarget(true, true, Color.black);

			// Draw the objects in the list
			context.cmd.DrawRendererList(data.rendererListHandle);
		}
	}
}
