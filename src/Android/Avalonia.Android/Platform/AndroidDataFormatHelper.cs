using Android.Content;
using Avalonia.Input.Platform;

namespace Avalonia.Android.Platform;

internal static class AndroidDataFormatHelper
{
    private const string MimeTypeTextPlain = "text/plain";
    private const string MimeTypeTextUriList = "text/uri-list";

    public static DataFormat[] GetDataFormats(this ClipDescription clipDescription)
    {
        var count = clipDescription.MimeTypeCount;
        if (count == 0)
            return [];

        var formats = new DataFormat[count];

        for (var i = 0; i < count; ++i)
            formats[i] = MimeTypeToDataFormat(clipDescription.GetMimeType(i)!);

        return formats;
    }

    public static DataFormat MimeTypeToDataFormat(string mimeType)
    {
        if (mimeType == MimeTypeTextPlain)
            return DataFormat.Text;

        if (mimeType == MimeTypeTextUriList)
            return DataFormat.File;

        return DataFormat.FromSystemName(mimeType);
    }

    public static string DataFormatToMimeType(DataFormat format)
    {
        if (DataFormat.Text.Equals(format))
            return MimeTypeTextPlain;

        if (DataFormat.File.Equals(format))
            return MimeTypeTextUriList;

        return format.SystemName;
    }

}
