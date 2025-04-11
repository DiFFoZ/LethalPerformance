using HarmonyLib;
using LethalPerformance.Utilities;
using UnityEngine;
using UnityEngine.Pool;

namespace LethalPerformance.Validation;
[HarmonyPatch(typeof(StartOfRound))]
internal static class EntranceTeleportValidation
{
    [HarmonyPatch(nameof(StartOfRound.OnShipLandedMiscEvents))]
    [HarmonyPostfix]
    private static void ShowWarningTip()
    {
        var scene = SceneUtilities.GetLastLoadedScene();
        if (!scene.IsValid())
        {
            return;
        }

        using var _ = ListPool<GameObject>.Get(out var roots);
        using var __ = ListPool<EntranceTeleport>.Get(out var entrances);
        scene.GetRootGameObjects(roots);

        var hasBrokenEntrance = false;

        foreach (var obj in roots)
        {
            entrances.Clear();
            obj.GetComponentsInChildren(entrances);
            foreach (var entrance in entrances)
            {
                if (!entrance.IsSpawned)
                {
                    hasBrokenEntrance = true;
                    goto exit;
                }
            }
        }

        exit:
        if (!hasBrokenEntrance)
        {
            return;
        }

        HUDManager.Instance.DisplayTip("Lethal Performance",
            "Entrance teleport mismatch. Expect 'The entrance appears to be blocked'.", true, false, "LP_ETBAD");
    }
}
