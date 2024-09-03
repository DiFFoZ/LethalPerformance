using LethalPerformance.Patcher.API;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace LethalPerformance.Utilities;
internal class OpenLogsFolderKeybind
{
    private static InputActionAsset? s_InputActions;

    [InitializeOnAwake]
    private static void Initialize()
    {
        var asset = s_InputActions = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.name = "LethalPerformance Input Asset";
        asset.hideFlags = HideFlags.HideAndDontSave;
        Object.DontDestroyOnLoad(asset);

        var actionMap = asset.AddActionMap("LPActionMap");
        var action = actionMap.AddAction("General", InputActionType.Button);

        action.AddCompositeBinding("TwoModifiers")
            .With("Binding", "<Keyboard>/l")
            .With("Modifier1", "<Keyboard>/ctrl")
            .With("Modifier2", "<Keyboard>/shift");

        action.performed += OnOpenLogsPressed;

        // defer initialization of input action
        SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
    }

    private static void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
    {
        SceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;

        
        if (s_InputActions != null)
        {
            s_InputActions.Enable();
        }
    }

    private static void OnOpenLogsPressed(InputAction.CallbackContext obj)
    {
        if (GameNetworkManager.Instance != null
            && GameNetworkManager.Instance.localPlayerController != null
            && GameNetworkManager.Instance.localPlayerController.isTypingChat)
        {
            return;
        }

        // wrap it with "" because of spacing
        Application.OpenURL($"\"{Application.persistentDataPath}\"");
    }
}
