using HarmonyLib;

namespace LethalPerformance.EasySave3;
[HarmonyPatch(typeof(ES3File))]
internal static class Patch_ES3File
{
    [HarmonyPatch(nameof(ES3File.KeyExists), [typeof(string), typeof(ES3Settings)])]
    [HarmonyPrefix]
    public static void KeyExists(ES3Settings settings)
    {
        ES3Utilities.ForceToCache(settings);
        ES3Utilities.LoadFileToCache(settings);
    }

    [HarmonyPatch("LethalPerformance_Save")] // added by preloader
    [HarmonyPrefix]
    public static void ScheduleSave(ES3File __instance)
    {
        LethalPerformancePlugin.Instance.ES3SaverTask.ScheduleSaveFor(__instance);
    }
}
