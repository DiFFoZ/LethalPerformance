using System;
using System.Collections.Generic;
using System.Reflection;
using GameNetcodeStuff;
using HarmonyLib;
using LethalPerformance.API;
using LethalPerformance.Patcher.API;

namespace LethalPerformance.Patches.FindingObjectOptimization;
[HarmonyPatch]
internal static class ReplaceFindObjectOfTypePatch
{
    private static readonly Dictionary<Type, MethodInfo> s_MapGettingInstance = new()
    {
        { typeof(StartOfRound), AccessTools.PropertyGetter(typeof(StartOfRound), nameof(StartOfRound.Instance)) },
        { typeof(TimeOfDay), AccessTools.PropertyGetter(typeof(TimeOfDay), nameof(TimeOfDay.Instance)) },
        { typeof(GameNetworkManager), AccessTools.PropertyGetter(typeof(GameNetworkManager), nameof(GameNetworkManager.Instance)) },
        { typeof(HUDManager), AccessTools.PropertyGetter(typeof(HUDManager), nameof(HUDManager.Instance)) },
        { typeof(GlobalEffects), AccessTools.PropertyGetter(typeof(GlobalEffects), nameof(GlobalEffects.Instance)) },
        { typeof(IngamePlayerSettings), AccessTools.PropertyGetter(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.Instance)) },
        { typeof(RoundManager), AccessTools.PropertyGetter(typeof(RoundManager), nameof(RoundManager.Instance)) },
        { typeof(ShipBuildModeManager), AccessTools.PropertyGetter(typeof(ShipBuildModeManager), nameof(ShipBuildModeManager.Instance)) },
        { typeof(SoundManager), AccessTools.PropertyGetter(typeof(SoundManager), nameof(SoundManager.Instance)) },
        { typeof(SteamManager), AccessTools.PropertyGetter(typeof(SteamManager), nameof(SteamManager.Instance)) },
    }; 

    private static readonly HashSet<Type> s_ExcludedTypes = [];

    private static readonly MethodInfo s_FindObjectByTypeNonOrdered = typeof(ObjectExtensions)
        .GetMethod(nameof(ObjectExtensions.FindObjectByTypeNonOrdered), AccessTools.all);

    private static readonly MethodInfo s_FindObjectByTypeNonOrderedInActive = typeof(ObjectExtensions)
        .GetMethod(nameof(ObjectExtensions.FindObjectByTypeNonOrderedInActive), AccessTools.all);

    [HarmonyCleanup]
    public static Exception? Cleanup(Exception exception)
    {
        return HarmonyExceptionHandler.ReportException(exception);
    }

    [HarmonyTargetMethods]
    private static IEnumerable<MethodInfo> GetTargetMethods()
    {
        var type = typeof(BreakerBox);

        yield return type.GetMethod(nameof(BreakerBox.Start), AccessTools.all);
        yield return type.GetMethod(nameof(BreakerBox.SetSwitchesOff), AccessTools.all);

        type = typeof(FoliageDetailDistance);

        yield return type.GetMethod(nameof(FoliageDetailDistance.Start), AccessTools.all);

        type = typeof(animatedSun);

        yield return type.GetMethod(nameof(animatedSun.Start), AccessTools.all);

        type = typeof(BoomboxItem);

        yield return type.GetMethod(nameof(BoomboxItem.Start), AccessTools.all);

        type = typeof(TimeOfDay);

        yield return type.GetMethod(nameof(TimeOfDay.Start), AccessTools.all);
        yield return type.GetMethod(nameof(TimeOfDay.SyncNewProfitQuotaClientRpc), AccessTools.all);

        type = typeof(Terminal);

        yield return type.GetMethod(nameof(Terminal.Start), AccessTools.all);
        yield return type.GetMethod(nameof(Terminal.ParsePlayerSentence), AccessTools.all);
        yield return type.GetMethod(nameof(Terminal.LoadNewNodeIfAffordable), AccessTools.all);
        yield return AccessTools.EnumeratorMoveNext(type.GetMethod(nameof(Terminal.displayReimbursedTipDelay), AccessTools.all));

        type = typeof(StartOfRound);

        yield return type.GetMethod(nameof(StartOfRound.Start), AccessTools.all);
        yield return type.GetMethod(nameof(StartOfRound.SetMapScreenInfoToCurrentLevel), AccessTools.all);
        yield return type.GetMethod(nameof(StartOfRound.ChangePlanet), AccessTools.all);
        yield return type.GetMethod(nameof(StartOfRound.ArriveAtLevel), AccessTools.all);
        yield return type.GetMethod(nameof(StartOfRound.SwitchMapMonitorPurpose), AccessTools.all);
        yield return type.GetMethod(nameof(StartOfRound.StartGameServerRpc), AccessTools.all);
        yield return type.GetMethod(nameof(StartOfRound.StartGame), AccessTools.all);
        yield return type.GetMethod(nameof(StartOfRound.ShipHasLeft), AccessTools.all);
        yield return type.GetMethod(nameof(StartOfRound.SetTimeAndPlanetToSavedSettings), AccessTools.all);
        yield return type.GetMethod(nameof(StartOfRound.SetShipReadyToLand), AccessTools.all);
        yield return type.GetMethod(nameof(StartOfRound.SetShipDoorsOverheatClientRpc), AccessTools.all);
        yield return type.GetMethod(nameof(StartOfRound.SetPlanetsMold), AccessTools.all);
        yield return type.GetMethod(nameof(StartOfRound.ResetShip), AccessTools.all);
        yield return type.GetMethod(nameof(StartOfRound.ResetPlayersLoadedValueClientRpc), AccessTools.all);
        yield return type.GetMethod(nameof(StartOfRound.ResetMoldStates), AccessTools.all);
        yield return type.GetMethod(nameof(StartOfRound.PowerSurgeShip), AccessTools.all);
        yield return type.GetMethod(nameof(StartOfRound.PassTimeToNextDay), AccessTools.all);
        yield return AccessTools.EnumeratorMoveNext(type.GetMethod(nameof(StartOfRound.TravelToLevelEffects), AccessTools.all));

        type = typeof(Shovel);

        yield return type.GetMethod(nameof(Shovel.HitShovel), AccessTools.all);

        type = typeof(RoundManager);

        yield return type.GetMethod(nameof(RoundManager.Start), AccessTools.all);
        yield return AccessTools.EnumeratorMoveNext(type.GetMethod(nameof(RoundManager.turnOnLights), AccessTools.all));

        type = typeof(OutOfBoundsTrigger);

        yield return type.GetMethod(nameof(OutOfBoundsTrigger.Start), AccessTools.all);

        type = typeof(MouthDogAI);

        yield return type.GetMethod(nameof(MouthDogAI.Start), AccessTools.all);

        type = typeof(LungProp);

        yield return type.GetMethod(nameof(LungProp.Start), AccessTools.all);

        type = typeof(LevelGenerationManager);

        yield return type.GetMethod(nameof(LevelGenerationManager.Awake), AccessTools.all);

        type = typeof(Landmine);

        yield return AccessTools.EnumeratorMoveNext(type.GetMethod(nameof(Landmine.StartIdleAnimation), AccessTools.all));

        type = typeof(KnifeItem);

        yield return type.GetMethod(nameof(KnifeItem.HitKnife), AccessTools.all);

        type = typeof(ItemDropship);

        yield return type.GetMethod(nameof(ItemDropship.Start), AccessTools.all);

        type = typeof(InteractTrigger);

        yield return type.GetMethod(nameof(InteractTrigger.Start), AccessTools.all);

        type = typeof(HUDManager);

        yield return type.GetMethod(nameof(HUDManager.Awake), AccessTools.all);

        type = typeof(HangarShipDoor);

        yield return type.GetMethod(nameof(HangarShipDoor.Start), AccessTools.all);

        type = typeof(GlobalEffects);

        yield return type.GetMethod(nameof(GlobalEffects.Awake), AccessTools.all);

        type = typeof(GameNetworkManager);

        yield return type.GetMethod(nameof(GameNetworkManager.SaveGameValues), AccessTools.all);
        yield return type.GetMethod(nameof(GameNetworkManager.ResetSavedGameValues), AccessTools.all);

        type = typeof(PlayerControllerB);

        yield return type.GetMethod(nameof(PlayerControllerB.SetSpectatedPlayerEffects), AccessTools.all);

        type = typeof(EntranceTeleport);

        yield return type.GetMethod(nameof(EntranceTeleport.Awake), AccessTools.all);

        type = typeof(EnemyVent);

        yield return type.GetMethod(nameof(EnemyVent.Start), AccessTools.all);

        type = typeof(Anomaly);

        yield return type.GetMethod(nameof(Anomaly.Start), AccessTools.all);

        type = typeof(PlayerVoiceIngameSettings);

        yield return type.GetMethod(nameof(PlayerVoiceIngameSettings.InitializeComponents), AccessTools.all);

        type = typeof(StunGrenadeItem);

        yield return type.GetMethod(nameof(StunGrenadeItem.ExplodeStunGrenade), AccessTools.all);

        type = typeof(StoryLog);

        yield return type.GetMethod(nameof(StoryLog.Start), AccessTools.all);
        yield return type.GetMethod(nameof(StoryLog.CollectLog), AccessTools.all);

        type = typeof(AudioReverbTrigger);

        yield return type.GetMethod(nameof(AudioReverbTrigger.OnTriggerStay), AccessTools.all);
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> ReplaceFindObjectOfTypeTranspiler(IEnumerable<CodeInstruction> codeInstructions)
    {
        var matcher = new CodeMatcher(codeInstructions);

        matcher.MatchForward(false, new CodeMatch(CallsFindObjectOfType))
            .Repeat(m =>
            {
                var method = (MethodInfo)matcher.Operand;
                var genericType = method.GetGenericArguments()[0];
                var parameters = method.GetParameters();

                if (s_MapGettingInstance.TryGetValue(genericType, out var instanceGetter))
                {
                    if (parameters.Length > 0 && parameters[0].ParameterType == typeof(bool))
                    {
                        // remove ldc.i4.0 or ldc.i4.1
                        m.Advance(-1)
                        .RemoveInstruction();
                    }

                    matcher.Operand = instanceGetter;
                    return;
                }

                // replace it with FindObjectByType non ordered
                if (parameters.Length > 0 && parameters[0].ParameterType == typeof(bool))
                {
                    instanceGetter = s_FindObjectByTypeNonOrderedInActive.MakeGenericMethod(genericType);
                }
                else
                {
                    instanceGetter = s_FindObjectByTypeNonOrdered.MakeGenericMethod(genericType);
                }

                matcher.Operand = instanceGetter;
            });

        return matcher.InstructionEnumeration();
    }

    private static bool CallsFindObjectOfType(CodeInstruction instruction)
    {
        if (instruction.operand is not MethodInfo method
            || !method.IsGenericMethod
            || method.DeclaringType != typeof(Object)
            || !method.Name.Equals(nameof(Object.FindObjectOfType)))
        {
            return false;
        }

        var genericType = method.GetGenericArguments()[0];
        if (s_ExcludedTypes.Contains(genericType))
        {
            return false;
        }

        return true;
    }
}
