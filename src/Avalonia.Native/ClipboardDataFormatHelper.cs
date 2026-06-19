using System;
using Avalonia.Input;
using Avalonia.Native.Interop;

namespace Avalonia.Native;

internal static class ClipboardDataFormatHelper
{
    // TODO hide native types behind IAvnClipboard abstraction, so managed side won't depend on macOS.
    private const string NSPasteboardTypeString = "public.utf8-plain-text";
    private const string NSPasteboardTypeFileUrl = "public.file-url";
    private const string NSPasteboardTypePng = "public.png";
    private const string AppPrefix = "net.avaloniaui.app.uti.";

    public static DataFormat[] ToDataFormats(IAvnStringArray? nativeFormats, Func<string, bool> isTextFormat)
    {
        if (nativeFormats is null)
            return [];

        var count = nativeFormats.Count;
        if (count == 0)
            return [];

        var results = new DataFormat[count];

        for (var c = 0u; c < count; c++)
        {
            using var nativeFormat = nativeFormats.Get(c);
            results[c] = ToDataFormat(nativeFormat.String ?? string.Empty, isTextFormat);
        }

        return results;
    }

    public static DataFormat ToDataFormat(string nativeFormat, Func<string, bool> isTextFormat)
        => nativeFormat switch
        {
            NSPasteboardTypeString => DataFormat.Text,
            NSPasteboardTypeFileUrl => DataFormat.File,
            NSPasteboardTypePng => DataFormat.Bitmap,
            _ when isTextFormat(nativeFormat) => DataFormat.FromSystemName<string>(nativeFormat, AppPrefix),
            _ => DataFormat.FromSystemName<byte[]>(nativeFormat, AppPrefix)
        };

    public static string ToNativeFormat(DataFormat format)
    {
        if (DataFormat.Text.Equals(format))
            return NSPasteboardTypeString;

        if (DataFormat.File.Equals(format))
            return NSPasteboardTypeFileUrl;

        if (DataFormat.Bitmap.Equals(format))
            return NSPasteboardTypePng;

        return format.ToSystemName(AppPrefix);
    }
}
