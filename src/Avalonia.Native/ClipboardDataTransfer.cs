#nullable enable

using System.Collections.Generic;
using System.Linq;
using Avalonia.Input.Platform;

namespace Avalonia.Native;

/// <summary>
/// Implementation of <see cref="IDataTransfer"/> for Avalonia.Native.
/// </summary>
/// <param name="session">
/// The clipboard session.
/// The <see cref="ClipboardDataTransfer"/> assumes ownership over this instance.
/// </param>
internal sealed class ClipboardDataTransfer(ClipboardReadSession session)
    : IDataTransfer
{
    private readonly ClipboardReadSession _session = session;
    private DataFormat[]? _formats;
    private ClipboardDataTransferItem[]? _items;

    private DataFormat[] Formats
    {
        get
        {
            return _formats ??= GetFormatsCore();

            DataFormat[] GetFormatsCore()
            {
                using var formats = _session.GetFormats();
                return ClipboardDataFormatHelper.ToDataFormats(formats);
            }
        }
    }

    private ClipboardDataTransferItem[] Items
    {
        get
        {
            return _items ??= GetItemsCore();

            ClipboardDataTransferItem[] GetItemsCore()
            {
                var itemCount = _session.GetItemCount();
                if (itemCount == 0)
                    return [];

                var items = new ClipboardDataTransferItem[itemCount];

                for (var i = 0; i < itemCount; ++i)
                    items[i] = new ClipboardDataTransferItem(_session, i);

                return items;
            }
        }
    }

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

    public IEnumerable<DataFormat> GetFormats()
        => Formats;

    public void Dispose()
        => _session.Dispose();
}
