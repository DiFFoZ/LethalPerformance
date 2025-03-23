using System;
using GameNetcodeStuff;
using LethalPerformance.Caching;
using LethalPerformance.Patcher.API;
using LethalPerformance.Utilities;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

namespace LethalPerformance.Patches.ReferenceHolder;
internal static class MoonReferences
{
    [InitializeOnAwake]
    internal static void Initialize()
    {
        LightProbes.lightProbesUpdated += UpdateReferences;
    }

    private static void UpdateReferences()
    {
        var scene = SceneUtilities.GetLastLoadedScene();
        if (scene.IsSceneShip())
        {
            CacheShip();
        }
    }

    private static void CacheShip()
    {
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
