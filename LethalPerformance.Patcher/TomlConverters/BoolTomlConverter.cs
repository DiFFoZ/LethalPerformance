namespace LethalPerformance.Patcher.TomlConverters;
internal class BoolTomlConverter : TypeConverter<bool>
{
    public override bool ConvertToObject(string value)
    {
        return bool.Parse(value);
    }

    public override string ConvertToString(bool value)
    {
        return value ? "true" : "false";
    }
}
