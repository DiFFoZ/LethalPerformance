using Dissonance;
using Dissonance.Audio.Capture;
using HarmonyLib;
using LethalPerformance.Utilities;

namespace LethalPerformance.Dissonance;
[HarmonyPatch(typeof(DissonanceComms))]
internal static class Patch_DissonanceComms
{
    [HarmonyPatch(nameof(DissonanceComms.Start))]
    [HarmonyPrefix]
    public static void AddSteamMicrophoneCapture(DissonanceComms __instance)
    {
        var scene = __instance.gameObject.scene;
        if (!scene.IsSceneShip())
        {
            return;
        }

        if (GameNetworkManager.Instance.disableSteam)
        {
            return;
        }

        if (!LethalPerformancePlugin.Instance.Configuration.UseSteamVoiceAPI.Value)
        {
            return;
        }

        if (__instance.TryGetComponent(typeof(IMicrophoneCapture), out var component))
        {
            // destroy BasicMicrophoneCapture (it maybe doesn't even attached to the Gameobject, but just in case we will remove it)
            Object.Destroy(component);
        }

        __instance.gameObject.AddComponent<SteamMicrophoneCapture>();
    }
}
