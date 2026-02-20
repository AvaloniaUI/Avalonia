using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Utilities;

namespace Avalonia.Input;

/// <summary>
/// A mutable implementation of <see cref="IDataTransfer"/> and <see cref="IAsyncDataTransfer"/>.
/// </summary>
/// <remarks>
/// While it also implements <see cref="IAsyncDataTransfer"/>, this class always returns data synchronously.
/// For advanced usages, consider implementing <see cref="IAsyncDataTransfer"/> directly.
/// </remarks>
public sealed class DataTransfer : IDataTransfer, IAsyncDataTransfer
{
    private readonly List<DataTransferItem> _items = [];
    private DataFormat[]? _formats;

    /// <inheritdoc cref="IDataTransferItem.Formats" />
    public IReadOnlyList<DataFormat> Formats
    {
        get
        {
            return _formats ??= GetFormatsCore();

            DataFormat[] GetFormatsCore()
                => Items.SelectMany(item => item.Formats).Distinct().ToArray();
        }
    }

    /// <summary>
    /// Gets a list of <see cref="DataTransferItem"/> contained in this object.
    /// </summary>
    public IReadOnlyList<DataTransferItem> Items
        => _items;

    IReadOnlyList<IDataTransferItem> IDataTransfer.Items
        => Items;

    IReadOnlyList<IAsyncDataTransferItem> IAsyncDataTransfer.Items
        => Items;

    /// <summary>
    /// Adds an existing <see cref="DataTransferItem"/> to this object.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Add(DataTransferItem item)
    {
        ThrowHelper.ThrowIfNull(item);

        _formats = null;
        _items.Add(item);
    }

    void IDisposable.Dispose()
    {
    }
}
