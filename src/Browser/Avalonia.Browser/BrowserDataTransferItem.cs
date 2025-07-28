using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia.Browser.Interop;
using Avalonia.Browser.Storage;
using Avalonia.Input.Platform;

namespace Avalonia.Browser;

/// <summary>
/// Wraps a ReadableClipboardItem (a custom type defined in input.ts) into a <see cref="IDataTransferItem"/>.
/// </summary>
/// <param name="jsItem">The ReadableClipboardItem object.</param>
internal sealed class BrowserDataTransferItem(JSObject jsItem)
    : PlatformDataTransferItem, IDisposable
{
    private readonly JSObject _jsItem = jsItem; // JS type: ReadableClipboardItem

    protected override DataFormat[] ProvideFormats()
    {
        var formatStrings = InputHelper.GetReadableClipboardItemFormats(_jsItem);
        var formats = new DataFormat[formatStrings.Length];
        for (var i = 0; i < formatStrings.Length; ++i)
            formats[i] = BrowserDataFormatHelper.ToDataFormat(formatStrings[i]);
        return formats;
    }

    protected override async Task<object?> TryGetAsyncCore(DataFormat format)
    {
        var formatString = BrowserDataFormatHelper.ToBrowserFormat(format);
        var value = await InputHelper.TryGetReadableClipboardItemValueAsync(_jsItem, formatString).ConfigureAwait(false);

        return value?.GetPropertyAsString("type") switch
        {
            "string" => value.GetPropertyAsString("value"),
            "bytes" => value.GetPropertyAsByteArray("value"),
            "file" => value.GetPropertyAsJSObject("value") is { } jsFile ? new JSStorageFile(jsFile) : null,
            _ => null
        };
    }

    public void Dispose()
        => _jsItem.Dispose();
}
