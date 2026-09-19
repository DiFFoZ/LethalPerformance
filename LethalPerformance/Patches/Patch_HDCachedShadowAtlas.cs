using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using LethalPerformance.Patcher.API;
using LethalPerformance.Unity;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace LethalPerformance.Patches;

[HarmonyPatch(typeof(HDCachedShadowAtlas))]
internal static class Patch_HDCachedShadowAtlas
{
    private sealed class AtlasState
    {
        public readonly HashSet<int> FailedLightIds = new();
        public bool ForceFullPack = true;
    }

    private static readonly ConditionalWeakTable<HDCachedShadowAtlas, AtlasState> s_States = new();

    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    private static AtlasState GetState(HDCachedShadowAtlas atlas)
    {
        return s_States.GetValue(atlas, static _ => new AtlasState());
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(HDCachedShadowAtlas.AssignOffsetsInAtlas))]
    public static bool AssignOffsetsInAtlas(HDCachedShadowAtlas __instance, HDShadowInitParameters initParameters)
    {
        var pending = __instance.m_RegisteredLightDataPendingPlacement;
        if (pending.Count == 0 || !__instance.m_CanTryPlacement)
        {
            return false;
        }

        var state = GetState(__instance);
        var tempList = __instance.m_TempListForPlacement;
        tempList.Clear();

        if (state.ForceFullPack)
        {
            state.FailedLightIds.Clear();
            tempList.AddRange(__instance.m_RecordsPendingPlacement.Values);
            __instance.AddLightListToRecordList(pending, initParameters, ref tempList);
            state.ForceFullPack = false;
        }
        else
        {
            AddLightsNotYetFailed(__instance, pending, initParameters, state.FailedLightIds);
            if (tempList.Count == 0)
            {
                __instance.m_CanTryPlacement = false;
                return false;
            }
        }

        if (__instance.m_NeedOptimalPacking)
        {
            __instance.InsertionSort(ref tempList, 0, tempList.Count);
            __instance.m_NeedOptimalPacking = false;
        }

        __instance.PerformPlacement();
        __instance.m_CanTryPlacement = false;

        foreach (var id in pending.Keys)
        {
            state.FailedLightIds.Add(id);
        }

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(HDCachedShadowAtlas.InitAtlas))]
    public static void InitAtlas(HDCachedShadowAtlas __instance)
    {
        var state = GetState(__instance);
        state.FailedLightIds.Clear();
        state.ForceFullPack = true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(HDCachedShadowAtlas.EvictLight))]
    public static void EvictLight(HDCachedShadowAtlas __instance, HDAdditionalLightData lightData)
    {
        var lightId = lightData.lightIdxForCachedShadows;
        if (lightId < 0 || !__instance.m_PlacedShadows.ContainsKey(lightId))
        {
            return;
        }

        var state = GetState(__instance);
        state.FailedLightIds.Clear();
        state.ForceFullPack = true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(HDCachedShadowAtlas.FindSlotInAtlas), [typeof(int), typeof(bool), typeof(int), typeof(int)], [ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out])]
    public static unsafe bool FindSlotInAtlas(HDCachedShadowAtlas __instance, int resolution, bool tempFill, out int x, out int y, ref bool __result)
    {
        var numEntries = HDUtils.DivRoundUp(resolution, 64);
        var res = __instance.m_AtlasResolutionInSlots;
        var max = res - numEntries;
        if (max < 0)
        {
            x = 0;
            y = 0;
            __result = false;
            return false;
        }

        bool found;

        var list = __instance.m_AtlasSlots;
        var slots = NoAllocHelpers.ExtractArrayFromListT(list);
        fixed (HDCachedShadowAtlas.SlotValue* slotPtr = slots)
        {
            found = CachedShadowAtlasBurst.FindFreeSlot((byte*)slotPtr, list.Count, res, numEntries, out x, out y);
        }

        if (!found)
        {
            x = 0;
            y = 0;
            __result = false;
            return false;
        }

        if (tempFill)
        {
            __instance.MarkEntries(x, y, numEntries, HDCachedShadowAtlas.SlotValue.TempOccupied);
        }

        __result = true;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(HDCachedShadowAtlas.PlaceMultipleShadows))]
    public static bool PlaceMultipleShadows(HDCachedShadowAtlas __instance, int startIdx, int numberOfShadows, ref bool __result)
    {
        var viewportSize = __instance.m_TempListForPlacement[startIdx].viewportSize;
        var entries = HDUtils.DivRoundUp(viewportSize, 64);

        Span<Vector2Int> slots = stackalloc Vector2Int[6];
        var placed = 0;
        for (var i = 0; i < numberOfShadows; i++)
        {
            if (!__instance.GetSlotInAtlas(__instance.m_TempListForPlacement[startIdx + i].viewportSize, out var x, out var y))
            {
                break;
            }

            slots[i] = new Vector2Int(x, y);
            placed++;
        }

        if (placed == numberOfShadows)
        {
            for (var i = 0; i < numberOfShadows; i++)
            {
                var value = __instance.m_TempListForPlacement[startIdx + i];
                value.offsetInAtlas = new Vector4(slots[i].x * 64, slots[i].y * 64, slots[i].x, slots[i].y);
                if (value.rendersOnPlacement)
                {
                    __instance.m_ShadowsPendingRendering.Add(value.shadowIndex, value);
                }

                __instance.m_PlacedShadows.Add(value.shadowIndex, value);
            }

            __result = true;
            return false;
        }

        if (placed > 0)
        {
            for (var i = 0; i < placed; i++)
            {
                __instance.MarkEntries(slots[i].x, slots[i].y, entries, HDCachedShadowAtlas.SlotValue.Free);
            }
        }

        __result = false;
        return false;
    }

    private static void AddLightsNotYetFailed(HDCachedShadowAtlas atlas, Dictionary<int, HDAdditionalLightData> lightList,
        HDShadowInitParameters initParams, HashSet<int> failedLightIds)
    {
        foreach (var value in lightList.Values)
        {
            var lightId = value.lightIdxForCachedShadows;
            if (failedLightIds.Contains(lightId))
            {
                continue;
            }

            var resolution = value.GetResolutionFromSettings(atlas.m_ShadowType, initParams);
            var shadowCount = value.type != HDLightType.Point ? 1 : 6;
            for (var i = 0; i < shadowCount; i++)
            {
                HDCachedShadowAtlas.CachedShadowRecord item = new()
                {
                    shadowIndex = lightId + i,
                    viewportSize = resolution,
                    offsetInAtlas = new Vector4(-1f, -1f, -1f, -1f),
                    rendersOnPlacement = value.shadowUpdateMode != ShadowUpdateMode.OnDemand || value.forceRenderOnPlacement || value.onDemandShadowRenderOnPlacement
                };

                value.forceRenderOnPlacement = false;
                atlas.m_TempListForPlacement.Add(item);
            }
        }
    }
}
