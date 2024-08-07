using System;

namespace LethalPerformance.Patcher.Helpers;
internal static class SpanExtensions
{
    public static bool Split(ReadOnlySpan<char> input, Span<Range> ranges, char separator)
    {
        if (ranges.IsEmpty && ranges.Length != 2)
        {
            return false;
        }

        for (var i = 0; i < input.Length; i++)
        {
            var chr = input[i];
            if (chr == separator)
            {
                ranges[0] = 0..i;
                ranges[1] = (i + 1)..input.Length;
                return true;
            }
        }

        return false;
    }
}
