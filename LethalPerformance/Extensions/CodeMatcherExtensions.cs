using System.Collections.Generic;
using HarmonyLib;

namespace LethalPerformance.Extensions;
internal static class CodeMatcherExtensions
{
    public static CodeMatcher GetOperand(this CodeMatcher matcher, out object operand)
    {
        operand = matcher.Operand;
        return matcher;
    }
}
