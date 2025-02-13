using BepInEx.Configuration;
using UnityEngine;

namespace LethalPerformance.Dev.Configuration;
public class ConfigManager
{
    public ConfigEntry<int> OverriddenSeed { get; }

    public ConfigEntry<Vector3> PositionToTeleport { get; }
    public ConfigEntry<Vector3> RotationToTeleport { get; }
    public ConfigEntry<KeyboardShortcut> SavePositionButton { get; }
    public ConfigEntry<KeyboardShortcut> TeleportToPositionButton { get; }

    public ConfigEntry<bool> ShouldSpawnEnemies { get; }

    public ConfigEntry<LevelWeatherType> OverriddenWeather { get; }

    public ConfigManager(ConfigFile config)
    {
        OverriddenSeed = config.Bind("Debug", "Seed generation", 0);

        PositionToTeleport = config.Bind("Debug", "Position", Vector3.zero);
        RotationToTeleport = config.Bind("Debug", "Rotation", Vector3.zero);

        SavePositionButton = config.Bind("Debug", "Save position button", new KeyboardShortcut(KeyCode.Minus));
        TeleportToPositionButton = config.Bind("Debug", "Teleport to position button", new KeyboardShortcut(KeyCode.Equals));

        ShouldSpawnEnemies = config.Bind("Debug", "Should Spawn Enemies", true);

        OverriddenWeather = config.Bind("Weather", "Weather override", LevelWeatherType.None);
    }
}
