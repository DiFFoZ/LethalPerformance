using HarmonyLib;
using UnityEngine;

namespace LethalPerformance.Patches;
[HarmonyPatch(typeof(TimeOfDay))]
[HarmonyPriority(Priority.Last)]
internal static class Patch_TimeOfDay
{
    [HarmonyPatch(nameof(TimeOfDay.Start))]
    [HarmonyPostfix]
    public static void FixParticleSubEmitters(TimeOfDay __instance)
    {
        // Fixes spamming Unity log
        // Sub-emitters may not use stop actions. The Stop action will not be executed.

        foreach (var weatherEffect in __instance.effects)
        {
            if (weatherEffect == null || weatherEffect.effectObject == null)
            {
                continue;
            }

            var particleSystem = weatherEffect.effectObject.GetComponentInChildren<ParticleSystem>();
            if (particleSystem == null)
            {
                continue;
            }

            var subEmittersModule = particleSystem.subEmitters;
            if (!subEmittersModule.enabled)
            {
                continue;
            }

            var subEmittersCount = subEmittersModule.subEmittersCount;
            for (var i = 0; i < subEmittersCount; i++)
            {
                var subEmitter = subEmittersModule.GetSubEmitterSystem(i);
                var mainModule = subEmitter.main;
                mainModule.stopAction = ParticleSystemStopAction.None;
            }
        }
    }
}
