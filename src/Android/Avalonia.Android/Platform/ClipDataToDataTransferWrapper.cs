using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Avalonia.Input.Platform;

namespace Avalonia.Android.Platform;

/// <summary>
/// Wraps a <see cref="ClipData"/> into a <see cref="IDataTransfer"/>.
/// </summary>
/// <param name="clipData">The clip data.</param>
/// <param name="context">The application context.</param>
internal sealed class ClipDataToDataTransferWrapper(ClipData clipData, Context? context)
    : IDataTransfer
{
    private readonly ClipData _clipData = clipData;
    private DataFormat[]? _formats;
    private ClipDataItemToDataTransferItemWrapper[]? _items;

    public Context? Context { get; } = context;

    public DataFormat[] Formats
        => _formats ??= _clipData.Description?.GetDataFormats() ?? [];

    private ClipDataItemToDataTransferItemWrapper[] Items
    {
        get
        {
            return _items ??= GetItemsCore();

            ClipDataItemToDataTransferItemWrapper[] GetItemsCore()
            {
                var count = _clipData.ItemCount;
                var items = new ClipDataItemToDataTransferItemWrapper[count];
                for (var i = 0; i < count; ++i)
                    items[i] = new ClipDataItemToDataTransferItemWrapper(_clipData.GetItemAt(i)!, this);
                return items;
            }
        }
    }

    IEnumerable<DataFormat> IDataTransfer.GetFormats()
        => Formats;

    public IEnumerable<IDataTransferItem> GetItems(IEnumerable<DataFormat>? formats = null)
    {
        if (formats is null)
            return Items;

        var formatArray = formats as DataFormat[] ?? formats.ToArray();
        if (formatArray.Length == 0)
            return [];

        // All items share the same formats
        if (Formats.AsSpan().IndexOfAny(formatArray.AsSpan()) < 0)
            return [];

        return Items;
    }

    public void Dispose()
    {
    }
}
