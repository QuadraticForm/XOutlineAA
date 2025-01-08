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

	[Header("Debug Pass")]
	[Range(0, 1), Tooltip("If alpha == 0, won't do debug")]
	public float debugGBufferAlpha = 0.0f;

	public Material debugMaterial;
	
	XGBAAPostProcessPass debugPass;

	[Header("Resolve Pass")]

	public Material resolveMaterial;

	XGBAAPostProcessPass resolvePass;

	// Shared gbuffer texture
	private TextureHandle gbuffer = TextureHandle.nullHandle;

	public override void Create()
	{
		gbufferPass = new XGBAAGBufferPass(this);
		gbufferPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

		debugPass = new XGBAAPostProcessPass(this, debugMaterial, debugGBufferAlpha);
		debugPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

		resolvePass = new XGBAAPostProcessPass(this, resolveMaterial, 1); // resolve pass's alpha should always be 1
		resolvePass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		renderer.EnqueuePass(gbufferPass);
		if (debugGBufferAlpha > 0.0001f)
			renderer.EnqueuePass(debugPass);
		//renderer.EnqueuePass(resolvePass);
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
				textureProperties.colorFormat = RenderTextureFormat.RGFloat;
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
			// Clear with 2
			// gbuffer stores the pixel-edge distance,
			// 0 means on the edge, 1 means 1 pixel away from the edge,
			// 2 is far enough to be used as a default value
			context.cmd.ClearRenderTarget(true, true, new Color(2,2,2));
			// context.cmd.ClearRenderTarget(true, true, Color.black);

			context.cmd.DrawRendererList(data.rendererListHandle);
		}
	}

	class XGBAAPostProcessPass : ScriptableRenderPass
	{
		private class CopyCameraColorPassData
		{
			public TextureHandle source;
			public TextureHandle destination;
		}

		private class MainPassData
		{
			public TextureHandle cameraColorCopy;
			public TextureHandle gbuffer;

			public TextureHandle destination;
		}

		public Material postProcessMaterial;
		public float alpha = 1;
		private XGBAARendererFeature rendererFeature;
		private MaterialPropertyBlock propertyBlock;

		public XGBAAPostProcessPass(XGBAARendererFeature rendererFeature, Material postProcessMaterial, float alpha)
		{
			this.rendererFeature = rendererFeature;
			propertyBlock = new MaterialPropertyBlock();
			this.postProcessMaterial = postProcessMaterial;
			this.alpha = alpha;
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

			// build render graph for post process pass

			using (var builder = renderGraph.AddRasterRenderPass<MainPassData>("XGBAA Post-Process", out var passData, profilingSampler))
			{
				passData.cameraColorCopy = cameraColorCopy;
				passData.gbuffer = rendererFeature.gbuffer;
				passData.destination = resourcesData.activeColorTexture;

				builder.UseTexture(passData.cameraColorCopy);
				builder.UseTexture(passData.gbuffer);
				builder.SetRenderAttachment(passData.destination, 0);

				builder.SetRenderFunc((MainPassData data, RasterGraphContext context) =>
				{
					propertyBlock.SetTexture("_CameraColorCopy", data.cameraColorCopy);
					propertyBlock.SetTexture("_GBuffer", data.gbuffer);
					propertyBlock.SetFloat("_Alpha", alpha);

					// var material = rendererFeature.debugGBufferAlpha > 0.01f ? rendererFeature.debugMaterial : rendererFeature.resolveMaterial;

					// copied form Unity URP's FullScreenPassRendererFeature.cs
					// it seems the FullScreen Shader Graph determines the vertex position based on vertex index
					// and it made this triangle large enough to cover the whole screen
					context.cmd.DrawProcedural(Matrix4x4.identity, postProcessMaterial, 0, MeshTopology.Triangles, 3, 1, propertyBlock);
				});
			}
		}
	}
}
