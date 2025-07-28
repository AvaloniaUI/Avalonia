using Avalonia.Input.Platform;

namespace Avalonia.iOS.Clipboard;

internal static class ClipboardDataFormatHelper
{
    private const string UTTypeUTF8PlainText = "public.utf8-plain-text";
    private const string UTTypeFileUrl = "public.file-url";
    private const string AppPrefix = "net.avaloniaui.app.uti.";

    public static DataFormat ToDataFormat(string type)
        => type switch
        {
            UTTypeUTF8PlainText => DataFormat.Text,
            UTTypeFileUrl => DataFormat.File,
            _ => DataFormat.FromSystemName(type, AppPrefix)
        };

    public static string ToSystemType(DataFormat format)
    {
        if (format.Equals(DataFormat.Text))
            return UTTypeUTF8PlainText;

        if (format.Equals(DataFormat.File))
            return UTTypeFileUrl;

        return format.ToSystemName(AppPrefix);
    }
}
