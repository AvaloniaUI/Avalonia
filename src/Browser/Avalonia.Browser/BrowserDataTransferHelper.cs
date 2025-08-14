using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Browser.Storage;
using Avalonia.Input;

namespace Avalonia.Browser;

internal static class BrowserDataTransferHelper
{
    public static DataFormat[] GetReadableItemFormats(JSObject readableDataItem /* JS type: ReadableDataItem */)
    {
        var formatStrings = InputHelper.GetReadableDataItemFormats(readableDataItem);
        var formats = new DataFormat[formatStrings.Length];
        for (var i = 0; i < formatStrings.Length; ++i)
            formats[i] = BrowserDataFormatHelper.ToDataFormat(formatStrings[i]);
        return formats;
    }

    public static object? TryGetValue(JSObject? readableDataValue /* JS type: ReadableDataValue */)
    {
        return readableDataValue?.GetPropertyAsString("type") switch
        {
            "string" => readableDataValue.GetPropertyAsString("value"),
            "bytes" => readableDataValue.GetPropertyAsByteArray("value"),
            "file" => readableDataValue.GetPropertyAsJSObject("value") is { } jsFile ? new JSStorageFile(jsFile) : null,
            _ => null
        };
    }
}
