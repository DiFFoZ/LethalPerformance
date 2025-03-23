using UnityEngine.SceneManagement;

namespace LethalPerformance.Utilities;
internal static class SceneUtilities
{
    public static bool IsDontDestroyOnLoad(this Scene scene)
    {
        return scene.buildIndex == -1;
    }

    public static bool IsMainMenu(this Scene scene)
    {
        return scene.name == "MainMenu";
    }

    public static bool IsSceneShip(this Scene scene)
    {
        return scene.name == "SampleSceneRelay";
    }

    public static Scene GetLastLoadedScene()
    {
        var sceneCount = SceneManager.sceneCount;
        return SceneManager.GetSceneAt(sceneCount - 1);
    }
}
