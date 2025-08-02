using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Utilities;

namespace Avalonia.Input;

/// <summary>
/// A mutable implementation of <see cref="IDataTransfer"/>.
/// </summary>
public sealed class DataTransfer : IDataTransfer
{
    private readonly List<IDataTransferItem> _items = [];
    private DataFormat[]? _formats;

    /// <inheritdoc />
    public IReadOnlyList<DataFormat> Formats
    {
        get
        {
            return _formats ??= GetFormatsCore();

            DataFormat[] GetFormatsCore()
                => Items.SelectMany(item => item.Formats).Distinct().ToArray();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<IDataTransferItem> Items
        => _items;

    /// <summary>
    /// Adds an existing <see cref="IDataTransferItem"/>.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Add(IDataTransferItem item)
    {
        ThrowHelper.ThrowIfNull(item);

        _formats = null;
        _items.Add(item);
    }

    void IDisposable.Dispose()
    {
    }
}
