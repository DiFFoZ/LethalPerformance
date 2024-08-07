using System;
using System.Collections.Generic;

namespace LethalPerformance.Patcher.Helpers;
internal static class EnumCacheHelper
{
    private static readonly Dictionary<Type, string[]> s_EnumNamesCache = new();

    public static string[] GetOrCreate(Type enumType)
    {
        if (s_EnumNamesCache.Count >= 255)
        {
            // to much values stored, clearing
            s_EnumNamesCache.Clear();
        }

        if (s_EnumNamesCache.TryGetValue(enumType, out var result))
        {
            return result;
        }

        var names = Enum.GetNames(enumType);
        s_EnumNamesCache[enumType] = names;

        return names;
    }
}
