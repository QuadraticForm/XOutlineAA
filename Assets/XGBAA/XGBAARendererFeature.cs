using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
//using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using static Unity.Burst.Intrinsics.X86.Avx;

public class XGBAARendererFeature : ScriptableRendererFeature
{
	#region GBuffer Pass Fields

	[Header("GBuffer Pass")]

	public Material gbufferMaterial;
	public LayerMask layerMask = 0;

	public List<string> shaderTagNameList = new List<string>
	{
		"UniversalForward",
		"UniversalGBuffer", // this is to ensure shaders like UnlitShaderGraph are included
							// (which doesn't have a light mode in UniversalForward, but do have a UniversalGBuffer pass)
	};

	// ShaderTagId list moved to RenderFeature as a field
	private List<ShaderTagId> shaderTagIdList;

	XGBAAGBufferPass gbufferPass;

	#endregion

	#region Internal Edge Detection(Removal) Pass Fields

	/*
	[Header("Edge Detection Pass")]

	public bool enableEdgeDetection = false;

	public Material edgeDetectionMaterial;

	private XGBAAEdgeDetectionPass edgeDetectionPass;
	*/

	#endregion

	#region Resolve Pass Fields

	[Header("Resolve Pass")]

	[Range(0, 1), Tooltip("If alpha == 0, won't execute this pass")]
	public float resolveAlpha = 1.0f;

	public Material resolveMaterial;

	XGBAAPostProcessPass resolvePass;

	#endregion

	#region Debug Pass Fields

	[Header("Debug Pass")]

	[Range(0, 1), Tooltip("If alpha == 0, won't execute this pass")]
	public float debugAlpha = 0.0f;

	public Material debugMaterial;

	XGBAAPostProcessPass debugPass;

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

		gbufferPass = new XGBAAGBufferPass(this);
		gbufferPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

		resolvePass = new XGBAAPostProcessPass(this, resolveMaterial, resolveAlpha);
		resolvePass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

		debugPass = new XGBAAPostProcessPass(this, debugMaterial, debugAlpha);
		debugPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		if (gbufferMaterial != null)
			renderer.EnqueuePass(gbufferPass);

		if (resolveMaterial != null && resolveAlpha > 0.0001f)
			renderer.EnqueuePass(resolvePass);

		if (debugMaterial != null && debugAlpha > 0.0001f)
			renderer.EnqueuePass(debugPass);
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

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            throw new System.NotImplementedException();
        }

        /*public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
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
				var drawSettings = RenderingUtils.CreateDrawingSettings(rendererFeature.shaderTagIdList, renderingData, cameraData, lightData, sortFlags);

				drawSettings.overrideMaterial = rendererFeature.gbufferMaterial;

				var filterSettings = new FilteringSettings(RenderQueueRange.opaque, rendererFeature.layerMask);

				var rendererListParameters = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);
				passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParameters);

				// create render target

				var textureProperties = cameraData.cameraTargetDescriptor;
				textureProperties.depthBufferBits = 0;
				textureProperties.colorFormat = RenderTextureFormat.ARGBHalf;
				rendererFeature.gbuffer = UniversalRenderer.CreateRenderGraphTexture(renderGraph, textureProperties, "XGBAA GBuffer", false);

				// actual build render graph

				builder.SetRenderAttachment(rendererFeature.gbuffer, 0);
				builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);
				builder.UseRendererList(passData.rendererListHandle);

				builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
			}
		}*/

		/*static void ExecutePass(PassData data, RasterGraphContext context)
		{
			// Clear with 2
			// gbuffer stores the pixel-edge distance,
			// 0 means on the edge, 1 means 1 pixel away from the edge,
			// 1 is the maximum value GBufferShader will output,
			// and all value outside [-1, 1] is disregarded by the resolve pass
			// so 2 is large enough to be used as a invalid value
			context.cmd.ClearRenderTarget(false, true, new Color(-2, 2, -2, 2));
			// context.cmd.ClearRenderTarget(false, true, Color.black);

			context.cmd.DrawRendererList(data.rendererListHandle);
		}*/
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
			public TextureHandle cameraDepth;
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

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            throw new System.NotImplementedException();
        }
        /*public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
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
		}*/
	}

	/*
	public class XGBAAEdgeDetectionPass : ScriptableRenderPass
	{
		private XGBAARendererFeature rendererFeature;
		private Material edgeDetectionMaterial;
		private MaterialPropertyBlock propertyBlock;

		public XGBAAEdgeDetectionPass(XGBAARendererFeature rendererFeature, Material edgeDetectionMaterial)
		{
			this.rendererFeature = rendererFeature;
			this.edgeDetectionMaterial = edgeDetectionMaterial;

			propertyBlock = new MaterialPropertyBlock();
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
		{
			using (var builder = renderGraph.AddRasterRenderPass<PassData>("XGBAA Edge Detection Pass", out var passData))
			{
				var resourcesData = frameContext.Get<UniversalResourceData>();
				var cameraData = frameContext.Get<UniversalCameraData>();

				passData.cameraDepth = resourcesData.cameraDepthTexture;
				passData.cameraNormals = resourcesData.cameraNormalsTexture;
				passData.destination = resourcesData.activeColorTexture;

				builder.UseTexture(passData.cameraDepth);
				builder.UseTexture(passData.cameraNormals);
				builder.SetRenderAttachment(passData.destination, 0);

				builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
				{
					var cmd = context.cmd;

					propertyBlock.SetTexture("_CameraDepthTexture", data.cameraDepth);
					propertyBlock.SetTexture("_CameraNormalsTexture", data.cameraNormals);

					context.cmd.DrawProcedural(Matrix4x4.identity, edgeDetectionMaterial, 0, MeshTopology.Triangles, 3, 1, propertyBlock);
				});
			}
		}

		private class PassData
		{
			public TextureHandle cameraDepth;
			public TextureHandle cameraNormals;
			public TextureHandle destination;
		}
	}

	*/
}
