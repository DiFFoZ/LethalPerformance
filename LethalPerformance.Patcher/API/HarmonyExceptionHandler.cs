using System;

namespace LethalPerformance.Patcher.API;
internal static class HarmonyExceptionHandler
{
    public static Exception? ReportException(Exception? exception)
    {
        if (exception != null)
        {
            LethalPerformancePatcher.Logger.LogWarning(exception);
            // stacktrace needed to find class that fails to patch
            LethalPerformancePatcher.Logger.LogWarning(Environment.StackTrace);
        }

        return null;
    }
}
