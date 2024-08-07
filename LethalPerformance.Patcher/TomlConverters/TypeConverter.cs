using System;
using BepTypeConverter = BepInEx.Configuration.TypeConverter;

namespace LethalPerformance.Patcher.TomlConverters;
internal abstract class TypeConverter<T> : BepTypeConverter where T : IEquatable<T>
{
    protected TypeConverter()
    {
        base.ConvertToObject = (value, _) => ConvertToObject(value);
        base.ConvertToString = (value, _) => ConvertToString((T)value);
    }

    public abstract T ConvertToObject(string value);

    public abstract string ConvertToString(T value);
}
