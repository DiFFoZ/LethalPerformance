using System;
using BepInEx.Configuration;

namespace LethalPerformance.Configuration;
internal class AcceptableValueEnum<T> : AcceptableValueBase where T : struct
{
    private static readonly string[] s_EnumValues = Enum.GetNames(typeof(T));

    public AcceptableValueEnum() : base(typeof(T))
    {
        if (s_EnumValues.Length == 0)
        {
            throw new ArgumentException("Enum should have any value");
        }
    }

    public override object Clamp(object value)
    {
        if (IsValid(value))
        {
            return value;
        }

        return Enum.Parse<T>(s_EnumValues[0]);
    }

    public override bool IsValid(object value)
    {
        return Enum.IsDefined(typeof(T), value);
    }

    public override string ToDescriptionString()
    {
        return "# Acceptable values: " + string.Join(", ", s_EnumValues);
    }
}
