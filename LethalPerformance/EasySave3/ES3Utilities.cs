using System;
using System.IO;
using ES3Internal;

namespace LethalPerformance.EasySave3;
internal static class ES3Utilities
{
    public static void ForceToCache(ES3Settings settings)
    {
        if (settings._location is ES3.Location.File or ES3.Location.Cache)
        {
            settings._location = ES3.Location.Cache;
        }
    }

    public static void LoadFileToCache(ES3Settings settings)
    {
        if (settings.encryptionType is not ES3.EncryptionType.None)
        {
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
        using var stream = File.OpenRead(path);

        ReadOnlySpan<byte> validChars = "{\""u8;

        Span<byte> buffer = stackalloc byte[2];
        var readCount = stream.Read(buffer);

        // if stream is empty or doesn't start with: {"
        // then we think it's encrypted
        return readCount != 2 || !buffer.SequenceEqual(validChars);
    }
}
