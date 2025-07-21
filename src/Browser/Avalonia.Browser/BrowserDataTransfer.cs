using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Input.Platform;

namespace Avalonia.Browser;

/// <summary>
/// Wraps an array of ReadableClipboardItem (a custom type defined in input.ts) into a <see cref="IDataTransfer"/>.
/// </summary>
/// <param name="jsItems">The array of ReadableClipboardItem objects.</param>
internal sealed class BrowserDataTransfer(JSObject jsItems) : IDataTransfer
{
    private readonly JSObject _jsItems = jsItems; // JS type: ReadableClipboardItem[]
    private BrowserDataTransferItem[]? _items;
    private DataFormat[]? _formats;

    private BrowserDataTransferItem[] Items
    {
        get
        {
            return _items ??= GetItemsCore();

            BrowserDataTransferItem[] GetItemsCore()
            {
                var count = _jsItems.GetPropertyAsInt32("length");
                var items = new BrowserDataTransferItem[count];
                for (var i = 0; i < count; ++i)
                    items[i] = new BrowserDataTransferItem(_jsItems.GetArrayItem(i));
                return items;
            }
        }
    }

    public DataFormat[] Formats
    {
        get
        {
            return _formats ??= GetFormatsCore();

            DataFormat[] GetFormatsCore()
                => Items.SelectMany(item => item.Formats).Distinct().ToArray();
        }
    }

    public IEnumerable<DataFormat> GetFormats()
        => Formats;

    public IEnumerable<IDataTransferItem> GetItems(IEnumerable<DataFormat>? formats = null)
    {
        if (formats is null)
            return Items;

        var formatArray = formats as DataFormat[] ?? formats.ToArray();
        if (formatArray.Length == 0)
            return [];

        return FilterItems();

        IEnumerable<IDataTransferItem> FilterItems()
        {
            foreach (var item in Items)
            {
                if (item.ContainsAny(formatArray))
                    yield return item;
            }
        }
    }

    public void Dispose()
    {
        _jsItems.Dispose();

        if (_items is not null)
        {
            foreach (var item in _items)
                item.Dispose();
        }
    }
}
