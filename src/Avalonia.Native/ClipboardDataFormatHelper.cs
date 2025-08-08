#nullable enable

using Avalonia.Input;
using Avalonia.Native.Interop;

namespace Avalonia.Native;

internal static class ClipboardDataFormatHelper
{
    // TODO hide native types behind IAvnClipboard abstraction, so managed side won't depend on macOS.
    private const string NSPasteboardTypeString = "public.utf8-plain-text";
    private const string NSPasteboardTypeFileUrl = "public.file-url";
    private const string AppPrefix = "net.avaloniaui.app.uti.";

    public static DataFormat[] ToDataFormats(IAvnStringArray? nativeFormats)
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
            results[c] = ToDataFormat(nativeFormat.String);
        }

        return results;
    }

    public static DataFormat ToDataFormat(string nativeFormat)
        => nativeFormat switch
        {
            NSPasteboardTypeString => DataFormat.Text,
            NSPasteboardTypeFileUrl => DataFormat.File,
            _ => DataFormat.FromSystemName(nativeFormat, AppPrefix)
        };

    public static string ToNativeFormat(DataFormat format)
    {
        if (format.Equals(DataFormat.Text))
            return NSPasteboardTypeString;

        if (format.Equals(DataFormat.File))
            return NSPasteboardTypeFileUrl;

        return format.ToSystemName(AppPrefix);
    }
}
