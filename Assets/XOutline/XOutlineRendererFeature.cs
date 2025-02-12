using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class XOutlineRendererFeature : ScriptableRendererFeature
{
	#region Shared Fields

	[Header("Common")]

	public LayerMask layerMask = 0;

	public List<string> shaderTagNameList = new List<string>
	{
		"UniversalForward",
		"UniversalGBuffer", // this is to ensure shaders like UnlitShaderGraph are included
							// (which doesn't have a light mode in UniversalForward, but do have a UniversalGBuffer pass)
	};

	private List<ShaderTagId> shaderTagIdList;

	public enum GBufferPrecision
	{
		Float,
		Half,
	}

	[Space]
	public GBufferPrecision gbufferPrecision = GBufferPrecision.Half;

    // xy: normal in spherical coordinates, zw: delta screen space position between offseted and original
	private RenderTargetIdentifier gbuffer1;

    // DEPRECATED since resolve shader v6, only kept for testing purpose
    // Outline Color and Alpha, 
    // separately stored without blending with camera Color,
    // for coverage bluring in resolve pass.
    private RenderTargetIdentifier gbuffer2;

    XOutlinePreparePass preparePass;

	#endregion

	#region Front Normal Pass

	[Header("Normal Pass")]
	public Material frontNormalMaterial;

	XOutlineFrontNormalPass frontNormalPass;

	#endregion

	#region Outline Passes Fields

	[Header("Outline Pass")]
	public Material outlineMaterial;

	XOutlineOutlinePass outlineGBufferPass;

	#endregion

	#region Resolve Pass Fields

	XOutlinePostProcessPass resolvePass;

	[Header("Resolve Pass")]

	public bool resolveEnabled = true;

	[Range(0, 1), Tooltip("If alpha == 0, won't execute this pass")]
	public float resolveAlpha = 1.0f;

	public Material resolveMaterial;

	public enum ResolveInjectionPoint
	{
		BeforeRenderingPostProcessing = RenderPassEvent.BeforeRenderingPostProcessing,
		AfterRenderingPostProcessing = RenderPassEvent.AfterRenderingPostProcessing,
	}

	public ResolveInjectionPoint resolveInjectionPoint = ResolveInjectionPoint.BeforeRenderingPostProcessing;

    #endregion

    #region Debug Pass Fields

    [Header("Debug Pass")]

	public bool debugEnabled = false;

	[Range(0, 1), Tooltip("If alpha == 0, won't execute this pass")]
	public float debugAlpha = 1.0f;

	public Material debugMaterial;

	XOutlinePostProcessPass debugPass;

	#endregion

    public override void Create()
	{
		shaderTagIdList = new List<ShaderTagId>();
		foreach (var passName in shaderTagNameList)
		{
			shaderTagIdList.Add(new ShaderTagId(passName));
		}

		preparePass = new XOutlinePreparePass(this);
		preparePass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;

		outlineGBufferPass = new XOutlineOutlinePass(this, "XOutline Outline Pass", outlineMaterial);
		frontNormalPass = new XOutlineFrontNormalPass(this, "XOutline Front Normal Pass", frontNormalMaterial);
		resolvePass = new XOutlinePostProcessPass(this, resolveMaterial, resolveAlpha);
		debugPass = new XOutlinePostProcessPass(this, debugMaterial, debugAlpha);
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		// set injection points

		outlineGBufferPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
		frontNormalPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
		resolvePass.renderPassEvent = (RenderPassEvent)resolveInjectionPoint;
		debugPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

		// enqueue passes

		renderer.EnqueuePass(preparePass);
		renderer.EnqueuePass(outlineGBufferPass);
		renderer.EnqueuePass(frontNormalPass);

		if (resolveEnabled && resolveAlpha > 0.0f)
			renderer.EnqueuePass(resolvePass);

		if (debugEnabled && debugAlpha > 0.0f)
			renderer.EnqueuePass(debugPass);
	}

	class XOutlinePreparePass : ScriptableRenderPass
	{
		protected XOutlineRendererFeature rendererFeature;
        private const string profilerTag = "PreparePass";
        private ProfilingSampler prepareSampler = new(profilerTag);

        public XOutlinePreparePass(XOutlineRendererFeature rendererFeature)
		{
			this.rendererFeature = rendererFeature;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
			ResetTarget();
			RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
			desc.msaaSamples = 1;
            desc.depthBufferBits = 0;
        }
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			CommandBuffer cmd = CommandBufferPool.Get("XOutline Prepare GBuffer");

			using (new ProfilingScope(cmd, prepareSampler))
			{

				RenderTextureDescriptor textureProperties = renderingData.cameraData.cameraTargetDescriptor;
				textureProperties.depthBufferBits = 0;

				// create gbuffer 1

				if (rendererFeature.gbufferPrecision == GBufferPrecision.Half)
					textureProperties.colorFormat = RenderTextureFormat.ARGBHalf;
				else
					textureProperties.colorFormat = RenderTextureFormat.ARGBFloat;

				cmd.GetTemporaryRT(Shader.PropertyToID("XOutline GBuffer 1"), textureProperties);
				rendererFeature.gbuffer1 = new RenderTargetIdentifier("XOutline GBuffer 1");

				// create gbuffer 2
				// DEPRECATED since resolve shader v6, only kept for testing purpose

				textureProperties.colorFormat = RenderTextureFormat.ARGB32;
				cmd.GetTemporaryRT(Shader.PropertyToID("XOutline GBuffer 2"), textureProperties);
				rendererFeature.gbuffer2 = new RenderTargetIdentifier("XOutline GBuffer 2");

                // clear them
				cmd.SetRenderTarget(rendererFeature.gbuffer1);
                cmd.ClearRenderTarget(false, true, new Color(0, 0, 0, 0));
                cmd.SetRenderTarget(rendererFeature.gbuffer2);
                cmd.ClearRenderTarget(false, true, new Color(0, 0, 0, 0));
			}

			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
        }
    }

	class XOutlineDrawObjectsPass : ScriptableRenderPass
	{
		protected string name;
		protected XOutlineRendererFeature rendererFeature;
		protected Material overrideMaterial;

		protected RenderTargetIdentifier cameraColorTarget;
		protected RenderTargetIdentifier cameraDepthTarget;
        protected RendererList rendererList;

        public XOutlineDrawObjectsPass(XOutlineRendererFeature rendererFeature, string name, Material overrideMaterial = null)
		{
			this.name = name;
			this.rendererFeature = rendererFeature;
			this.overrideMaterial = overrideMaterial;
        }

        protected virtual void SetupRenderTargets(CommandBuffer cmd)
        {
        }

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			CommandBuffer cmd = CommandBufferPool.Get(name);

            cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
            cameraDepthTarget = renderingData.cameraData.renderer.cameraDepthTargetHandle;

            // create renderer list 

            var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
            var drawSettings = RenderingUtils.CreateDrawingSettings(rendererFeature.shaderTagIdList, ref renderingData, sortFlags);

            if (overrideMaterial != null)
            {
                drawSettings.overrideMaterial = overrideMaterial;
            }

            var filterSettings = new FilteringSettings(RenderQueueRange.all, rendererFeature.layerMask);

            var rendererListParameters = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);
            rendererList = context.CreateRendererList(ref rendererListParameters);

            // create render target

            SetupRenderTargets(cmd);

            //context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);
			
			cmd.DrawRendererList(rendererList);

			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
        }
	}

    class XOutlineOutlinePass : XOutlineDrawObjectsPass
	{
        public XOutlineOutlinePass(XOutlineRendererFeature rendererFeature, string name, Material overrideMaterial = null)
			: base(rendererFeature, name, overrideMaterial)
		{
        }

		protected override void SetupRenderTargets(CommandBuffer cmd)
		{
            cmd.SetRenderTarget(
				new RenderTargetIdentifier[] { 
					cameraColorTarget, 
					rendererFeature.gbuffer1, 
					rendererFeature.gbuffer2 }, 
				cameraDepthTarget);
        }
    }

	// Renders the view space normals of the front faces of objects
	// this pass can be skipped if the pipeline already has a normal pass£¬
	// or, if opaque pass uses MRT to output normals
	class XOutlineFrontNormalPass : XOutlineDrawObjectsPass
	{
		public XOutlineFrontNormalPass(XOutlineRendererFeature rendererFeature, string name, Material overrideMaterial = null)
			: base(rendererFeature, name, overrideMaterial)
		{
		}

        protected override void SetupRenderTargets(CommandBuffer cmd)
        {
            cmd.SetRenderTarget(
				new RenderTargetIdentifier[] { 
					rendererFeature.gbuffer1, 
					rendererFeature.gbuffer2 }, 
				cameraDepthTarget);
        }
	}

	class XOutlinePostProcessPass : ScriptableRenderPass
	{
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
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (postProcessMaterial == null || alpha < 0.000001)
                return;

            CommandBuffer cmd = CommandBufferPool.Get("XOutline PostProcess Pass");

            // get all sorts of data 

            RTHandle cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
            RTHandle cameraDepthTarget = renderingData.cameraData.renderer.cameraDepthTargetHandle;

            // create a texture to copy current active color texture to

            var targetDesc = renderingData.cameraData.cameraTargetDescriptor;
			var targetShaderID = Shader.PropertyToID("XOutline Copy Camera Color");
            RenderTargetIdentifier cameraColorCopy = new RenderTargetIdentifier(targetShaderID);
            cmd.GetTemporaryRT(targetShaderID, targetDesc);

            // build render graph for copying camera color

            cmd.SetRenderTarget(cameraColorCopy);
            Blitter.BlitTexture(cmd, cameraColorTarget, new Vector4(1, 1, 0, 0), 0.0f, false);
            //cmd.ClearRenderTarget(true, true, Color.clear);

            // set material properties

            cmd.SetGlobalTexture("_CameraColorCopy", cameraColorCopy);
            cmd.SetGlobalTexture("_GBuffer", rendererFeature.gbuffer1);
            cmd.SetGlobalTexture("_GBuffer2", rendererFeature.gbuffer2);
            cmd.SetGlobalFloat("_Alpha", alpha);


            // draw the post process pass

			cmd.SetRenderTarget(cameraColorTarget, cameraDepthTarget);	
            cmd.DrawProcedural(Matrix4x4.identity, postProcessMaterial, 0, MeshTopology.Triangles, 3, 1);
            //cmd.Blit(cameraColorCopy, cameraColorTarget, postProcessMaterial, 0);

            // release temporary texture
            cmd.ReleaseTemporaryRT(targetShaderID);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}