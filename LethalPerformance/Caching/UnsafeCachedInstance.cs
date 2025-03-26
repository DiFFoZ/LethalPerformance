using System.Collections.Generic;
using UnityEngine;

namespace LethalPerformance.Caching;
internal abstract class UnsafeCachedInstance
{
    public static List<UnsafeCachedInstance> UnsafeCachedInstances { get; private set; } = new();

    public abstract InstanceResult TryGetInstance(FindObjectsInactive findObjectsInactive);

    public abstract void Cleanup();

    public static T AddSelfToMap<T, B>(T unsafeInstance) where B : Behaviour where T : UnsafeCachedInstance<B>
    {
        return (T)UnsafeCacheManager.AddReferenceToMap(unsafeInstance);
    }
}

internal class UnsafeCachedInstance<T> : UnsafeCachedInstance where T : Behaviour
{
    public T? Instance { get; protected set; }

    protected UnsafeCachedInstance()
    {
        Instance = null;

        UnsafeCachedInstances.Add(this);
    }

    public void SetInstance(T? instance)
    {
        if (instance != null && Instance != null)
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning($"{typeof(T).Name} is requested caching, while cached instance is still alive");
        }

        Instance = instance;
    }

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
        if (Instance.enabled && Instance.gameObject.activeInHierarchy)
        {
            return InstanceResult.Found(Instance);
        }

        return InstanceResult.Found(null);
    }

    public override void Cleanup()
    {
        Instance = null;
    }
}
