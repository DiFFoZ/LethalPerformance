using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace LethalPerformance.API;
internal static class ObjectExtensions
{
    public static T? FindObjectByTypeNonOrdered<T>() where T : Object
    {
#if ENABLE_PROFILER
        Profiler.BeginSample("DiFFoZ.FindObjectFast." + typeof(T).Name);
#endif
        var objects = Object.FindObjectsByType(typeof(T), FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var uObject = objects.Length > 0 ? objects[0] : null;

#if ENABLE_PROFILER
        Profiler.EndSample();
#endif

        return uObject as T;
    }

    public static T? FindObjectByTypeNonOrderedInActive<T>(bool includeInactive) where T : Object
    {
        var findObjectsInactive = includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;

        var objects = Object.FindObjectsByType(typeof(T), findObjectsInactive, FindObjectsSortMode.None);
        var uObject = objects.Length > 0 ? objects[0] : null;

        return uObject as T;
    }
}
