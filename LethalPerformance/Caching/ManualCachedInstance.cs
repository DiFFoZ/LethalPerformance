using UnityEngine;

namespace LethalPerformance.Caching;
internal class ManualCachedInstance<T> : UnsafeCachedInstance<T> where T : MonoBehaviour
{
    public ManualCachedInstance() : base()
    { }

    public override (bool, MonoBehaviour?) TryGetInstance(FindObjectsInactive findObjectsInactive)
    {
        LethalPerformancePlugin.Instance.Logger.LogInfo("Aboba");
        if (Instance == null)
        {
            return (true, null);
        }

        if (findObjectsInactive is FindObjectsInactive.Include)
        {
            return (true, Instance);
        }

        // .isActiveAndEnabled doesn't work until Awake method was called, using this to prevent that
        if (Instance!.enabled && Instance.gameObject.activeInHierarchy)
        {
            return (true, Instance);
        }

        return (true, null);
    }
}
