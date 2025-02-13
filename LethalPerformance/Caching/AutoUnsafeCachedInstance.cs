using UnityEngine;

namespace LethalPerformance.Caching;
internal class AutoUnsafeCachedInstance<T> : UnsafeCachedInstance<T>, IAutoInstance where T : Behaviour
{
    private readonly string m_HierarchyPath;

    public AutoUnsafeCachedInstance(string hierarchyPath) : base()
    {
        m_HierarchyPath = hierarchyPath;
    }

    public void SaveInstance()
    {
        if (Instance != null)
        {
            LethalPerformancePlugin.Instance.Logger.LogWarning($"{typeof(T).Name} instance is already cached");
        }

        var gameObject = GameObject.Find(m_HierarchyPath);
        if (gameObject != null)
        {
            if (gameObject.TryGetComponent<T>(out var component))
            {
                Instance = component;
                return;
            }

            LethalPerformancePlugin.Instance.Logger.LogWarning($"Failed to cache instance of {typeof(T).Name}");
            return;
        }

        LethalPerformancePlugin.Instance.Logger.LogWarning($"Failed to find gameobject of {typeof(T).Name}");
    }
}
