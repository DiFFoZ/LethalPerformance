using System;

namespace LethalPerformance.Patches;
internal static class HarmonyExceptionHandler
{
    public static Exception? ReportException(Exception? exception)
    {
        LethalPerformancePlugin.Instance.Logger.LogError(exception);
        return null;
    }
}
