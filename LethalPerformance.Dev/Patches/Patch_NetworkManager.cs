using HarmonyLib;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace LethalPerformance.Dev.Patches;
[HarmonyPatch(typeof(NetworkManager))]
internal static class Patch_NetworkManager
{
    [HarmonyPatch(nameof(NetworkManager.SetSingleton))]
    [HarmonyPostfix]
    public static void SetTimeout()
    {
        // 2 minutes will be fine with a lot of LLL extended mods, right??
        NetworkManager.Singleton.NetworkConfig.LoadSceneTimeOut = 120;

        if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is UnityTransport transport)
        {
            transport.ConnectTimeoutMS = 120 * 1000;
            transport.DisconnectTimeoutMS = 120 * 1000;
            transport.HeartbeatTimeoutMS = 120 * 1000;
        }
    }
}
