using System.Collections.Generic;
using LethalPerformance.Patcher;

namespace LethalPerformance.Utilities;
internal class ES3SaverTask
{
    private readonly HashSet<string> m_ChangedSaves = new();

    public void ScheduleSaveFor(string path)
    {
        m_ChangedSaves.Add(path);
    }

    public void SaveIfDirty()
    {
        if (m_ChangedSaves.Count == 0)
        {
            return;
        }

        foreach (var path in m_ChangedSaves)
        {
            if (!ES3File.cachedFiles.TryGetValue(path, out var file))
            {
                continue;
            }

            file.Sync(new ES3Settings(path)
            {
                encryptionType = ES3.EncryptionType.AES,
                _location = ES3.Location.File
            });
        }

        LethalPerformancePatcher.Logger.LogInfo($"Saved {m_ChangedSaves.Count} save(s)");

        m_ChangedSaves.Clear();
    }
}
