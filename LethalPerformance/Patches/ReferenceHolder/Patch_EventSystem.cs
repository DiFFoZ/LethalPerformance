using System;
using GameNetcodeStuff;
using HarmonyLib;
using LethalPerformance.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

namespace LethalPerformance.Patches.ReferenceHolder;
[HarmonyPatch(typeof(EventSystem))]
internal static class Patch_EventSystem
{
    [HarmonyPatch("OnEnable")]
    [HarmonyPrefix]
    public static void FindReferences(EventSystem __instance)
    {
        var scene = __instance.gameObject.scene;
        if (!scene.IsSceneShip())
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

    [HarmonyPatch("OnDisable")]
    [HarmonyPrefix]
    public static void CleanReferences(EventSystem __instance)
    {
        var scene = __instance.gameObject.scene;
        if (!scene.IsSceneShip())
        {
            return;
        }

        LethalPerformancePlugin.Instance.Logger.LogInfo("Cleanup references");

        try
        {
            UnsafeCacheManager.CleanupCache();
        }
        catch (Exception ex)
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning($"Failed to clean references.\n{ex}");
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
            if (go.TryGetComponent<GraphicRaycaster>(out var raycaster))
            {
                Object.Destroy(raycaster);
                LethalPerformancePlugin.Instance.Logger.LogInfo("Destroyed GraphicRaycaster of map screen");
            }

            if (go.TryGetComponent<CanvasScaler>(out var scaler))
            {
                Object.Destroy(scaler);
                LethalPerformancePlugin.Instance.Logger.LogInfo("Destroyed CanvasScaler of map screen");
            }
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

        ChangeUICameraSettings();
        RemoveAudioSpecializerPlugin();
    }

    private static void RemoveAudioSpecializerPlugin()
    {
        if (Dependencies.IsModLoaded(Dependencies.LethalLevelLoader) || Dependencies.IsModLoaded(Dependencies.LobbyControl))
        {
            return;
        }

        var audioSources = Resources.FindObjectsOfTypeAll<AudioSource>();
        foreach (var audioSource in audioSources)
        {
            audioSource.spatialize = false;
        }

        LethalPerformancePlugin.Instance.Logger.LogInfo($"Disabled spatialize for {audioSources.Length} audio sources");
    }

    private static void ChangeUICameraSettings()
    {
        if (Dependencies.IsModLoaded(Dependencies.LethalCompanyVR))
        {
            return;
        }

        var go = GameObject.Find("/Systems/UI/UICamera");
        if (go == null || !go.TryGetComponent<HDAdditionalCameraData>(out var data))
        {
            return;
        }

        ref var maskFrameSettings = ref data.renderingPathCustomFrameSettingsOverrideMask;
        ref var frameSettings = ref data.renderingPathCustomFrameSettings;

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
