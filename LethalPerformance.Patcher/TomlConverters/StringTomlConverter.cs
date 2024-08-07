using System.Text;
using System.Text.RegularExpressions;

namespace LethalPerformance.Patcher.TomlConverters;
internal class StringTomlConverter : TypeConverter<string>
{
    private static readonly Regex s_WindowsPathRegex = new("^\"?\\w:\\\\(?!\\\\)(?!.+\\\\\\\\)", RegexOptions.Compiled);

    public override string ConvertToObject(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (s_WindowsPathRegex.IsMatch(value))
        {
            return value;
        }

        return Unescape(value);
    }

    public override string ConvertToString(string value)
    {
        return Escape(value);
    }

    private static string Escape(string txt)
    {
        if (string.IsNullOrEmpty(txt))
        {
            return string.Empty;
        }

        var stringBuilder = new StringBuilder(txt.Length + 2);
        foreach (var c in txt)
        {
            switch (c)
            {
                case '\0':
                    stringBuilder.Append(@"\0");
                    break;
                case '\a':
                    stringBuilder.Append(@"\a");
                    break;
                case '\b':
                    stringBuilder.Append(@"\b");
                    break;
                case '\t':
                    stringBuilder.Append(@"\t");
                    break;
                case '\n':
                    stringBuilder.Append(@"\n");
                    break;
                case '\v':
                    stringBuilder.Append(@"\v");
                    break;
                case '\f':
                    stringBuilder.Append(@"\f");
                    break;
                case '\r':
                    stringBuilder.Append(@"\r");
                    break;
                case '\'':
                    stringBuilder.Append(@"\'");
                    break;
                case '\"':
                    stringBuilder.Append(@"\""");
                    break;
                default:
                    stringBuilder.Append(c);
                    break;
            }
        }

        if (stringBuilder.Length == txt.Length)
        {
            // no changes required, returning original string
            return txt;
        }

        return stringBuilder.ToString();
    }

    private static string Unescape(string txt)
    {
        if (string.IsNullOrEmpty(txt))
        {
            return txt;
        }

        int indexOfBackslash = txt.IndexOf('\\');
        if (indexOfBackslash == -1)
        {
            return txt;
        }

        var stringBuilder = new StringBuilder(txt.Length);
        for (var i = indexOfBackslash; i < txt.Length;)
        {
            var num = txt.IndexOf('\\', i);
            if (num < 0 || num == txt.Length - 1)
            {
                num = txt.Length;
            }

            stringBuilder.Append(txt, i, num - i);
            if (num >= txt.Length)
            {
                break;
            }

            var c = txt[num + 1];
            switch (c)
            {
                case '0':
                    stringBuilder.Append('\0');
                    break;
                case 'a':
                    stringBuilder.Append('\a');
                    break;
                case 'b':
                    stringBuilder.Append('\b');
                    break;
                case 't':
                    stringBuilder.Append('\t');
                    break;
                case 'n':
                    stringBuilder.Append('\n');
                    break;
                case 'v':
                    stringBuilder.Append('\v');
                    break;
                case 'f':
                    stringBuilder.Append('\f');
                    break;
                case 'r':
                    stringBuilder.Append('\r');
                    break;
                case '\'':
                    stringBuilder.Append('\'');
                    break;
                case '\"':
                    stringBuilder.Append('\"');
                    break;
                case '\\':
                    stringBuilder.Append('\\');
                    break;
                default:
                    stringBuilder.Append('\\').Append(c);
                    break;
            }

            i = num + 2;
        }

        return stringBuilder.ToString();
    }
}
