using System;

namespace LethalPerformance.Patcher.API;
internal static class HarmonyExceptionHandler
{
    public static Exception? ReportException(Exception? exception)
    {
        if (exception != null)
        {
            LethalPerformancePatcher.Logger.LogWarning(exception);
        }

        return null;
    }
}
