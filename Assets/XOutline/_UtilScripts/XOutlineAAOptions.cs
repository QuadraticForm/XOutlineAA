using UnityEngine;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class XOutlineAAOptions : MonoBehaviour
{
	
	[Header("Presets")]
	public bool usePresets = true;

	public enum Preset
	{
		None,
		FXAA,
		SMAA,
		TAA,
		[InspectorName("XOutlineAA")]
		XOutlineAA,
		[InspectorName("FXAA then XOutlineAA")]
		FXAA_XOutlineAA,
		[InspectorName("XOutlineAA then FXAA")]
		XOutlineAA_FXAA,
		[InspectorName("SMAA then XOutlineAA")]
		SMAA_XOutlineAA,
		[InspectorName("XOutlineAA then TAA")]
		XOutlineAA_TAA,
	}

	public Preset preset = Preset.XOutlineAA;

	[Space]
	[Header("XOutlineAA")]
	public XOutlineRendererFeature rendererFeature = null;
	[Space]
	public bool enable = false;
	public bool disableUrpAAWhenEnabled = true;
	public XOutlineRendererFeature.ResolveInjectionPoint injectionPoint = XOutlineRendererFeature.ResolveInjectionPoint.BeforeRenderingPostProcessing;
	[Space]
	public Material resolveMaterial;
	[Min(0), Tooltip("In Pixels")]
	public float minBlurDistance = 8f;
	[Min(0), Tooltip("In Pixels")]
	public float maxBlurDistance = 128f;

	[Space]
	[Header("URP AA")]
	public AntialiasingMode urpAaMode = AntialiasingMode.None;
	public AntialiasingQuality urpSmaaQuality = AntialiasingQuality.Low;

	[Space]
	[Header("(Deprecated) XOutlineAA V5")]
	[Min(0)]
	public float diagonalBlurRadius = 0.1f;
	[Min(0)]
	public float flatBlurRadius = 1f;


	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{
		
	}

	void UpdateSettingsByPreset()
	{
		if (!usePresets)
			return;

		if (preset == Preset.None)
		{
			enable = false;
			urpAaMode = AntialiasingMode.None;
		}
		else if (preset == Preset.FXAA)
		{
			enable = false;
			urpAaMode = AntialiasingMode.FastApproximateAntialiasing;
		}
		else if (preset == Preset.SMAA)
		{
			enable = false;
			urpAaMode = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
		}
		else if (preset == Preset.TAA)
		{
			enable = false;
			urpAaMode = AntialiasingMode.TemporalAntiAliasing;
		}
		else if (preset == Preset.XOutlineAA)
		{
			enable = true;
			urpAaMode = AntialiasingMode.None;
		}
		else if (preset == Preset.FXAA_XOutlineAA)
		{
			enable = true;
			disableUrpAAWhenEnabled = false;
			urpAaMode = AntialiasingMode.FastApproximateAntialiasing;

			// XOutlineAA after FXAA
			injectionPoint = XOutlineRendererFeature.ResolveInjectionPoint.AfterRenderingPostProcessing;
		}
		else if (preset == Preset.XOutlineAA_FXAA)
		{
			enable = true;
			disableUrpAAWhenEnabled = false;
			urpAaMode = AntialiasingMode.FastApproximateAntialiasing;

			// XOutlineAA before FXAA
			injectionPoint = XOutlineRendererFeature.ResolveInjectionPoint.BeforeRenderingPostProcessing;
		}
		else if (preset == Preset.SMAA_XOutlineAA)
		{
			enable = true;
			disableUrpAAWhenEnabled = false;
			urpAaMode = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

			// must after SMAA, cuz SMAA doesn't work well with already smooth(blurred) edges
			injectionPoint = XOutlineRendererFeature.ResolveInjectionPoint.AfterRenderingPostProcessing;
		}
		else if (preset == Preset.XOutlineAA_TAA)
		{
			enable = true;
			disableUrpAAWhenEnabled = false;
			urpAaMode = AntialiasingMode.TemporalAntiAliasing;

			// must before TAA, otherwise it will jitter, cuz XOutline GBuffer is jittering
			injectionPoint = XOutlineRendererFeature.ResolveInjectionPoint.BeforeRenderingPostProcessing;
		}
	}

	void ApplySettings()
	{
		if (Camera.main == null || rendererFeature == null)
			return;

		var urpCameraSettings = Camera.main.GetUniversalAdditionalCameraData();

		urpCameraSettings.antialiasing = urpAaMode;
		urpCameraSettings.antialiasingQuality = urpSmaaQuality;

		rendererFeature.resolveEnabled = enable;
		rendererFeature.resolveInjectionPoint = injectionPoint;

		if (enable && disableUrpAAWhenEnabled)
		{
			urpCameraSettings.antialiasing = AntialiasingMode.None;
		}

		if (resolveMaterial != null)
		{
			// XOutlineAA V6, the blur distance is determined by rasterized line's step length in pixels,
			// so its always correct regardless of the resolution and line angle.
			//
			// but pixels at which the line is completely vertical or horizontal,
			// the blur distance will be too large.
			// and pixels at which the line is at 45 degree,
			// the mathematical blur distance is too small to give a smooth result,
			// so we need to clamp it.
			resolveMaterial.SetFloat("_MinBlurDistance", minBlurDistance);
			resolveMaterial.SetFloat("_MaxBlurDistance", maxBlurDistance);

			// (Deprecated and WRONG)
			// XOutlineAA V5, arbitrary values, not scalable on different resolutions,
			// which means for different resolutions, the optimal blur radius will be different,
			// and mathmetically the interpolation between diagonal and flat blur radius should not be linear
			resolveMaterial.SetFloat("_DiagonalBlurRadius", diagonalBlurRadius);
			resolveMaterial.SetFloat("_FlatBlurRadius", flatBlurRadius);

			rendererFeature.resolveMaterial = resolveMaterial;

		}
	}

	// Update is called once per frame
	void Update()
	{
		UpdateSettingsByPreset();

		ApplySettings();
	}
}
