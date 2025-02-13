using System.Collections.Generic;
using UnityEngine;

namespace LethalPerformance.Caching;
internal abstract class UnsafeCachedInstance
{
    public static List<UnsafeCachedInstance> UnsafeCachedInstances { get; private set; } = new();

    public abstract (bool found, Behaviour? instance) TryGetInstance(FindObjectsInactive findObjectsInactive);

    public abstract void Cleanup();
}

internal class UnsafeCachedInstance<T> : UnsafeCachedInstance where T : Behaviour
{
    public T? Instance { get; protected set; }

    public UnsafeCachedInstance()
    {
        Instance = null;

        UnsafeCachedInstances.Add(this);
    }

    public void SetInstance(T instance)
    {
        if (Instance != null)
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning($"{typeof(T).Name} is requested caching, while cached instance is still alive");
        }

        Instance = instance;
    }

    public override (bool, Behaviour?) TryGetInstance(FindObjectsInactive findObjectsInactive)
    {
        if (Instance == null)
        {
            return (false, null);
        }

        if (findObjectsInactive is FindObjectsInactive.Include)
        {
            return (true, Instance);
        }

        // .isActiveAndEnabled doesn't work until Awake method was called, using this to prevent that
        if (Instance.enabled && Instance.gameObject.activeInHierarchy)
        {
            return (true, Instance);
        }

        return (true, null);
    }

    public override void Cleanup()
    {
        Instance = null;
    }
}
