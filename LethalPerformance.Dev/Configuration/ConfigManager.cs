using BepInEx.Configuration;

namespace LethalPerformance.Dev.Configuration;
public class ConfigManager
{
    public ConfigEntry<int> OverriddenSeed { get; }

    public ConfigManager(ConfigFile config)
    {
        OverriddenSeed = config.Bind("Debug", "Seed generation", 0);
    }
}
