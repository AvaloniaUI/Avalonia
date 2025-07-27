using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Input.Platform;

/// <summary>
/// A mutable implementation of <see cref="IDataTransfer"/>.
/// </summary>
public sealed class DataTransfer : IDataTransfer
{
    /// <summary>
    /// Gets the list of <see cref="IDataTransferItem"/> contained in this object.
    /// </summary>
    public List<IDataTransferItem> Items { get; } = [];

    /// <inheritdoc />
    public IEnumerable<DataFormat> GetFormats()
        => Items.SelectMany(item => item.GetFormats()).Distinct();

    IEnumerable<IDataTransferItem> IDataTransfer.GetItems(IEnumerable<DataFormat>? formats)
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

    void IDisposable.Dispose()
    {
    }
}
