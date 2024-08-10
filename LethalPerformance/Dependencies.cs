using BepInEx.Bootstrap;

namespace LethalPerformance;
internal static class Dependencies
{
    public const string MoreCompany = "me.swipez.melonloader.morecompany";

    public const string Loadstone = "com.adibtw.loadstone";

    public const string MoreSuits = "x753.More_Suits";

    public const string LethalCompanyVR = "io.daxcess.lcvr";

    public static bool IsModLoaded(string id)
    {
        return Chainloader.PluginInfos.ContainsKey(id);
    }
}
