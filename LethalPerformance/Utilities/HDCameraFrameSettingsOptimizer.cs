using UnityEngine.Rendering.HighDefinition;

namespace LethalPerformance.Utilities;
internal static class HDCameraFrameSettingsOptimizer
{
    private static readonly FrameSettingsField[] s_DisabledExtraCameraFields =
    [
        FrameSettingsField.ProbeVolume,
        FrameSettingsField.VolumetricClouds,
        FrameSettingsField.FullResolutionCloudsForSky,
        FrameSettingsField.Volumetrics,
        FrameSettingsField.ReprojectionForVolumetrics,
        FrameSettingsField.AtmosphericScattering,
        FrameSettingsField.MotionVectors,
        FrameSettingsField.ObjectMotionVectors,
        FrameSettingsField.TransparentsWriteMotionVector,
        FrameSettingsField.Distortion,
        FrameSettingsField.RoughDistortion,
        FrameSettingsField.SubsurfaceScattering,
        FrameSettingsField.Transmission,
        FrameSettingsField.ContactShadows,
        FrameSettingsField.ScreenSpaceShadows,
        FrameSettingsField.ShadowMaps,
        FrameSettingsField.Shadowmask,
        FrameSettingsField.Refraction,
        FrameSettingsField.AfterPostprocess,
        FrameSettingsField.SSR,
        FrameSettingsField.TransparentSSR,
        FrameSettingsField.SSAO,
        FrameSettingsField.SSGI,
        FrameSettingsField.Decals,
        FrameSettingsField.DecalLayers,
        FrameSettingsField.DepthPrepassWithDeferredRendering,
        FrameSettingsField.PlanarProbe,
        FrameSettingsField.ReflectionProbe,
        FrameSettingsField.LensFlareDataDriven,
        FrameSettingsField.LowResTransparent,
        FrameSettingsField.Water,
        FrameSettingsField.CustomPass,
    ];

    public static void ApplyExtraCameraFrameSettings(HDAdditionalCameraData data)
    {
        data.customRenderingSettings = true;
        data.probeLayerMask = 0;

        ref var maskFrameSettings = ref data.renderingPathCustomFrameSettingsOverrideMask;
        ref var frameSettings = ref data.renderingPathCustomFrameSettings;

        foreach (var field in s_DisabledExtraCameraFields)
        {
            maskFrameSettings.mask[(uint)field] = true;
            frameSettings.SetEnabled(field, false);
        }
    }
}
