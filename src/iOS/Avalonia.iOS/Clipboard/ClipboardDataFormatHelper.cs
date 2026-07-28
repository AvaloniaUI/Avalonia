using System;
using Avalonia.Input;
using UniformTypeIdentifiers;

namespace Avalonia.iOS.Clipboard;

internal static class ClipboardDataFormatHelper
{
    private const string UTTypeUTF8PlainText = "public.utf8-plain-text";
    private const string UTTypeFileUrl = "public.file-url";
    internal const string UTTypeImage = "public.image";
    internal const string UTTypePng = "public.png";
    internal const string UTTypeJpeg = "public.jpeg";
    internal const string UTTypeTiff = "public.tiff";
    private const string AppPrefix = "net.avaloniaui.app.uti.";

    public static DataFormat ToDataFormat(string type)
    {
        return type switch
        {
            UTTypeUTF8PlainText => DataFormat.Text,
            UTTypeFileUrl => DataFormat.File,
            UTTypeImage or UTTypePng or UTTypeJpeg or UTTypeTiff => DataFormat.Bitmap,
            _ when IsTextUti(type) => DataFormat.FromSystemName<string>(type, AppPrefix),
            _ => DataFormat.FromSystemName<byte[]>(type, AppPrefix)
        };
    }

    public static string ToSystemType(DataFormat format)
    {
        if (DataFormat.Text.Equals(format))
            return UTTypeUTF8PlainText;

        if (DataFormat.File.Equals(format))
            return UTTypeFileUrl;

        if (DataFormat.Bitmap.Equals(format))
            // Avalonia writes images as PNGs to the clipboard for iOS/macOS.
            return UTTypePng;

        return format.ToSystemName(AppPrefix);
    }

    /// <summary>
    /// Best effort trying to find whether an UTI is text.
    /// Falling back to <c>byte[]</c> is fine.
    /// </summary>
    private static bool IsTextUti(string type)
    {
        try
        {
            return !type.StartsWith(AppPrefix, StringComparison.OrdinalIgnoreCase)
               && UTType.CreateFromIdentifier(type) is { } utType
               && utType.ConformsTo(UTTypes.Text);
        }
        catch
        {
            return false;
        }
    }
}
