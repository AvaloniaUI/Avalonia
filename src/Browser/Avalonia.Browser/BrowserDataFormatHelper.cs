using Avalonia.Input.Platform;

namespace Avalonia.Browser;

internal static class BrowserDataFormatHelper
{
    private const string FormatTextPlain = "text/plain";
    private const string FormatFiles = "Files";

    public static DataFormat ToDataFormat(string formatString)
        => formatString switch
        {
            FormatTextPlain => DataFormat.Text,
            FormatFiles => DataFormat.File,
            _ => DataFormat.FromSystemName(formatString)
        };

    public static string ToBrowserFormat(DataFormat format)
    {
        if (DataFormat.Text.Equals(format))
            return FormatTextPlain;

        if (DataFormat.File.Equals(format))
            return FormatFiles;

        return format.SystemName;
    }
}
