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

#nullable restore

    private void BindConfig()
    {
    }
}
