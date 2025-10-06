using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.Browser;

/// <summary>
/// Wraps an array of ReadableDataItem (a custom type defined in input.ts) into a <see cref="IAsyncDataTransfer"/>.
/// Asynchronous only - used to read the clipboard.
/// </summary>
/// <param name="jsItems">The array of ReadableDataItem objects.</param>
internal sealed class BrowserClipboardDataTransfer(JSObject jsItems) : PlatformAsyncDataTransfer
{
    private readonly JSObject _jsItems = jsItems; // JS type: ReadableDataItem[]

    protected override DataFormat[] ProvideFormats()
        => Items.SelectMany(item => item.Formats).Distinct().ToArray();

    protected override IAsyncDataTransferItem[] ProvideItems()
    {
        var count = _jsItems.GetPropertyAsInt32("length");
        var items = new IAsyncDataTransferItem[count];
        for (var i = 0; i < count; ++i)
            items[i] = new BrowserClipboardDataTransferItem(_jsItems.GetArrayItem(i));
        return items;
    }

    public override void Dispose()
    {
        _jsItems.Dispose();

        if (AreItemsInitialized)
        {
            foreach (var item in Items)
                ((BrowserClipboardDataTransferItem)item).Dispose();
        }
    }
}
