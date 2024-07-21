using System;
using GameNetcodeStuff;
using HarmonyLib;
using LethalPerformance.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LethalPerformance.Patches.ReferenceHolder;
[HarmonyPatch(typeof(SceneManager))]
internal static class Patch_SceneManager
{
    [HarmonyPatch(nameof(SceneManager.Internal_ActiveSceneChanged))]
    [HarmonyPrefix]
    public static void FindReferences(Scene previousActiveScene, Scene newActiveScene)
    {
        if (previousActiveScene.IsValid() && previousActiveScene.IsSceneShip())
        {
            UnsafeCacheManager.CleanupCache();
            return;
        }

        if (!newActiveScene.IsValid() || !newActiveScene.IsSceneShip())
        {
            return;
        }

        try
        {
            UnsafeCacheManager.CacheInstances();
            DeleteUnusedStuff();
        }
        catch (Exception ex)
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning($"Failed to get references. Probably other mod destroyed object.\n{ex}");
        }
    }

    private static void DeleteUnusedStuff()
    {
        var go = GameObject.Find("/PlayersContainer");
        if (go == null)
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning("Failed to find Player container");
            return;
        }

        using var _ = ListPool<PlayerControllerB>.Get(out var players);
        go.GetComponentsInChildren(players);

        foreach (var player in players)
        {
            if (player.usernameCanvas.TryGetComponent<CanvasScaler>(out var scaler))
            {
                Object.Destroy(scaler);
                LethalPerformancePlugin.Instance.Logger.LogInfo("Destroyed Username CanvasScaler");
            }

            if (player.usernameCanvas.TryGetComponent<GraphicRaycaster>(out var raycaster))
            {
                Object.Destroy(raycaster);
                LethalPerformancePlugin.Instance.Logger.LogInfo("Destroyed Username GraphicRaycaster");
            }
        }

        go = GameObject.Find("/Systems/GameSystems/ItemSystems/MapScreenUIWorldSpace");
        if (go != null)
        {
            Object.Destroy(go);
            LethalPerformancePlugin.Instance.Logger.LogInfo("Destroyed MapScreenUIWorldSpace");
        }

        go = GameObject.Find("/Environment/HangarShip/ShipModels2b/MonitorWall/Cube/Canvas (1)");
        if (go != null)
        {
            if (go.TryGetComponent<GraphicRaycaster>(out var raycaster))
            {
                Object.Destroy(raycaster);
                LethalPerformancePlugin.Instance.Logger.LogInfo("Destroyed GraphicRaycaster of quota monitor");
            }

            if (go.TryGetComponent<CanvasScaler>(out var scaler))
            {
                Object.Destroy(scaler);
                LethalPerformancePlugin.Instance.Logger.LogInfo("Destroyed CanvasScaler of quota monitor");
            }
        }

        var settings = HDRenderPipelineGlobalSettings.instance;
        ref var frameSettings = ref settings.GetDefaultFrameSettings(FrameSettingsRenderType.Camera);
        frameSettings.bitDatas[(uint)FrameSettingsField.StopNaN] = false;
        frameSettings.bitDatas[(uint)FrameSettingsField.DepthPrepassWithDeferredRendering] = true;
        frameSettings.bitDatas[(uint)FrameSettingsField.ClearGBuffers] = true;
        LethalPerformancePlugin.Instance.Logger.LogInfo("Disabled StopNan and enabled DepthPrepassWithDeferredRendering globally");

        go = GameObject.Find("/Systems/UI/UICamera");
        if (go != null && go.TryGetComponent<HDAdditionalCameraData>(out var data))
        {
            ref var maskFrameSettings = ref data.renderingPathCustomFrameSettingsOverrideMask;
            frameSettings = ref data.renderingPathCustomFrameSettings;

            maskFrameSettings.mask[(uint)FrameSettingsField.LitShaderMode] = true;
            frameSettings.litShaderMode = LitShaderMode.Forward;

            maskFrameSettings.mask[(uint)FrameSettingsField.OpaqueObjects] = true;
            frameSettings.bitDatas[(uint)FrameSettingsField.OpaqueObjects] = false;

            maskFrameSettings.mask[(uint)FrameSettingsField.ProbeVolume] = true;
            frameSettings.bitDatas[(uint)FrameSettingsField.ProbeVolume] = false;

            maskFrameSettings.mask[(uint)FrameSettingsField.VolumetricClouds] = true;
            frameSettings.bitDatas[(uint)FrameSettingsField.VolumetricClouds] = false;

            data.probeLayerMask = 0;
        }
    }
}
