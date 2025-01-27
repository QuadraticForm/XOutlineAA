using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class XOutlineAARendererFeature : ScriptableRendererFeature
{
	#region GBuffer Passes Fields

	[Header("GBuffer Passes")]

	public LayerMask layerMask = 0;

	public List<string> shaderTagNameList = new List<string>
	{
		"UniversalForward",
		"UniversalGBuffer", // this is to ensure shaders like UnlitShaderGraph are included
							// (which doesn't have a light mode in UniversalForward, but do have a UniversalGBuffer pass)
	};

	private List<ShaderTagId> shaderTagIdList;

	XOutlineDrawFrontNormalPass frontNormalPass;

	public Material frontNormalMaterial;

	XOutlineDrawOutlinePass outlineGBufferPass;

	public Material outlineMaterial;

	#endregion

	#region Resolve Pass Fields

	[Header("Resolve Pass")]

	[Range(0, 1), Tooltip("If alpha == 0, won't execute this pass")]
	public float resolveAlpha = 1.0f;

	public Material resolveMaterial;

	XOutlinePostProcessPass resolvePass;

	#endregion

	#region Debug Pass Fields

	[Header("Debug Pass")]

	[Range(0, 1), Tooltip("If alpha == 0, won't execute this pass")]
	public float debugAlpha = 0.0f;

	public Material debugMaterial;

	XOutlinePostProcessPass debugPass;

	#endregion

	#region Shared Fields

	// Shared gbuffer texture
	private TextureHandle gbuffer1 = TextureHandle.nullHandle;
	private TextureHandle gbuffer2 = TextureHandle.nullHandle;

	#endregion

	public override void Create()
	{
		shaderTagIdList = new List<ShaderTagId>();
		foreach (var passName in shaderTagNameList)
		{
			shaderTagIdList.Add(new ShaderTagId(passName));
		}

		frontNormalPass = new XOutlineDrawFrontNormalPass(this, frontNormalMaterial, clearGBuffer: true, createGBuffer: true);
		frontNormalPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

		outlineGBufferPass = new XOutlineDrawOutlinePass(this, outlineMaterial, clearGBuffer: false, createGBuffer: false);
		outlineGBufferPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

		resolvePass = new XOutlinePostProcessPass(this, resolveMaterial, resolveAlpha);
		resolvePass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

		debugPass = new XOutlinePostProcessPass(this, debugMaterial, debugAlpha);
		debugPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		renderer.EnqueuePass(frontNormalPass);
		renderer.EnqueuePass(outlineGBufferPass);
		renderer.EnqueuePass(resolvePass);
		renderer.EnqueuePass(debugPass);
	}

	class XOutlineDrawObjectsPass : ScriptableRenderPass
	{
		private XOutlineAARendererFeature rendererFeature;
		private Material overrideMaterial;
		private bool clearGBuffer;
		private bool createGBuffer;

		public static class ShaderPropertyId
		{
			public static readonly int IsGBufferPass = Shader.PropertyToID("_IsGBufferPass");
		}

		// XX: is this really necessary?
		protected class PassData
		{
			public RendererListHandle rendererListHandle;
		}

		public XOutlineDrawObjectsPass(XOutlineAARendererFeature rendererFeature, Material overrideMaterial = null, bool clearGBuffer = false, bool createGBuffer = false)
		{
			this.rendererFeature = rendererFeature;
			this.overrideMaterial = overrideMaterial;
			this.clearGBuffer = clearGBuffer;
			this.createGBuffer = createGBuffer;
		}

		protected virtual void CreateRendererList(RenderGraph renderGraph, PassData passData, UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData)
		{
			var sortFlags = cameraData.defaultOpaqueSortFlags;
			var drawSettings = RenderingUtils.CreateDrawingSettings(rendererFeature.shaderTagIdList, renderingData, cameraData, lightData, sortFlags);

			if (overrideMaterial != null)
			{
				drawSettings.overrideMaterial = overrideMaterial;
			}

			var filterSettings = new FilteringSettings(RenderQueueRange.all, rendererFeature.layerMask);

			var rendererListParameters = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);
			passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParameters);
		}

		protected virtual void CreateRenderTargets(RenderGraph renderGraph, UniversalCameraData cameraData)
		{
			if (createGBuffer)
			{
				var textureProperties = cameraData.cameraTargetDescriptor;
				textureProperties.depthBufferBits = 0;
				textureProperties.colorFormat = RenderTextureFormat.ARGBFloat;
				// textureProperties.colorFormat = RenderTextureFormat.ARGBHalf;

				rendererFeature.gbuffer1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, textureProperties, "XOutline GBuffer 1", false);

				textureProperties.colorFormat = RenderTextureFormat.ARGB32;

				rendererFeature.gbuffer2 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, textureProperties, "XOutline GBuffer 2", false);
			}
		}

		protected virtual void SetupRenderGraph(IRasterRenderGraphBuilder builder, PassData passData, UniversalResourceData resourceData)
		{
			builder.AllowGlobalStateModification(true);

			builder.SetRenderAttachment(rendererFeature.gbuffer1, 0);
			builder.SetRenderAttachment(rendererFeature.gbuffer2, 1);
			builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);
			builder.UseRendererList(passData.rendererListHandle);

			builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
			{
				context.cmd.SetGlobalFloat(ShaderPropertyId.IsGBufferPass, 1);

				if (clearGBuffer)
				{
					context.cmd.ClearRenderTarget(false, true, new Color(0, 0, 0, 0));
				}

				context.cmd.DrawRendererList(data.rendererListHandle);

				context.cmd.SetGlobalFloat(ShaderPropertyId.IsGBufferPass, 0);
			});
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
		{
			using (var builder = renderGraph.AddRasterRenderPass<PassData>("XOutline GBuffer Pass", out var passData))
			{
				// get all sorts of data from the frame context

				var renderingData = frameContext.Get<UniversalRenderingData>();
				var resourceData = frameContext.Get<UniversalResourceData>();
				var cameraData = frameContext.Get<UniversalCameraData>();
				var lightData = frameContext.Get<UniversalLightData>();

				// create renderer list

				CreateRendererList(renderGraph, passData, renderingData, cameraData, lightData);

				// create render target

				CreateRenderTargets(renderGraph, cameraData);

				// actual build render graph

				SetupRenderGraph(builder, passData, resourceData);
			}
		}
	}

	class XOutlineDrawFrontNormalPass : XOutlineDrawObjectsPass
	{
		public XOutlineDrawFrontNormalPass(XOutlineAARendererFeature rendererFeature, Material overrideMaterial = null, bool clearGBuffer = false, bool createGBuffer = false)
			: base(rendererFeature, overrideMaterial, clearGBuffer, createGBuffer)
		{
		}

		protected override void CreateRendererList(RenderGraph renderGraph, PassData passData, UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData)
		{
			base.CreateRendererList(renderGraph, passData, renderingData, cameraData, lightData);
		}

		protected override void CreateRenderTargets(RenderGraph renderGraph, UniversalCameraData cameraData)
		{
			base.CreateRenderTargets(renderGraph, cameraData);
		}

		protected override void SetupRenderGraph(IRasterRenderGraphBuilder builder, PassData passData, UniversalResourceData resourceData)
		{
			base.SetupRenderGraph(builder, passData, resourceData);
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
		{
			base.RecordRenderGraph(renderGraph, frameContext);
		}
	}

	class XOutlineDrawOutlinePass : XOutlineDrawObjectsPass
	{
		public XOutlineDrawOutlinePass(XOutlineAARendererFeature rendererFeature, Material overrideMaterial = null, bool clearGBuffer = false, bool createGBuffer = false)
			: base(rendererFeature, overrideMaterial, clearGBuffer, createGBuffer)
		{
		}

		protected override void CreateRendererList(RenderGraph renderGraph, PassData passData, UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData)
		{
			base.CreateRendererList(renderGraph, passData, renderingData, cameraData, lightData);
		}

		protected override void CreateRenderTargets(RenderGraph renderGraph, UniversalCameraData cameraData)
		{
			base.CreateRenderTargets(renderGraph, cameraData);
		}

		protected override void SetupRenderGraph(IRasterRenderGraphBuilder builder, PassData passData, UniversalResourceData resourceData)
		{
			base.SetupRenderGraph(builder, passData, resourceData);
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
		{
			base.RecordRenderGraph(renderGraph, frameContext);
		}
	}

	class XOutlinePostProcessPass : ScriptableRenderPass
	{
		private class CopyCameraColorPassData
		{
			public TextureHandle source;
			public TextureHandle destination;
		}

		private class MainPassData
		{
			public TextureHandle cameraColorCopy;
			public TextureHandle cameraDepth;
			public TextureHandle gbuffer;
			public TextureHandle gbuffer2;

			public TextureHandle destination;
		}

		public Material postProcessMaterial;
		public float alpha = 1;
		private XOutlineAARendererFeature rendererFeature;
		private MaterialPropertyBlock propertyBlock;

		public XOutlinePostProcessPass(XOutlineAARendererFeature rendererFeature, Material postProcessMaterial, float alpha)
		{
			this.rendererFeature = rendererFeature;
			propertyBlock = new MaterialPropertyBlock();
			this.postProcessMaterial = postProcessMaterial;
			this.alpha = alpha;
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
		{
			if (postProcessMaterial == null || alpha < 0.000001)
				return;

			// get all sorts of data from the frame context

			var resourcesData = frameContext.Get<UniversalResourceData>();
			var cameraData = frameContext.Get<UniversalCameraData>();

			// create a texture to copy current active color texture to

			var targetDesc = renderGraph.GetTextureDesc(resourcesData.cameraColor);
			targetDesc.name = "XOutline Camera Color";
			targetDesc.clearBuffer = false;

			var cameraColorCopy = renderGraph.CreateTexture(targetDesc);

			// build render graph for copying camera color

			using (var builder = renderGraph.AddRasterRenderPass<CopyCameraColorPassData>("XOutline Copy Camera Color", out var passData, profilingSampler))
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

			using (var builder = renderGraph.AddRasterRenderPass<MainPassData>("XOutline Post-Process", out var passData, profilingSampler))
			{
				passData.cameraColorCopy = cameraColorCopy;
				passData.cameraDepth = resourcesData.cameraDepthTexture;
				passData.gbuffer = rendererFeature.gbuffer1;
				passData.gbuffer2 = rendererFeature.gbuffer2;
				passData.destination = resourcesData.activeColorTexture;

				builder.UseTexture(passData.cameraColorCopy);
				builder.UseTexture(passData.cameraDepth);
				builder.UseTexture(passData.gbuffer);
				builder.UseTexture(passData.gbuffer2);
				builder.SetRenderAttachment(passData.destination, 0);

				builder.SetRenderFunc((MainPassData data, RasterGraphContext context) =>
				{
					propertyBlock.SetTexture("_CameraColorCopy", data.cameraColorCopy);
					propertyBlock.SetTexture("_CameraDepthCopy", data.cameraDepth); // "_CameraDepthTexture" is in use by unity, so I just use "_CameraDepthCopy" instead
					propertyBlock.SetTexture("_GBuffer", data.gbuffer);
					propertyBlock.SetTexture("_GBuffer2", data.gbuffer2);
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
