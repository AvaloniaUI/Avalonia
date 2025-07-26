using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input.Platform;
using UIKit;

namespace Avalonia.iOS.Clipboard;

internal sealed class PasteboardToDataTransferWrapper(UIPasteboard pasteboard, long changeCount)
    : IDataTransfer
{
    private readonly UIPasteboard _pasteboard = pasteboard;
    private readonly long _changeCount = changeCount;
    private DataFormat[]? _formats;
    private PasteboardItemToDataTransferItemWrapper[]? _items;

    public DataFormat[] Formats
    {
        get
        {
            return _formats ??= GetFormatsCore();

            DataFormat[] GetFormatsCore()
            {
                if (_changeCount != _pasteboard.ChangeCount)
                    throw CreateObjectDisposedException();

                var types = _pasteboard.Types;
                var formats = new DataFormat[types.Length];
                for (var i = 0; i < formats.Length; ++i)
                    formats[i] = ClipboardDataFormatHelper.ToDataFormat(types[i]);
                return formats;
            }
        }
    }

    private PasteboardItemToDataTransferItemWrapper[] Items
    {
        get
        {
            return _items ??= GetItemsCore();

            PasteboardItemToDataTransferItemWrapper[] GetItemsCore()
            {
                if (_changeCount != _pasteboard.ChangeCount)
                    throw CreateObjectDisposedException();

                var pasteboardItems = _pasteboard.Items;
                var items = new PasteboardItemToDataTransferItemWrapper[pasteboardItems.Length];
                for (var i = 0; i < pasteboardItems.Length; ++i)
                    items[i] = new PasteboardItemToDataTransferItemWrapper(pasteboardItems[i]);
                return items;
            }
        }
    }

    private static ObjectDisposedException CreateObjectDisposedException()
        => new(nameof(PasteboardToDataTransferWrapper));

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
    }
}
