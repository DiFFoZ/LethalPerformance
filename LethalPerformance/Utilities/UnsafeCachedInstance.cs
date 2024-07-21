using System.Collections.Generic;
using UnityEngine;

namespace LethalPerformance.Utilities;
internal abstract class UnsafeCachedInstance
{
    public static List<UnsafeCachedInstance> UnsafeCachedInstances { get; private set; } = new();

    public abstract void SaveInstance();

    public abstract void Cleanup();
}

internal sealed class UnsafeCachedInstance<T> : UnsafeCachedInstance where T : MonoBehaviour
{
    private readonly string m_HierarchyPath;

    public T? Instance { get; set; }

    public UnsafeCachedInstance(string hierarchyPath)
    {
        m_HierarchyPath = hierarchyPath;
        Instance = null;

        UnsafeCachedInstances.Add(this);
    }

    public override void SaveInstance()
    {
        var gameObject = GameObject.Find(m_HierarchyPath);
        if (gameObject != null)
        {
            if (gameObject.TryGetComponent<T>(out var component))
            {
                Instance = component;
            }
            else
            {
                LethalPerformancePlugin.Instance.Logger.LogWarning($"Failed to cache instance of {typeof(T).Name}");
            }
        }
        else
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning($"Failed to find gameobject of {typeof(T).Name}");
        }
    }

    public (bool, MonoBehaviour?) TryGetInstance(FindObjectsInactive findObjectsInactive)
    {
        if (Instance == null)
        {
            return (false, null);
        }

        if (findObjectsInactive is FindObjectsInactive.Include && Instance.isActiveAndEnabled)
        {
            return (true, Instance);
        }

        return (false, null);
    }

    public (bool, MonoBehaviour?) TryGetOnlyActiveInstance(FindObjectsInactive findObjectsInactive)
    {
        if (Instance == null)
        {
            return (false, null);
        }

        if (findObjectsInactive is FindObjectsInactive.Include)
        {
            return (true, Instance);
        }

        if (Instance.isActiveAndEnabled)
        {
            return (true, Instance);
        }

        // note: returning as found, but instance is null
        // making that way we sure that instance exists, but not active
        // meaning that Object.FindObjectOfType(bool includeInactive = false) will always return null
        return (true, null);
    }

    public override void Cleanup()
    {
        Instance = null;
    }
}
