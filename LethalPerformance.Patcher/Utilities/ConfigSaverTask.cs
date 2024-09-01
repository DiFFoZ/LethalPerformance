using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BepInEx.Configuration;
using LethalPerformance.Patcher.Helpers;

namespace LethalPerformance.Patcher.Utilities;
public class ConfigSaverTask
{
    private readonly HashSet<ConfigFile> m_ConfigFilesToSave = new();
    private readonly HashSet<ConfigFile> m_IgnoredConfigFiles = new();

    public void ScheduleSaveFor(ConfigFile configFile)
    {
        if (m_IgnoredConfigFiles.Contains(configFile))
        {
            return;
        }

        m_ConfigFilesToSave.Add(configFile);
    }

    public void AddIgnoredConfigFile(ConfigFile configFile)
    {
        if (!m_IgnoredConfigFiles.Add(configFile))
        {
            return;
        }

        foreach (var ignoredFile in m_IgnoredConfigFiles)
        {
            m_ConfigFilesToSave.Remove(ignoredFile);
        }
    }

    public void ScheduleSave()
    {
        if (m_ConfigFilesToSave.Count == 0)
        {
            return;
        }

        var queue = new Queue<ConfigFile>(m_ConfigFilesToSave);
        m_ConfigFilesToSave.Clear();

        AsyncHelper.Schedule(SaveAsync, queue);
    }

    public void Save()
    {
        if (m_ConfigFilesToSave.Count == 0)
        {
            return;
        }

        var queue = new Queue<ConfigFile>(m_ConfigFilesToSave);
        m_ConfigFilesToSave.Clear();
        
        SaveAsync(queue);
    }

    private Task SaveAsync(Queue<ConfigFile> queue)
    {
        var count = queue.Count;
        while (queue.TryDequeue(out var configFile))
        {
            configFile.Save();
        }

        LethalPerformancePatcher.Logger.LogInfo($"Saved {count} config(s)");

        return Task.CompletedTask;
    }
}
