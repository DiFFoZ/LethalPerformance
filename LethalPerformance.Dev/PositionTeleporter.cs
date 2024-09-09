using UnityEngine;

namespace LethalPerformance.Dev;
internal class PositionTeleporter : MonoBehaviour
{
    private void Update()
    {
        if (GameNetworkManager.Instance == null)
        {
            return;
        }

        var player = GameNetworkManager.Instance.localPlayerController;
        if (player == null || player.isTypingChat || player.quickMenuManager.isMenuOpen || player.inTerminalMenu
            || player.isInsideFactory)
        {
            return;
        }

        if (LethalPerformanceDevPlugin.Instance.Config.SavePositionButton.Value.IsPressed())
        {
            var position = player.transform.position;
            var rotation = player.transform.localEulerAngles;

            LethalPerformanceDevPlugin.Instance.Config.PositionToTeleport.Value = position;
            LethalPerformanceDevPlugin.Instance.Config.RotationToTeleport.Value = rotation;
        }

        if (LethalPerformanceDevPlugin.Instance.Config.TeleportToPositionButton.Value.IsPressed())
        {
            var position = LethalPerformanceDevPlugin.Instance.Config.PositionToTeleport.Value;
            var rotation = LethalPerformanceDevPlugin.Instance.Config.RotationToTeleport.Value;

            if (position == Vector3.zero)
            {
                return;
            }

            player.TeleportPlayer(position, withRotation: false, 0);
            player.transform.localEulerAngles = rotation;
        }
    }
}
