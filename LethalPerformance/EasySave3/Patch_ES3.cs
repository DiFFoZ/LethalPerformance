using System;
using System.IO;
using System.Threading;
using ES3Internal;
using HarmonyLib;
using LethalPerformance.Patcher.API;

namespace LethalPerformance.EasySave3;

[HarmonyPatch(typeof(ES3))]
internal static class Patch_ES3
{
    [InitializeOnAwake]
    private static void SetDefaultLocationToCache()
    {
        var defaultSettings = ES3Settings.defaultSettings;
        defaultSettings.location = ES3.Location.Cache;
        defaultSettings.encryptionType = ES3.EncryptionType.None;

        ES3.Init();
    }

    [HarmonyPatch("LethalPerformance_Load")] // added by preloader
    [HarmonyPrefix]
    private static void LoadPatch(ES3Settings settings)
    {
        CheckAndForceCache(settings);
        LoadFileToCache(settings);

        LethalPerformancePlugin.Instance.Logger.LogFatal("[ES3 Load] " + settings.path);
    }

    [HarmonyPatch("LethalPerformance_Save")] // added by preloader
    [HarmonyPrefix]
    private static void SavePatch(ES3Settings settings)
    {
        CheckAndForceCache(settings);
        LoadFileToCache(settings);

        LethalPerformancePlugin.Instance.ES3SaverTask.ScheduleSaveFor(settings.path);

        LethalPerformancePlugin.Instance.Logger.LogFatal("[ES3 Save] " + settings.path);
    }

    [HarmonyPatch(nameof(ES3.DeleteFile), [typeof(ES3Settings)])]
    [HarmonyPostfix]
    private static void DeleteCachedFile(ES3Settings settings)
    {
        if (!ES3File.cachedFiles.ContainsKey(settings.path))
        {
            ES3.DeleteFile(new ES3Settings(settings.path)
            {
                _location = ES3.Location.File
            });
        }
    }

    [HarmonyPatch(nameof(ES3.FileExists), [typeof(ES3Settings)])]
    [HarmonyPrefix]
    private static bool FileExists(ES3Settings settings, ref bool __result)
    {
        LoadFileToCache(settings);

        if (settings._location is ES3.Location.Cache
            && ES3File.cachedFiles.TryGetValue(settings.path, out var file)
            && file.cache.Count == 0)
        {
            // file got loaded into the cache, but contains no data.
            // Setting result as file not exists
            __result = false;
            return false;
        }

        return true;
    }

    private static void CheckAndForceCache(ES3Settings settings)
    {
        if (settings._location is not ES3.Location.Cache)
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning($"Expecting save location to be in Cache, but got {settings._location} for {settings.path}");
            settings._location = ES3.Location.Cache;
        }
    }

    internal static void LoadFileToCache(ES3Settings settings)
    {
        if (settings.encryptionType is not ES3.EncryptionType.None)
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning($"Expecting encryption to be in None, but got AES for {settings.path}");
            settings.encryptionType = ES3.EncryptionType.None;
        }

        if (ES3File.cachedFiles.ContainsKey(settings.path))
        {
            return;
        }

        var saveSettings = (ES3Settings)settings.Clone();
        saveSettings._location = ES3.Location.File;

        var fullPath = ES3IO.persistentDataPath + "/" + settings.path;
        if (!File.Exists(fullPath))
        {
            var es3File = new ES3File(saveSettings, syncWithFile: false);
            es3File.syncWithFile = true;

            ES3File.cachedFiles[settings.path] = es3File;
            return;
        }

        var readSettings = (ES3Settings)settings.Clone();
        readSettings._location = ES3.Location.File;
        if (CheckFileEncrypted(fullPath))
        {
            // encrypted, using AES
            readSettings.encryptionType = ES3.EncryptionType.AES;
        }

        ES3File.cachedFiles[settings.path] = new ES3File(ES3.LoadRawBytes(readSettings), saveSettings);
    }

    private static bool CheckFileEncrypted(string path)
    {
        // check known path, for the reason check ES3SaverTask file
        if (path.StartsWith("LCGeneralSaveData"))
        {
            return true;
        }

        using var stream = File.OpenRead(path);

        ReadOnlySpan<byte> validChars = [(byte)'{', (byte)'"'];

        Span<byte> buffer = stackalloc byte[2];
        var readCount = stream.Read(buffer);

        // if stream is empty or doesn't start with: {"
        // then we think it's encrypted
        return readCount != 2 || !buffer.SequenceEqual(validChars);
    }
}
