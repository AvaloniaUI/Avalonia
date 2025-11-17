using System;
using Android.Content;
using Avalonia.Input;

namespace Avalonia.Android.Platform;

internal static class AndroidDataFormatHelper
{
    private const string AppPrefix = "application/avn-fmt.";
    private const string MimeTypeImagePng = "image/png";

    public static DataFormat MimeTypeToDataFormat(string mimeType)
    {
        if (mimeType == ClipDescription.MimetypeTextPlain)
            return DataFormat.Text;

        if (mimeType == ClipDescription.MimetypeTextUrilist)
            return DataFormat.File;

        if (mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return DataFormat.Bitmap;

        if (mimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
            return DataFormat.FromSystemName<string>(mimeType, AppPrefix);

        return DataFormat.FromSystemName<byte[]>(mimeType, AppPrefix);
    }

    public static string DataFormatToMimeType(DataFormat format)
    {
        if (DataFormat.Text.Equals(format))
            return ClipDescription.MimetypeTextPlain;

        if (DataFormat.File.Equals(format))
            return ClipDescription.MimetypeTextUrilist;

        if (DataFormat.Bitmap.Equals(format))
            return MimeTypeImagePng;

        return format.ToSystemName(AppPrefix);
    }

}
