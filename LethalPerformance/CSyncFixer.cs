#if ENABLE_PROFILER || DEBUG
using System;
using System.Reflection;
using HarmonyLib;
using LethalPerformance.API;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LethalPerformance;
[HarmonyPatch]
internal static class CSyncFixer
{
    private static PropertyInfo? s_CSyncPrefab;
    private static PropertyInfo? s_ConfigInstanceKey;
    private static MethodInfo? s_MethodToPatch;

    [InitializeOnAwake]
    public static void Initialize()
    {
        // fixes csync doesn't work well without a mod FixPluginTypesSerialization
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
    }

    private static void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        SceneManager.sceneLoaded -= SceneManager_sceneLoaded;

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

        s_MethodToPatch = configSyncBehaviourType
            .GetMethod("Awake", AccessTools.all);

        PatchCSync();
    }

    public static void PatchCSync()
    {
        if (s_MethodToPatch == null)
        {
            return;
        }

        LethalPerformancePlugin.Instance.Harmony?.PatchAll(typeof(CSyncFixer));
    }

    [HarmonyPrepare]
    public static bool ShouldPatch()
    {
        return s_MethodToPatch != null;
    }

    [HarmonyTargetMethod]
    public static MethodInfo? GetTargetPatch()
    {
        return s_MethodToPatch!;
    }

    [HarmonyPrefix]
    private static void FixSerializedKey(NetworkBehaviour __instance)
    {
        var components = __instance.gameObject.GetComponents<Object>();

        var selfComponentIndex = Array.IndexOf(components, __instance);

        var prefabComponents = ((GameObject)s_CSyncPrefab!.GetValue(null)).GetComponents<Object>();

        var value = s_ConfigInstanceKey!.GetValue(prefabComponents[selfComponentIndex]);
        s_ConfigInstanceKey.SetValue(__instance, value);
    }
}
#endif