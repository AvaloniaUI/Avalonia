using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.Browser;

/// <summary>
/// Wraps an array of ReadableDataItem (a custom type defined in input.ts) into a <see cref="IDataTransfer"/>.
/// Synchronous only - used to read the drag and drop items.
/// </summary>
/// <param name="jsItems">The array of ReadableDataItem objects.</param>
internal sealed class BrowserDragDataTransfer(JSObject jsItems) : PlatformDataTransfer
{
    private readonly JSObject _jsItems = jsItems; // JS type: ReadableDataItem[]

    protected override DataFormat[] ProvideFormats()
        => Items.SelectMany(item => item.Formats).Distinct().ToArray();

    protected override PlatformDataTransferItem[] ProvideItems()
    {
        var count = _jsItems.GetPropertyAsInt32("length");
        var items = new PlatformDataTransferItem[count];
        for (var i = 0; i < count; ++i)
            items[i] = new BrowserDragDataTransferItem(_jsItems.GetArrayItem(i));
        return items;
    }

    public override void Dispose()
    {
        _jsItems.Dispose();

        if (AreItemsInitialized)
        {
            foreach (var item in Items)
                ((BrowserDragDataTransferItem)item).Dispose();
        }
    }
}
