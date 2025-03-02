using LethalPerformance.Patcher;
using UnityEngine;

namespace LethalPerformance;
internal sealed class SaveScheduleComponent : MonoBehaviour
{
    public void LateUpdate()
    {
        LethalPerformancePatcher.ConfigSaverTask.ScheduleSave();
        LethalPerformancePlugin.Instance.ES3SaverTask.SaveIfDirty();
    }

    public void OnDisable()
    {
        LethalPerformancePatcher.ConfigSaverTask.Save();
        LethalPerformancePlugin.Instance.ES3SaverTask.SaveIfDirty();
    }
}
