using System;
using Avalonia.Input;

namespace Avalonia.Browser;

internal static class BrowserDataFormatHelper
{
    private const string FormatTextPlain = "text/plain";
    private const string FormatFiles = "Files";
    private const string FormatImage = "image/png";
    private const string AppPrefix = "application/avn-fmt.";

    public static DataFormat ToDataFormat(string formatString)
        => formatString switch
        {
            FormatTextPlain => DataFormat.Text,
            FormatFiles => DataFormat.File,
            _ when IsTextFormat(formatString) => DataFormat.FromSystemName<string>(formatString, AppPrefix),
            _ => DataFormat.FromSystemName<byte[]>(formatString, AppPrefix)
        };

    private static bool IsTextFormat(string format)
        => format.StartsWith("text/", StringComparison.OrdinalIgnoreCase);

    public static string ToBrowserFormat(DataFormat format)
    {
        if (DataFormat.Text.Equals(format))
            return FormatTextPlain;

        if (DataFormat.File.Equals(format))
            return FormatFiles;

        if (DataFormat.Bitmap.Equals(format))
            return FormatImage;

        return format.ToSystemName(AppPrefix);
    }
}
