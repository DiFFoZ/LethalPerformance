using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using LethalPerformance.Patcher.Helpers;

namespace LethalPerformance.Patcher.Patches;
[HarmonyPatch(typeof(ConfigEntryBase))]
internal static class Patch_ConfigEntryBase
{
    [HarmonyPatch(nameof(ConfigEntryBase.WriteDescription))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> CallWriteDescriptionOptimized()
    {
        return [new(OpCodes.Ldarg_0),
        new(OpCodes.Ldarg_1),
        new(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => WriteDescriptionOptimized(null!, null!))),
        new(OpCodes.Ret)];
    }

    public static void WriteDescriptionOptimized(ConfigEntryBase instance, StreamWriter writer)
    {
        if (!string.IsNullOrEmpty(instance.Description.Description))
        {
            writer.Write("## ");

            foreach (var chr in instance.Description.Description)
            {
                writer.Write(chr);

                if (chr == '\n')
                {
                    writer.Write("## ");
                }
            }
            writer.WriteLine();
        }

        writer.Write("# Setting type: ");
        writer.WriteLine(instance.SettingType.Name);

        writer.Write("# Default value: ");
        writer.WriteLine(TomlTypeConverter.ConvertToString(instance.DefaultValue, instance.SettingType));

        if (instance.Description.AcceptableValues != null)
        {
            writer.WriteLine(instance.Description.AcceptableValues.ToDescriptionString());
            return;
        }

        if (!instance.SettingType.IsEnum)
        {
            return;
        }

        writer.Write("# Acceptable values: ");

        var array = EnumCacheHelper.GetOrCreate(instance.SettingType);
        for (var i = 0; i < array.Length; i++)
        {
            var value = array[i];
            writer.Write(value);

            if (i != array.Length - 1)
            {
                writer.Write(", ");
            }
        }
        writer.WriteLine();

        if (instance.SettingType.IsDefined(typeof(FlagsAttribute), false))
        {
            writer.WriteLine("# Multiple values can be set at the same time by separating them with , (e.g. Debug, Warning)");
        }
    }
}
