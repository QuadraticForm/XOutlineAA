using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using static Unity.Burst.Intrinsics.X86.Avx;
using static UnityEngine.Rendering.Universal.ShaderInput;

public class XOutlineRendererFeature : ScriptableRendererFeature
{
	#region GBuffer Pass Fields

	[Header("GBuffer Pass")]

	// public Material gbufferMaterial;
	public LayerMask layerMask = 0;

	public List<string> shaderTagNameList = new List<string>
	{
		"UniversalForward",
		"UniversalGBuffer", // this is to ensure shaders like UnlitShaderGraph are included
							// (which doesn't have a light mode in UniversalForward, but do have a UniversalGBuffer pass)
	};

	// ShaderTagId list moved to RenderFeature as a field
	private List<ShaderTagId> shaderTagIdList;

	XOutlineGBufferPass gbufferPass;

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
	private TextureHandle gbuffer = TextureHandle.nullHandle;

	#endregion

	public override void Create()
	{
		shaderTagIdList = new List<ShaderTagId>();
		foreach (var passName in shaderTagNameList)
		{
			shaderTagIdList.Add(new ShaderTagId(passName));
		}

		gbufferPass = new XOutlineGBufferPass(this);
		gbufferPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

		resolvePass = new XOutlinePostProcessPass(this, resolveMaterial, resolveAlpha);
		resolvePass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

		debugPass = new XOutlinePostProcessPass(this, debugMaterial, debugAlpha);
		debugPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		renderer.EnqueuePass(gbufferPass);

		/*
		if (enableEdgeDetection)
			renderer.EnqueuePass(edgeDetectionPass);
		*/

		if (resolveAlpha > 0.0001f)
			renderer.EnqueuePass(resolvePass);

		if (debugAlpha > 0.0001f)
			renderer.EnqueuePass(debugPass);
	}

	class XOutlineGBufferPass : ScriptableRenderPass
	{
		private XOutlineRendererFeature rendererFeature;
		// private MaterialPropertyBlock propertyBlock;

		public static class ShaderPropertyId
		{
			public static readonly int IsGBufferPass = Shader.PropertyToID("_IsGBufferPass");
		}

		private class PassData
		{
			public RendererListHandle rendererListHandle;
		}

		public XOutlineGBufferPass(XOutlineRendererFeature rendererFeature)
		{
			this.rendererFeature = rendererFeature;
			// this.propertyBlock = new MaterialPropertyBlock();
			// this.propertyBlock.SetFloat("_IsGBufferPass", 1.0f);
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

				var sortFlags = cameraData.defaultOpaqueSortFlags;
				var drawSettings = RenderingUtils.CreateDrawingSettings(rendererFeature.shaderTagIdList, renderingData, cameraData, lightData, sortFlags);

				// drawSettings.overrideMaterial = rendererFeature.gbufferMaterial;

				var filterSettings = new FilteringSettings(RenderQueueRange.all, rendererFeature.layerMask);

				var rendererListParameters = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);
				passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParameters);

				// create render target

				var textureProperties = cameraData.cameraTargetDescriptor;
				textureProperties.depthBufferBits = 0;
				textureProperties.colorFormat = RenderTextureFormat.ARGBHalf;
				rendererFeature.gbuffer = UniversalRenderer.CreateRenderGraphTexture(renderGraph, textureProperties, "XOutline GBuffer", false);

				// actual build render graph

				builder.AllowGlobalStateModification(true);

				builder.SetRenderAttachment(rendererFeature.gbuffer, 0);
				builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);
				builder.UseRendererList(passData.rendererListHandle);

				builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
				{
					context.cmd.SetGlobalFloat(ShaderPropertyId.IsGBufferPass, 1);

					context.cmd.ClearRenderTarget(false, true, new Color(0, 0, 0, 0));

					context.cmd.DrawRendererList(data.rendererListHandle);

					context.cmd.SetGlobalFloat(ShaderPropertyId.IsGBufferPass, 0);
				});
			}
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

			public TextureHandle destination;
		}

		public Material postProcessMaterial;
		public float alpha = 1;
		private XOutlineRendererFeature rendererFeature;
		private MaterialPropertyBlock propertyBlock;

		public XOutlinePostProcessPass(XOutlineRendererFeature rendererFeature, Material postProcessMaterial, float alpha)
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
				passData.gbuffer = rendererFeature.gbuffer;
				passData.destination = resourcesData.activeColorTexture;

				builder.UseTexture(passData.cameraColorCopy);
				builder.UseTexture(passData.cameraDepth);
				builder.UseTexture(passData.gbuffer);
				builder.SetRenderAttachment(passData.destination, 0);

				builder.SetRenderFunc((MainPassData data, RasterGraphContext context) =>
				{
					propertyBlock.SetTexture("_CameraColorCopy", data.cameraColorCopy);
					propertyBlock.SetTexture("_CameraDepthCopy", data.cameraDepth); // "_CameraDepthTexture" is in use by unity, so I just use "_CameraDepthCopy" instead
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
