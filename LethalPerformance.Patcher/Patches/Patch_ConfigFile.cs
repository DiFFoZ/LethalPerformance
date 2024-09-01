using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using LethalPerformance.Patcher.Helpers;

namespace LethalPerformance.Patcher.Patches;
[HarmonyPatch(typeof(ConfigFile))]
internal static class Patch_ConfigFile
{
    private static bool s_IgnoreConfigSet;

    [HarmonyPatch(nameof(ConfigFile.Reload))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> CallOptimizedReload()
    {
        return [new (OpCodes.Ldarg_0),
        new(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => OptimizedReload(null!))),
        new(OpCodes.Ret)];
    }

    [HarmonyPatch(nameof(ConfigFile.Save))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> CallOptimizedSave()
    {
        return [new (OpCodes.Ldarg_0),
        new(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => SaveOptimized(null!))),
        new(OpCodes.Ret)];
    }

    [HarmonyPatch(MethodType.Constructor, [typeof(string), typeof(bool), typeof(BepInPlugin)])]
    [HarmonyPostfix]
    public static void SetFalseForSaveOnConfigSet(ConfigFile __instance, bool saveOnInit)
    {
        s_IgnoreConfigSet = true;
        __instance.SaveOnConfigSet = false;
        s_IgnoreConfigSet = false;

        if (saveOnInit)
        {
            // Add save schedule if mod uses saveOnInit
            LethalPerformancePatcher.ConfigSaverTask.ScheduleSaveFor(__instance);
        }

        __instance.SettingChanged +=
            (_, arg) => LethalPerformancePatcher.ConfigSaverTask.ScheduleSaveFor(arg.ChangedSetting.ConfigFile);
    }

    [HarmonyPatch(nameof(ConfigFile.SaveOnConfigSet), MethodType.Setter)]
    [HarmonyPostfix]
    public static void AddIgnoreIfSetToFalse(ConfigFile __instance, bool value)
    {
        if (s_IgnoreConfigSet)
        {
            return;
        }

        if (value)
        {
            return;
        }

        LethalPerformancePatcher.ConfigSaverTask.AddIgnoredConfigFile(__instance);
    }

    private static void OptimizedReload(ConfigFile instance)
    {
        lock (instance._ioLock)
        {
            instance.OrphanedEntries.Clear();

            using var streamReader = new StreamReader(instance.ConfigFilePath, Encoding.UTF8);

            var section = string.Empty;
            string rawLine;
            Span<Range> ranges = stackalloc Range[2];
            while ((rawLine = streamReader.ReadLine()) is not null)
            {
                var line = rawLine.AsSpan().Trim();
                if (line.IsEmpty)
                {
                    continue;
                }

                if (line.StartsWith("#"))
                {
                    continue;
                }

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Slice(1, line.Length - 2).ToString();
                    continue;
                }

                if (!SpanExtensions.Split(line, ranges, '='))
                {
                    continue;
                }

                var key = line[ranges[0]].Trim();
                var value = line[ranges[1]].Trim();

                var configDefinition = new ConfigDefinition(section, key.ToString());
                if (instance.Entries.TryGetValue(configDefinition, out var configEntryBase))
                {
                    configEntryBase.SetSerializedValue(value.ToString());
                    continue;
                }

                instance.OrphanedEntries[configDefinition] = value.ToString();
            }
        }

        instance.OnConfigReloaded();
    }

    private static void SaveOptimized(ConfigFile instance)
    {
        lock (instance._ioLock)
        {
            var directoryName = Path.GetDirectoryName(instance.ConfigFilePath);
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            Span<char> buffer = stackalloc char[128];

            using var writer = new StreamWriter(instance.ConfigFilePath, false, Utility.UTF8NoBom);
            if (instance._ownerMetadata != null)
            {
                writer.Write("## Settings file was created by plugin ");
                writer.Write(instance._ownerMetadata.Name);
                writer.Write(" v");

                instance._ownerMetadata.Version.TryFormat(buffer, out var charsWritten);
                writer.WriteLine(buffer.Slice(0, charsWritten));

                writer.Write("## Plugin GUID: ");
                writer.WriteLine(instance._ownerMetadata.GUID);
                writer.WriteLine();
            }

            var orphanedEntries = instance.OrphanedEntries
                .Select(static x => new { x.Key, entry = (ConfigEntryBase)null!, value = x.Value });

            var entries = instance.Entries
                .Select(static x => new { x.Key, entry = x.Value, value = (string)null! });

            var allConfigEntries = entries.Concat(orphanedEntries)
                .GroupBy(static x => x.Key.Section)
                .OrderBy(static x => x.Key);

            foreach (var sectionKv in allConfigEntries)
            {
                // Section heading
                writer.Write('[');
                writer.Write(sectionKv.Key);
                writer.WriteLine(']');

                foreach (var configEntry in sectionKv)
                {
                    writer.WriteLine();

                    configEntry.entry?.WriteDescription(writer);

                    var value = configEntry.value ?? configEntry.entry?.GetSerializedValue() ?? string.Empty;

                    writer.Write(configEntry.Key.Key);
                    writer.Write(" = ");
                    writer.WriteLine(value);
                }

                writer.WriteLine();
            }
        }
    }
}
