using DunGen;
using LethalPerformance.Patcher.API;

namespace LethalPerformance.Utilities;
internal class DungeonOptimization
{
    [InitializeOnAwake]
    private static void Initialize()
    {
        DungeonGenerator.OnAnyDungeonGenerationStarted += DungeonGenerator_OnAnyDungeonGenerationStarted;
    }

    private static void DungeonGenerator_OnAnyDungeonGenerationStarted(DungeonGenerator generator)
    {
        generator.CollisionSettings ??= new();

        // Removes searching for all tiles (used in DungeonCollisionManager.PreCacheBounds).
        // I'm sure that Lethal Company don't use multiple dungeons, so it should be safe
        generator.CollisionSettings.AvoidCollisionsWithOtherDungeons = false;
    }
}
