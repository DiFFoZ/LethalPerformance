using System;

namespace LethalPerformance.Patches;
internal static class HarmonyExceptionHandler
{
    public static Exception? ReportException(Exception? exception)
    {
        if (exception != null)
        {
            LethalPerformancePlugin.Instance.Logger.LogError(exception);
        }
        
        return null;
    }
}
