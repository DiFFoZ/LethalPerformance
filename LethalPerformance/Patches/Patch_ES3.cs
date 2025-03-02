using HarmonyLib;
using LethalPerformance.Patcher.API;

namespace LethalPerformance.Patches
{
    [HarmonyPatch(typeof(ES3))]
    internal static class Patch_ES3
    {
        [InitializeOnAwake]
        private static void SetDefaultLocationToCache()
        {
            var defaultSettings = ES3Settings.defaultSettings;
            defaultSettings.location = ES3.Location.Cache;
            defaultSettings.encryptionType = ES3.EncryptionType.None;
        }

        [HarmonyPatch("LethalPerformance_Load")] // added by preloader
        [HarmonyPrefix]
        public static void LoadPatch(ES3Settings settings)
        {
            CheckAndForceCache(settings);

            LethalPerformancePlugin.Instance.Logger.LogFatal("[ES3 Load] " + settings.path);
        }

        [HarmonyPatch("LethalPerformance_Save")] // added by preloader
        [HarmonyPrefix]
        public static void SavePatch(ES3Settings settings)
        {
            CheckAndForceCache(settings);

            LethalPerformancePlugin.Instance.ES3SaverTask.ScheduleSaveFor(settings.path);

            LethalPerformancePlugin.Instance.Logger.LogFatal("[ES3 Save] " + settings.path);
        }

        [HarmonyPatch(nameof(ES3.DeleteFile), [typeof(ES3Settings)])]
        [HarmonyPostfix]
        public static void DeleteCachedFile(ES3Settings settings)
        {
            ES3File.RemoveCachedFile(settings);
        }

        private static void CheckAndForceCache(ES3Settings settings)
        {
            if (settings._location is not ES3.Location.Cache)
            {
                LethalPerformancePlugin.Instance.Logger.LogWarning($"Expecting save location to be in Cache, but got {settings._location} for {settings.path}");
                settings._location = ES3.Location.Cache;
            }
        }
    }
}
