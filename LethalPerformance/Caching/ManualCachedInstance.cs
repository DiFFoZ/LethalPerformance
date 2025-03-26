using UnityEngine;

namespace LethalPerformance.Caching;
internal class ManualCachedInstance<T> : UnsafeCachedInstance<T> where T : Behaviour
{
    public ManualCachedInstance() : base()
    { }

    public override InstanceResult TryGetInstance(FindObjectsInactive findObjectsInactive)
    {
        if (Instance == null)
        {
            return InstanceResult.NotFound(null);
        }

        if (findObjectsInactive is FindObjectsInactive.Include)
        {
            return InstanceResult.Found(Instance);
        }

        // .isActiveAndEnabled doesn't work until Awake method was called, using this to prevent that
        if (Instance!.enabled && Instance.gameObject.activeInHierarchy)
        {
            return InstanceResult.Found(Instance);
        }

        return InstanceResult.Found(null);
    }
}
