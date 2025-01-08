using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using static Unity.Burst.Intrinsics.X86.Avx;

public class XGBAARendererFeature : ScriptableRendererFeature
{
	[Header("GBuffer Pass")]

	public Material gbufferMaterial;
	public LayerMask layerMask = 0;
	XGBAAGBufferPass gbufferPass;

	[Header("Resolve Pass")]

	public Material resolveMaterial;
	XGBAAResolvePass resolvePass;

	[Header("Debug")]

	public Material debugMaterial;
	[Range(0, 1)]
	public float debugGBufferAlpha = 0.0f;

	// Shared gbuffer texture
	private TextureHandle gbuffer = TextureHandle.nullHandle;

	public override void Create()
	{
		// Create the render pass that draws the objects, and pass in the override material
		gbufferPass = new XGBAAGBufferPass(this);
		gbufferPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

		// Create the post-processing pass
		resolvePass = new XGBAAResolvePass(this);
		resolvePass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		renderer.EnqueuePass(gbufferPass);
		renderer.EnqueuePass(resolvePass);
	}

	class XGBAAGBufferPass : ScriptableRenderPass
	{
		private XGBAARendererFeature rendererFeature;

		private class PassData
		{
			public RendererListHandle rendererListHandle;
		}

		public XGBAAGBufferPass(XGBAARendererFeature rendererFeature)
		{
			this.rendererFeature = rendererFeature;
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
		{
			using (var builder = renderGraph.AddRasterRenderPass<PassData>("XGBAA GBuffer Pass", out var passData))
			{
				// get all sorts of data from the frame context

				var renderingData = frameContext.Get<UniversalRenderingData>();
				var resourceData = frameContext.Get<UniversalResourceData>();
				var cameraData = frameContext.Get<UniversalCameraData>();
				var lightData = frameContext.Get<UniversalLightData>();

				// create renderer list

				var sortFlags = cameraData.defaultOpaqueSortFlags;
				var shadersToOverride = new ShaderTagId("UniversalForward");
				var drawSettings = RenderingUtils.CreateDrawingSettings(shadersToOverride, renderingData, cameraData, lightData, sortFlags);
				drawSettings.overrideMaterial = rendererFeature.gbufferMaterial;

				var filterSettings = new FilteringSettings(RenderQueueRange.opaque, rendererFeature.layerMask);

				var rendererListParameters = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);
				passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParameters);

				// create render target

				var textureProperties = cameraData.cameraTargetDescriptor;
				textureProperties.depthBufferBits = 0;
				textureProperties.colorFormat = RenderTextureFormat.RGHalf;
				rendererFeature.gbuffer = UniversalRenderer.CreateRenderGraphTexture(renderGraph, textureProperties, "XGBAA GBuffer", false);

				// actual build render graph

				builder.SetRenderAttachment(rendererFeature.gbuffer, 0);
				builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);
				builder.UseRendererList(passData.rendererListHandle);

				builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
			}
		}

		static void ExecutePass(PassData data, RasterGraphContext context)
		{
			context.cmd.ClearRenderTarget(true, true, Color.black);
			context.cmd.DrawRendererList(data.rendererListHandle);
		}
	}

	class XGBAAResolvePass : ScriptableRenderPass
	{
		private class CopyCameraColorPassData
		{
			public TextureHandle source;
			public TextureHandle destination;
		}

		private class ResolvePassData
		{
			public TextureHandle cameraColorCopy;
			public TextureHandle gbuffer;

			public TextureHandle destination;
		}

		private XGBAARendererFeature rendererFeature;
		private MaterialPropertyBlock propertyBlock;

		public XGBAAResolvePass(XGBAARendererFeature rendererFeature)
		{
			this.rendererFeature = rendererFeature;
			propertyBlock = new MaterialPropertyBlock();
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
		{
			// get all sorts of data from the frame context

			var resourcesData = frameContext.Get<UniversalResourceData>();
			var cameraData = frameContext.Get<UniversalCameraData>();

			// create a texture to copy current active color texture to

			var targetDesc = renderGraph.GetTextureDesc(resourcesData.cameraColor);
			targetDesc.name = "XGBAA Camera Color";
			targetDesc.clearBuffer = false;

			var cameraColorCopy = renderGraph.CreateTexture(targetDesc);

			// build render graph for copying camera color

			using (var builder = renderGraph.AddRasterRenderPass<CopyCameraColorPassData>("XGBAA Copy Camera Color", out var passData, profilingSampler))
			{
				passData.source = resourcesData.activeColorTexture;
				passData.destination = cameraColorCopy;

				builder.UseTexture(passData.source);

				builder.SetRenderAttachment(passData.destination, 0);

				builder.SetRenderFunc((CopyCameraColorPassData data, RasterGraphContext context) =>
				{
					Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 0.0f, false);
				});
			}

			// build render graph for resolve pass

			using (var builder = renderGraph.AddRasterRenderPass<ResolvePassData>("XGBAA Resolve Pass", out var passData, profilingSampler))
			{
				passData.cameraColorCopy = cameraColorCopy;
				passData.gbuffer = rendererFeature.gbuffer;
				passData.destination = resourcesData.activeColorTexture;

				builder.UseTexture(passData.cameraColorCopy);
				builder.UseTexture(passData.gbuffer);
				builder.SetRenderAttachment(passData.destination, 0);

				builder.SetRenderFunc((ResolvePassData data, RasterGraphContext context) =>
				{
					propertyBlock.SetTexture("_CameraColorCopy", data.cameraColorCopy);
					propertyBlock.SetTexture("_GBuffer", data.gbuffer);
					propertyBlock.SetFloat("_DebugGBufferAlpha", rendererFeature.debugGBufferAlpha);

					var material = rendererFeature.debugGBufferAlpha > 0.01f ? rendererFeature.debugMaterial : rendererFeature.resolveMaterial;

					// copied form Unity URP's FullScreenPassRendererFeature.cs
					// it seems the FullScreen Shader Graph determines the vertex position based on vertex index
					// and it made this triangle large enough to cover the whole screen
					context.cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3, 1, propertyBlock);
				});
			}
		}
	}
}
