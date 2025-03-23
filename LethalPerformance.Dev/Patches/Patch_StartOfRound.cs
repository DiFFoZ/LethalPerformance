using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using HarmonyLib;
using LethalPerformance.Patcher.API;
using Unity.Netcode;

namespace LethalPerformance.Dev.Patches;
[HarmonyPatch(typeof(StartOfRound))]
internal static class Patch_StartOfRound
{
    [InitializeOnAwake]
    public static void ListenForConfigChanges()
    {
        LethalPerformanceDevPlugin.Instance.Config.OverriddenSeed.SettingChanged += (_, _) => OverrideSeed();
    }

    [HarmonyPatch(nameof(StartOfRound.Start))]
    [HarmonyPostfix]
    public static void OverrideSeed()
    {
        ChangeWindowTitle();

        var newSeed = LethalPerformanceDevPlugin.Instance.Config.OverriddenSeed.Value;
        if (newSeed <= 0)
        {
            StartOfRound.Instance.overrideRandomSeed = false;
            return;
        }

        StartOfRound.Instance.overrideRandomSeed = true;
        StartOfRound.Instance.overrideSeedNumber = newSeed;
    }

    [HarmonyPatch(nameof(StartOfRound.SetPlanetsWeather))]
    [HarmonyPrefix]
    public static bool SetPlanetWeather(StartOfRound __instance)
    {
        var weather = LethalPerformanceDevPlugin.Instance.Config.OverriddenWeather.Value;
        if (weather is LevelWeatherType.None)
        {
            return true;
        }

        foreach (var level in __instance.levels)
        {
            level.currentWeather = weather;
        }

        return false;
    }

    private static void ChangeWindowTitle()
    {
        var handle = Process.GetCurrentProcess().MainWindowHandle;
        if (handle == IntPtr.Zero)
        {
            Console.WriteLine("fail handle");
            return;
        }

        var isServer = NetworkManager.Singleton.IsServer;
        var id = NetworkManager.Singleton.LocalClientId;

        WindowAPI.SetWindowText(handle, $"Lethal Company - {(isServer ? "Server" : "Client")} #{id}");
        WindowAPI.SetConsoleTitle($"Lethal Company - {(isServer ? "Server" : "Client")} #{id}");
    }

    [HarmonyPatch(nameof(StartOfRound.PlayFirstDayShipAnimation))]
    [HarmonyPrefix]
    public static bool DisableSpeaker()
    {
        return false;
    }

    private static class WindowAPI
    {
        [DllImport("User32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowText(IntPtr hWnd, string text);

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetConsoleTitle(string title);
    }
}
