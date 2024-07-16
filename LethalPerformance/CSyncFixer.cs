#if ENABLE_PROFILER
using System;
using System.Reflection;
using HarmonyLib;
using LethalPerformance.API;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LethalPerformance;
internal static class CSyncFixer
{
    private static PropertyInfo? s_CSyncPrefab;
    private static PropertyInfo? s_ConfigInstanceKey;
    private static bool s_CSyncPatched;

    [InitializeOnAwake]
    public static void Initialize()
    {
        // fixes csync doesn't work well without a mod FixPluginTypesSerialization
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
    }

    private static void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (s_CSyncPatched || scene.name != "MainMenu")
        {
            return;
        }

        var configSyncBehaviourType = Type.GetType("CSync.Lib.ConfigSyncBehaviour,com.sigurd.csync");
        if (configSyncBehaviourType == null)
        {
            LethalPerformancePlugin.Instance.Logger.LogInfo("Failed to find CSync mod");
            return;
        }

        s_ConfigInstanceKey = configSyncBehaviourType
            .GetProperty("ConfigInstanceKey", AccessTools.all);

        s_CSyncPrefab = configSyncBehaviourType.Assembly
            .GetType("CSync.Lib.ConfigManager")
            .GetProperty("Prefab", AccessTools.all);

        var methodToPatch = configSyncBehaviourType
            .GetMethod("Awake", AccessTools.all);

        LethalPerformancePlugin.Instance.Harmony?.CreateProcessor(methodToPatch)
            .AddPrefix(SymbolExtensions.GetMethodInfo((NetworkBehaviour x) => FixSerializedKey(x)))
            .Patch();

        s_CSyncPatched = true;
    }

    public static void FixSerializedKey(NetworkBehaviour __instance)
    {
        var components = __instance.gameObject.GetComponents<Object>();

        var selfComponentIndex = Array.IndexOf(components, __instance);

        var prefabComponents = ((GameObject)s_CSyncPrefab!.GetValue(null)).GetComponents<Object>();

        var value = s_ConfigInstanceKey!.GetValue(prefabComponents[selfComponentIndex]);
        s_ConfigInstanceKey.SetValue(__instance, value);
    }
}
#endif