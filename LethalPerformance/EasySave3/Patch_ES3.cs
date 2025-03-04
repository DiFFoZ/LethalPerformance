using HarmonyLib;

namespace LethalPerformance.EasySave3;

[HarmonyPatch(typeof(ES3))]
internal static class Patch_ES3
{
    [HarmonyPatch("LethalPerformance_Load")] // added by preloader
    [HarmonyPrefix]
    internal static void LoadPatch(ES3Settings settings)
    {
        ES3Utilities.ForceToCache(settings);
        ES3Utilities.LoadFileToCache(settings);

        //LethalPerformancePlugin.Instance.Logger.LogFatal("[ES3 Load] " + settings.path);
    }

    [HarmonyPatch("LethalPerformance_Save")] // added by preloader
    [HarmonyPrefix]
    internal static void SavePatch(ES3Settings settings)
    {
        ES3Utilities.ForceToCache(settings);
        ES3Utilities.LoadFileToCache(settings);

        //LethalPerformancePlugin.Instance.Logger.LogFatal("[ES3 Save] " + settings.path);
    }

    [HarmonyPatch(nameof(ES3.DeleteFile), [typeof(ES3Settings)])]
    [HarmonyPostfix]
    internal static void DeleteCachedFile(ES3Settings settings)
    {
        ES3File.cachedFiles.Remove(settings.path);
    }

    [HarmonyPatch(nameof(ES3.RenameFile), [typeof(ES3Settings), typeof(ES3Settings)])]
    [HarmonyPostfix]
    internal static void RenameCachedFile(ES3Settings oldSettings, ES3Settings newSettings)
    {
        if (ES3File.cachedFiles.Remove(oldSettings.path, out var file))
        {
            ES3File.cachedFiles[newSettings.path] = file;
        }
    }

    [HarmonyPatch(nameof(ES3.GetKeys), [typeof(ES3Settings)])]
    [HarmonyPatch(nameof(ES3.KeyExists), [typeof(string), typeof(ES3Settings)])]
    [HarmonyPatch(nameof(ES3.DeleteKey), [typeof(string), typeof(ES3Settings)])]
    [HarmonyPrefix]
    internal static void UseCache(ES3Settings settings)
    {
        ES3Utilities.ForceToCache(settings);
        ES3Utilities.LoadFileToCache(settings);
    }
}
