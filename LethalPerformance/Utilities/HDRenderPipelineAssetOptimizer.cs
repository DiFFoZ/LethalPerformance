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
        renderSettings.hdShadowInitParams.cachedAreaLightShadowAtlas = 8192;
        renderSettings.hdShadowInitParams.cachedPunctualLightShadowAtlas = 8192;
        renderSettings.hdShadowInitParams.allowDirectionalMixedCachedShadows = true;

        var supportsVolumetric = false && SystemInfo.supportsRenderTargetArrayIndexFromVertexShader;
        renderSettings.supportVolumetrics = supportsVolumetric;

        asset.currentPlatformRenderPipelineSettings = renderSettings;

        var settings = HDRenderPipelineGlobalSettings.instance;
        ref var frameSettings = ref settings.GetDefaultFrameSettings(FrameSettingsRenderType.Camera);
        frameSettings.SetEnabled(FrameSettingsField.Volumetrics, supportsVolumetric);
        frameSettings.SetEnabled(FrameSettingsField.StopNaN, false);
        frameSettings.SetEnabled(FrameSettingsField.DepthPrepassWithDeferredRendering, true);
        frameSettings.SetEnabled(FrameSettingsField.ClearGBuffers, true);
        frameSettings.SetEnabled(FrameSettingsField.Shadowmask, false);
        LethalPerformancePlugin.Instance.Logger.LogInfo("Disabled StopNan and enabled DepthPrepassWithDeferredRendering globally");

        if (!supportsVolumetric)
        {
            LethalPerformancePlugin.Instance.Logger.LogInfo("Disabled volumetric fog as hardware system doesn't support it");
        }
    }

}
