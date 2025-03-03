using System.Collections.Generic;
using LethalPerformance.Patcher;

namespace LethalPerformance.Utilities;
internal class ES3SaverTask
{
    private readonly HashSet<ES3File> m_ChangedSaves = new();

    public void ScheduleSaveFor(ES3File file)
    {
        m_ChangedSaves.Add(file);
    }

    public void SaveIfDirty()
    {
        if (m_ChangedSaves.Count == 0)
        {
            return;
        }

        foreach (var file in m_ChangedSaves)
        {
            if (!TryGetPath(file, out var path))
            {
                LethalPerformancePlugin.Instance.Logger.LogWarning($"Got unknown cached save");
                continue;
            }

            file.Sync(new ES3Settings(path)
            {
                encryptionType = ES3.EncryptionType.AES,
                _location = ES3.Location.File
            });
        }

        LethalPerformancePlugin.Instance.Logger.LogInfo($"Saved {m_ChangedSaves.Count} save(s)");

        m_ChangedSaves.Clear();
    }

    private static bool TryGetPath(ES3File file, out string? path)
    {
        foreach (var kv in ES3File.cachedFiles)
        {
            if (kv.Value == file)
            {
                path = kv.Key;
                return true;
            }
        }

        path = null;
        return false;
    }
}
