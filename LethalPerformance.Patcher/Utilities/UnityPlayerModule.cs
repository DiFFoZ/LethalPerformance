using System;
using System.Diagnostics;
using System.Linq;

namespace LethalPerformance.Patcher.Utilities;

internal static class UnityPlayerModule
{
    internal static IntPtr BaseAddress { get; private set; }

    internal static bool IsDebugBuild { get; private set; }

    internal static bool TryInitialize()
    {
        if (BaseAddress != IntPtr.Zero)
        {
            return true;
        }

        IsDebugBuild = UnityEngine.Debug.isDebugBuild;

        var module = Process.GetCurrentProcess().Modules
            .Cast<ProcessModule>()
            .FirstOrDefault(p => p.ModuleName.Contains("UnityPlayer"));

        if (module == null)
        {
            return false;
        }

        BaseAddress = module.BaseAddress;
        return BaseAddress != IntPtr.Zero;
    }

    internal static IntPtr GetRva(int rva)
    {
        return BaseAddress + rva;
    }
}
