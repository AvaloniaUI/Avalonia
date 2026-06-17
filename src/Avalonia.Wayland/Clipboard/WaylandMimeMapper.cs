using Avalonia.Input;

namespace Avalonia.Wayland.Clipboard;

/// <summary>
/// Maps between Avalonia <see cref="DataFormat"/> and MIME type strings used by Wayland's
/// wl_data_device protocol.
/// </summary>
static class WaylandMimeMapper
{
    public const string MimeTextPlainUtf8 = "text/plain;charset=utf-8";
    public const string MimeTextPlain = "text/plain";
    public const string MimeTextUriList = "text/uri-list";
    public const string MimeImagePng = "image/png";
    public const string AppPrefix = "application/x-avalonia-";

    /// <summary>
    /// Returns the MIME type strings that should be advertised for a given <see cref="DataFormat"/>.
    /// For <see cref="DataFormat.Text"/>, both <c>text/plain;charset=utf-8</c> and <c>text/plain</c> are returned.
    /// </summary>
    public static string[] ToMimeTypes(DataFormat format)
    {
        if (DataFormat.Text.Equals(format))
            return [MimeTextPlainUtf8, MimeTextPlain];

        if (DataFormat.File.Equals(format))
            return [MimeTextUriList];

        if (DataFormat.Bitmap.Equals(format))
            return [MimeImagePng];

        return [format.ToSystemName(AppPrefix)];
    }

    /// <summary>
    /// Returns the single preferred MIME type for reading a given <see cref="DataFormat"/>.
    /// </summary>
    public static string ToPreferredMimeType(DataFormat format)
    {
        if (DataFormat.Text.Equals(format))
            return MimeTextPlainUtf8;

        if (DataFormat.File.Equals(format))
            return MimeTextUriList;

        if (DataFormat.Bitmap.Equals(format))
            return MimeImagePng;

        return format.ToSystemName(AppPrefix);
    }

    /// <summary>
    /// Converts a MIME type string to a <see cref="DataFormat"/>, or null if not mappable.
    /// </summary>
    public static DataFormat? FromMimeType(string mimeType)
    {
        if (mimeType is MimeTextPlainUtf8 or MimeTextPlain)
            return DataFormat.Text;

        if (mimeType is MimeTextUriList)
            return DataFormat.File;

        if (mimeType is MimeImagePng)
            return DataFormat.Bitmap;

        return DataFormat.FromSystemName<byte[]>(mimeType, AppPrefix);
    }
}
