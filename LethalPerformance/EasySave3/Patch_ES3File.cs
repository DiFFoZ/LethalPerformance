using HarmonyLib;

namespace LethalPerformance.EasySave3;
[HarmonyPatch(typeof(ES3File))]
internal static class Patch_ES3File
{
    [HarmonyPatch(nameof(ES3File.KeyExists), [typeof(string), typeof(ES3Settings)])]
    [HarmonyPrefix]
    public static void KeyExists(ES3Settings settings)
    {
        Patch_ES3.LoadFileToCache(settings);
    }
}
