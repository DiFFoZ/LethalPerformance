using System;
using LethalPerformance.API;
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

        asset.currentPlatformRenderPipelineSettings = renderSettings;

        var settings = HDRenderPipelineGlobalSettings.instance;
        ref var frameSettings = ref settings.GetDefaultFrameSettings(FrameSettingsRenderType.Camera);
        frameSettings.bitDatas[(uint)FrameSettingsField.StopNaN] = false;
        frameSettings.bitDatas[(uint)FrameSettingsField.DepthPrepassWithDeferredRendering] = true;
        frameSettings.bitDatas[(uint)FrameSettingsField.ClearGBuffers] = true;
        frameSettings.bitDatas[(uint)FrameSettingsField.Shadowmask] = false;
        LethalPerformancePlugin.Instance.Logger.LogInfo("Disabled StopNan and enabled DepthPrepassWithDeferredRendering globally");
    }

}
