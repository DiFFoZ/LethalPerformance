using UnityEngine.SceneManagement;

namespace LethalPerformance.Utilities;
internal static class SceneUtilities
{
    public static bool IsSceneShip(this Scene scene)
    {
        return scene.name == "SampleSceneRelay";
    }
}
