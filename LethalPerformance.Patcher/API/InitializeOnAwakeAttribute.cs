using System;

namespace LethalPerformance.Patcher.API;
[AttributeUsage(AttributeTargets.Method)]
internal sealed class InitializeOnAwakeAttribute : Attribute
{
}
