using System;
using System.Collections.Generic;
using EasyTextEffects;
using HarmonyLib;
using LethalPerformance.Patcher.API;
using TMPro;

namespace LethalPerformance.Patches;

[HarmonyPatch(typeof(TextEffect))]
internal static class Patch_TextEffect
{
    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    // Replaced to remove LINQ usages to achieve no GC allocs
    [HarmonyPrefix]
    [HarmonyPatch(nameof(TextEffect.Update))]
    public static bool Update(TextEffect __instance)
    {
        var text = __instance.text;
        if (!text)
        {
            return false;
        }

        var time = TimeUtil.GetTime();
        if (time < __instance.nextUpdateTime_)
        {
            return false;
        }

        __instance.nextUpdateTime_ = time + 1f / __instance.updatesPerSecond;
        text.ForceMeshUpdate();

        var textInfo = text.textInfo;
        var onStartEffects = __instance.onStartEffects_;
        var manualEffects = __instance.manualEffects_;
        var onStartTagEffects = __instance.onStartTagEffects_;
        var manualTagEffects = __instance.manualTagEffects_;

        for (var i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible)
            {
                continue;
            }

            ApplyGlobalEffects(onStartEffects, textInfo, i, overrideTagEffects: false);
            ApplyGlobalEffects(manualEffects, textInfo, i, overrideTagEffects: false);
            ApplyTagEffects(onStartTagEffects, textInfo, i);
            ApplyTagEffects(manualTagEffects, textInfo, i);
            ApplyGlobalEffects(onStartEffects, textInfo, i, overrideTagEffects: true);
            ApplyGlobalEffects(manualEffects, textInfo, i, overrideTagEffects: true);
        }

        var meshInfo = textInfo.meshInfo;
        for (var j = 0; j < meshInfo.Length; j++)
        {
            var info = meshInfo[j];
            info.mesh.colors32 = info.colors32;
            info.mesh.vertices = info.vertices;
            text.UpdateGeometry(info.mesh, j);
        }

        return false;
    }

    private static void ApplyGlobalEffects(List<GlobalTextEffectEntry> effects, TMP_TextInfo textInfo, int charIndex, bool overrideTagEffects)
    {
        for (var i = 0; i < effects.Count; i++)
        {
            var entry = effects[i];
            if (entry.overrideTagEffects == overrideTagEffects)
            {
                entry.effect.ApplyEffect(textInfo, charIndex);
            }
        }
    }

    private static void ApplyTagEffects(List<TextEffectEntry> effects, TMP_TextInfo textInfo, int charIndex)
    {
        for (var i = 0; i < effects.Count; i++)
        {
            effects[i].effect.ApplyEffect(textInfo, charIndex);
        }
    }
}
