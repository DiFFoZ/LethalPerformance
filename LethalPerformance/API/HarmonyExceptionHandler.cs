﻿using System;

namespace LethalPerformance.API;
internal static class HarmonyExceptionHandler
{
    public static Exception? ReportException(Exception? exception)
    {
        if (exception != null)
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning(exception);
        }

        return null;
    }
}
