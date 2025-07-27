using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input.Platform;

namespace Avalonia.X11.Clipboard;

/// <summary>
/// Implementation of <see cref="IDataTransfer"/> for the X11 clipboard.
/// </summary>
/// <param name="reader">The object used to read values.</param>
/// <param name="formats">The formats.</param>
/// <param name="items">The items.</param>
/// <remarks>
/// Formats and items are pre-populated because we don't want to do some sync-over-async calls.
/// Note that this does not pre-populate values, which are still retrieved asynchronously on demand.
/// </remarks>
internal sealed class ClipboardDataTransfer(ClipboardDataReader reader, DataFormat[] formats, IDataTransferItem[] items)
    : IDataTransfer
{
    private readonly ClipboardDataReader _reader = reader;
    private readonly DataFormat[] _formats = formats;
    private readonly IDataTransferItem[] _items = items;

    public IEnumerable<DataFormat> GetFormats()
        => _formats;

    public IEnumerable<IDataTransferItem> GetItems(IEnumerable<DataFormat>? formats)
    {
        if (formats is null)
            return _items;

        var formatArray = formats as DataFormat[] ?? formats.ToArray();
        if (formatArray.Length == 0)
            return [];

        return FilterItems();

        IEnumerable<IDataTransferItem> FilterItems()
        {
            foreach (var item in _items)
            {
                foreach (var format in formatArray)
                {
                    if (item.Contains(format))
                    {
                        yield return item;
                        break;
                    }
                }
            }
        }
    }

    public bool Contains(DataFormat format)
        => Array.IndexOf(_formats, format) >= 0;

    public void Dispose()
        => _reader.Dispose();
}
