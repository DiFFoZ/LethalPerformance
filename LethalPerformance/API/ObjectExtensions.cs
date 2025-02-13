using LethalPerformance.Caching;
using UnityEngine;

namespace LethalPerformance.API;
internal static partial class ObjectExtensions
{
    public static T? FindObjectByTypeNonOrdered<T>() where T : Object
    {
        if (UnsafeCacheManager.TryGetCachedReference(typeof(T), FindObjectsInactive.Exclude, out var cache))
        {
            return (T?)cache;
        }

        var objects = Object.FindObjectsByType(typeof(T), FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var uObject = objects.Length > 0 ? objects[0] : null;

        return uObject as T;
    }

    public static T? FindObjectByTypeNonOrderedInActive<T>(bool includeInactive) where T : Object
    {
        if (UnsafeCacheManager.TryGetCachedReference(typeof(T), FindObjectsInactive.Include, out var cache))
        {
            return (T?)cache;
        }

        var findObjectsInactive = includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;

        var objects = Object.FindObjectsByType(typeof(T), findObjectsInactive, FindObjectsSortMode.None);
        var uObject = objects.Length > 0 ? objects[0] : null;

        return uObject as T;
    }
}
