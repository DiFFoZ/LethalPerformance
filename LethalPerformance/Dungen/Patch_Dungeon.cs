namespace LethalPerformance.Dungen;

internal static partial class Patch_Dungeon
{
    public static void PatchAll()
    {
        var harmony = LethalPerformancePlugin.Instance.Harmony!;

        harmony.PatchAll(typeof(Patch_Initialization));
        harmony.PatchAll(typeof(Patch_DoorwayPairFinder));
        harmony.PatchAll(typeof(Patch_Misc));
    }
}
