using Avalonia.Input;

namespace Avalonia.Android.Platform;

internal static class AndroidDataFormatHelper
{
    private const string MimeTypeTextPlain = "text/plain";
    private const string MimeTypeTextUriList = "text/uri-list";
    private const string AppPrefix = "application/avn-fmt.";

    public static DataFormat MimeTypeToDataFormat(string mimeType)
    {
        if (mimeType == MimeTypeTextPlain)
            return DataFormat.Text;

        if (mimeType == MimeTypeTextUriList)
            return DataFormat.File;

        return DataFormat.FromSystemName(mimeType, AppPrefix);
    }

    public static string DataFormatToMimeType(DataFormat format)
    {
        if (DataFormat.Text.Equals(format))
            return MimeTypeTextPlain;

        if (DataFormat.File.Equals(format))
            return MimeTypeTextUriList;

        return format.ToSystemName(AppPrefix);
    }

}
