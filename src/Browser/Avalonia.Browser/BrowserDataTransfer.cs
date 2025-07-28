using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.Browser;

/// <summary>
/// Wraps an array of ReadableClipboardItem (a custom type defined in input.ts) into a <see cref="IDataTransfer"/>.
/// </summary>
/// <param name="jsItems">The array of ReadableClipboardItem objects.</param>
internal sealed class BrowserDataTransfer(JSObject jsItems) : PlatformDataTransfer
{
    private readonly JSObject _jsItems = jsItems; // JS type: ReadableClipboardItem[]

    protected override DataFormat[] ProvideFormats()
        => Items.SelectMany(item => item.Formats).Distinct().ToArray();

    protected override IDataTransferItem[] ProvideItems()
    {
        var count = _jsItems.GetPropertyAsInt32("length");
        var items = new IDataTransferItem[count];
        for (var i = 0; i < count; ++i)
            items[i] = new BrowserDataTransferItem(_jsItems.GetArrayItem(i));
        return items;
    }

    public override void Dispose()
    {
        _jsItems.Dispose();

        if (AreItemsInitialized)
        {
            foreach (var item in Items)
                ((BrowserDataTransferItem)item).Dispose();
        }
    }
}
