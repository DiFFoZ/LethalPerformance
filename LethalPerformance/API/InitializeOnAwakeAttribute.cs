using System;

namespace LethalPerformance.API;
[AttributeUsage(AttributeTargets.Method)]
internal sealed class InitializeOnAwakeAttribute : Attribute
{
}
