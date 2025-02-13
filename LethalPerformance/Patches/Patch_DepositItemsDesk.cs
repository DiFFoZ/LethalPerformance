using HarmonyLib;
using LethalPerformance.Caching;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(DepositItemsDesk))]
internal static class Patch_DepositItemsDesk
{
    private static readonly UnsafeCachedInstance<DepositItemsDesk> s_CompanyDepositInstance
        = UnsafeCacheManager.AddReferenceToMap(new ManualCachedInstance<DepositItemsDesk>());

    // to initialize .cctor
    [HarmonyPrepare]
    public static void Prepare() { }

    [HarmonyPatch("Awake")] // added by preloader
    [HarmonyPrefix]
    public static void Awake(DepositItemsDesk __instance)
    {
        s_CompanyDepositInstance.SetInstance(__instance);
    }
}
