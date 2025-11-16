using System.IO;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using Avalonia.Browser.Interop;
using Avalonia.Browser.Storage;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace Avalonia.Browser;

internal static class BrowserDataTransferHelper
{
    public static DataFormat[] GetReadableItemFormats(JSObject readableDataItem /* JS type: ReadableDataItem */)
    {
        var formatStrings = InputHelper.GetReadableDataItemFormats(readableDataItem);
        var formats = new DataFormat[formatStrings.Length];
        var hasSupportedImage = false;
        for (var i = 0; i < formatStrings.Length; ++i)
        {
            var formatString = formatStrings[i];
            hasSupportedImage = formatString is "image/png";
            formats[i] = BrowserDataFormatHelper.ToDataFormat(formatString);
        }

        if (hasSupportedImage)
            formats = [..formats, DataFormat.Bitmap];

        return formats;
    }

    public static object? TryGetValue(JSObject? readableDataValue  /* JS type: ReadableDataValue */, DataFormat format)
    {
        object? data = readableDataValue?.GetPropertyAsString("type") switch
        {
            "string" => readableDataValue.GetPropertyAsString("value"),
            "bytes" => readableDataValue.GetPropertyAsByteArray("value"),
            "file" => readableDataValue.GetPropertyAsJSObject("value") is { } jsFile ? new JSStorageFile(jsFile) : null,
            _ => null
        };

        if (data is null)
            return null;

        if (DataFormat.Text.Equals(format))
            return data as string;

        if (DataFormat.File.Equals(format))
            return data as IStorageItem;

        if (format is DataFormat<string>)
        {
            return data switch
            {
                string text => text,
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                _ => null
            };
        }

        if (DataFormat.Bitmap.Equals(format))
        {
            if (data is byte[] bytes)
            {
                using var stream = new MemoryStream(bytes);
                return new Bitmap(stream);
            }

            return null;
        }

        if (format is DataFormat<byte[]>)
        {
            return data switch
            {
                byte[] bytes => bytes,
                string text => Encoding.UTF8.GetBytes(text),
                _ => null
            };
        }

        return null;
    }
}
