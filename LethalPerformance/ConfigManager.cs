using BepInEx.Configuration;

namespace LethalPerformance;
internal class ConfigManager
{
    private readonly ConfigFile m_Config;

    public ConfigManager(ConfigFile config)
    {
        m_Config = config;

        BindConfig();
    }

#nullable disable

    public ConfigEntry<bool> DisableUICamera { get; private set; }

#nullable restore

    private void BindConfig()
    {
        DisableUICamera = m_Config.Bind("Experimental", "DisableUICamera", false,
            """
            Disables UI camera to improve CPU time.
            Note: this option will make game to render in native resolution (meaning that settings via LCUltraWide will be ignored), so if you're GPU-bound I would not recommend to use this option.
            Incompatibilities with mods: LCVR (because it already disables UICamera).
            """);
    }
}
