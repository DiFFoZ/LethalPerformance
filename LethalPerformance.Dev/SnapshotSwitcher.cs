using UnityEngine;

namespace LethalPerformance.Dev;
internal class SnapshotSwitcher : MonoBehaviour
{
    internal void Update()
    {
        if (!LethalPerformanceDevPlugin.Instance.Config.NextMixerSnapshotButton.Value.IsDown())
        {
            return;
        }

        if (GameNetworkManager.Instance == null || SoundManager.Instance == null)
        {
            return;
        }

        var player = GameNetworkManager.Instance.localPlayerController;
        if (player != null && player.isTypingChat)
        {
            return;
        }

        var sound = SoundManager.Instance;
        var snapshots = sound.mixerSnapshots;

        var next = (sound.currentMixerSnapshotID + 1) % snapshots.Length;
        var snapshot = snapshots[next];

        sound.currentMixerSnapshotID = next;
        snapshot.TransitionTo(0.2f);
        Debug.Log($"[LP Dev] Mixer snapshot {next} ({snapshot.name})");
    }
}
