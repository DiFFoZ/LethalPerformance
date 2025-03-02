using System.Collections.Generic;
using System.Linq;
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
                encryptionType = GetEncryptionType(path)
            });
        }

        LethalPerformancePatcher.Logger.LogInfo($"Saved {m_ChangedSaves.Count} save(s)");

        m_ChangedSaves.Clear();
    }

    private static ES3.EncryptionType GetEncryptionType(string path)
    {
        // only forcing no encryption for saves
        // todo:
        // - add no encryption for generic save?
        // - compact with other mods (CodeRebirth, TooManyEmotes, BetterEXP)
        // - no encryption for "LCChallengeFile"?

        return ES3.EncryptionType.None;

        return path.StartsWith("LCSaveFile") ? ES3.EncryptionType.None : ES3.EncryptionType.AES;
    }
}
