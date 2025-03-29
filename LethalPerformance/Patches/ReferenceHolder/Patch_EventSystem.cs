using System;
using GameNetcodeStuff;
using HarmonyLib;
using LethalPerformance.Caching;
using LethalPerformance.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

namespace LethalPerformance.Patches.ReferenceHolder;
/// <summary>
/// Handles caching of ship scene
/// </summary>
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
            }

            if (player.usernameCanvas.TryGetComponent<GraphicRaycaster>(out var raycaster))
            {
                Object.Destroy(raycaster);
            }
        }

        go = GameObject.Find("/Systems/GameSystems/ItemSystems/MapScreenUIWorldSpace");
        if (go != null)
        {
            if (go.TryGetComponent<GraphicRaycaster>(out var raycaster))
            {
                Object.Destroy(raycaster);
            }

            if (go.TryGetComponent<CanvasScaler>(out var scaler))
            {
                Object.Destroy(scaler);
            }
        }

        go = GameObject.Find("/Environment/HangarShip/ShipModels2b/MonitorWall/Cube/Canvas (1)");
        if (go != null)
        {
            if (go.TryGetComponent<GraphicRaycaster>(out var raycaster))
            {
                Object.Destroy(raycaster);
            }

            if (go.TryGetComponent<CanvasScaler>(out var scaler))
            {
                Object.Destroy(scaler);
            }
        }

        ChangeUICameraSettings();
        RemoveAudioSpecializerPlugin();
        DisableCameraCleanup();
    }

    private static readonly string[] s_CameraPaths =
    [
        "/Systems/GameSystems/ItemSystems/MapCamera",
        "/Environment/HangarShip/Cameras/FrontDoorSecurityCam/SecurityCamera",
        "/Environment/HangarShip/Cameras/ShipCamera",
    ];

    private static void DisableCameraCleanup()
    {
        foreach (var cameraPath in s_CameraPaths)
        {
            var go = GameObject.Find(cameraPath);
            if (go == null || !go.TryGetComponent<HDAdditionalCameraData>(out var data))
            {
                continue;
            }

            data.hasPersistentHistory = true;
        }
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

        maskFrameSettings.mask[(uint)FrameSettingsField.ProbeVolume] = true;
        frameSettings.bitDatas[(uint)FrameSettingsField.ProbeVolume] = false;

        maskFrameSettings.mask[(uint)FrameSettingsField.VolumetricClouds] = true;
        frameSettings.bitDatas[(uint)FrameSettingsField.VolumetricClouds] = false;

        data.probeLayerMask = 0;
    }
}
