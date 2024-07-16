using UnityEngine;

namespace LethalPerformance.API;
internal static partial class ObjectExtensions
{
    public static bool ComparePlayerRagdollTag(GameObject gameObject)
    {
        return gameObject.CompareTag("PlayerRagdoll")
            || gameObject.CompareTag("PlayerRagdoll1")
            || gameObject.CompareTag("PlayerRagdoll2")
            || gameObject.CompareTag("PlayerRagdoll3");
    }
}
