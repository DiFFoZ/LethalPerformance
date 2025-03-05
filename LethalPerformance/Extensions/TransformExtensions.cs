using System.Text;
using UnityEngine;

namespace LethalPerformance.Extensions;
internal static class TransformExtensions
{
    public static string GetScenePath(this Transform transform)
    {
        var sb = new StringBuilder();
        sb.Append('/').Append(transform.name);

        Transform parent;
        while (parent = transform.parent)
        {
            sb.Insert(0, parent.name)
                .Insert(0, '/');

            transform = parent;
        }

        return sb.ToString();
    }
}
