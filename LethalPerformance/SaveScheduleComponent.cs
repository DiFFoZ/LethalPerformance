using LethalPerformance.Patcher;
using UnityEngine;

namespace LethalPerformance;
internal sealed class SaveScheduleComponent : MonoBehaviour
{
    public void LateUpdate()
    {
        LethalPerformancePatcher.ConfigSaverTask.ScheduleSave();
    }

    public void OnDisable()
    {
        LethalPerformancePatcher.ConfigSaverTask.Save();
    }
}
