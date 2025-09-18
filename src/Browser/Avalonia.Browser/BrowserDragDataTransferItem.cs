using System;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.Browser;

/// <summary>
/// Wraps a ReadableDataItem (a custom type defined in input.ts) into a <see cref="IDataTransferItem"/>.
/// Synchronous only - used to read a drag and drop item.
/// </summary>
/// <param name="readableDataItem">The ReadableDataItem object.</param>
internal sealed class BrowserDragDataTransferItem(JSObject readableDataItem)
    : PlatformDataTransferItem, IDisposable
{
    private readonly JSObject _readableDataItem = readableDataItem; // JS type: ReadableDataItem

    protected override DataFormat[] ProvideFormats()
        => BrowserDataTransferHelper.GetReadableItemFormats(_readableDataItem);

    protected override object? TryGetRawCore(DataFormat format)
    {
        var formatString = BrowserDataFormatHelper.ToBrowserFormat(format);
        var value = InputHelper.TryGetReadableDataItemValue(_readableDataItem, formatString);
        return BrowserDataTransferHelper.TryGetValue(value, format);
    }

    public void Dispose()
        => _readableDataItem.Dispose();
}
