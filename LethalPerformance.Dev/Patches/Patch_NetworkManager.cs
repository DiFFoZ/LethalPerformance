using HarmonyLib;
using Unity.Netcode;

namespace LethalPerformance.Dev.Patches;
[HarmonyPatch(typeof(NetworkManager))]
internal static class Patch_NetworkManager
{
    [HarmonyPatch(nameof(NetworkManager.SetSingleton))]
    [HarmonyPostfix]
    public static void SetTimeout()
    {
        // 2 minutes will be fine with a lot of LLL extended mods, right??
        NetworkManager.Singleton.NetworkConfig.LoadSceneTimeOut = 180;
    }
}
