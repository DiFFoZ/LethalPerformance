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
                encryptionType = GetEncryptionType(path),
                _location = ES3.Location.File
            });
        }

        LethalPerformancePatcher.Logger.LogInfo($"Saved {m_ChangedSaves.Count} save(s)");

        m_ChangedSaves.Clear();
    }

    private static ES3.EncryptionType GetEncryptionType(string path)
    {
        // forcing AES only for LCGeneralSaveData(.moddata)
        // the only main reason is to not reset generic save, because LethalPerformance is not loaded

        return path.StartsWith("LCGeneralSaveData") ? ES3.EncryptionType.AES : ES3.EncryptionType.None;
    }
}
