using System;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia.Browser.Interop;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.Browser;

/// <summary>
/// Wraps a ReadableDataItem (a custom type defined in input.ts) into a <see cref="IAsyncDataTransferItem"/>.
/// Asynchronous only - used to read a clipboard item.
/// </summary>
/// <param name="readableDataItem">The ReadableDataItem object.</param>
internal sealed class BrowserClipboardDataTransferItem(JSObject readableDataItem)
    : PlatformAsyncDataTransferItem, IDisposable
{
    private readonly JSObject _readableDataItem = readableDataItem; // JS type: ReadableDataItem

    protected override DataFormat[] ProvideFormats()
        => BrowserDataTransferHelper.GetReadableItemFormats(_readableDataItem);

    protected override async Task<object?> TryGetRawCoreAsync(DataFormat format)
    {
        string formatString = BrowserDataFormatHelper.ToBrowserFormat(format);

        var value = await InputHelper.TryGetReadableDataItemValueAsync(_readableDataItem, formatString)
            .ConfigureAwait(false);
        return BrowserDataTransferHelper.TryGetValue(value, format);
    }

    public void Dispose()
        => _readableDataItem.Dispose();
}
