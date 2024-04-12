using System.Text;

namespace Avalonia;

internal static class DebugDisplayHelper
{
    public static void AppendOptionalValue(StringBuilder builder, string name, object? value, bool includeContent)
    {
        const int maxValueLength = 50;

        if (value is null or string { Length: 0 })
        {
            return;
        }

        if (builder.Length > 0 && builder[builder.Length - 1] == ')')
        {
            --builder.Length;
            builder.Append(", ");
        }
        else
        {
            builder.Append(" (");
        }

        builder.Append(name);
        builder.Append(" = ");

        if (value is AvaloniaObject avaloniaObject)
        {
            avaloniaObject.BuildDebugDisplay(builder, includeContent);
        }
        else
        {
            var stringValue = value.ToString();
            if (stringValue?.Length > maxValueLength)
            {
                builder.Append(stringValue, 0, maxValueLength - 1);
                builder.Append('…');
            }
            else
            {
                builder.Append(stringValue);
            }
        }

        builder.Append(')');
    }
}
