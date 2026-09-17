using LethalPerformance.Patcher.API;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace LethalPerformance.Utilities;
internal static class HDRenderPipelineAssetOptimizer
{
    [InitializeOnAwake]
    public static void Initialize()
    {
        var asset = (HDRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        var renderSettings = asset.currentPlatformRenderPipelineSettings;

        renderSettings.lightLoopSettings.reflectionProbeTexCacheSize = LethalPerformancePlugin.Instance.Configuration.ReflectionProbeCacheResolution.Value;
        renderSettings.lightLoopSettings.cookieAtlasSize = (CookieAtlasResolution)LethalPerformancePlugin.Instance.Configuration.CookieAtlasResolution.Value;
        renderSettings.supportDataDrivenLensFlare = false;

        var supportsVolumetric = SystemInfo.supportsRenderTargetArrayIndexFromVertexShader;
        renderSettings.supportVolumetrics = supportsVolumetric;

        asset.currentPlatformRenderPipelineSettings = renderSettings;

        var settings = HDRenderPipelineGlobalSettings.instance;
        settings.rendererListCulling = true;
        settings.supportRuntimeDebugDisplay = false;

        ref var frameSettings = ref settings.GetDefaultFrameSettings(FrameSettingsRenderType.Camera);
        frameSettings.SetEnabled(FrameSettingsField.StopNaN, false);
        frameSettings.SetEnabled(FrameSettingsField.DepthPrepassWithDeferredRendering, true);
        frameSettings.SetEnabled(FrameSettingsField.ContactShadows, false);
        frameSettings.SetEnabled(FrameSettingsField.PlanarProbe, false);
        frameSettings.SetEnabled(FrameSettingsField.LensFlareDataDriven, false);

        if (!supportsVolumetric)
        {
            frameSettings.SetEnabled(FrameSettingsField.Volumetrics, false);
            LethalPerformancePlugin.Instance.Logger.LogInfo("Disabled volumetric fog as hardware system doesn't support it");
        }
    }
}
