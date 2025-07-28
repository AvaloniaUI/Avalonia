using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Utilities;

namespace Avalonia.Input.Platform;

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

    /// <summary>
    /// Creates and adds a new <see cref="IDataTransferItem"/> for a single format with a given value.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="value">The value corresponding to <paramref name="format"/>.</param>
    public void Add<T>(DataFormat format, T value)
        => Add(DataTransferItem.Create(format, value));

    /// <summary>
    /// Creates a new <see cref="IDataTransferItem"/> for a single format,
    /// with its value created synchronously on demand.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="format">The format.</param>
    /// <param name="getValue">A function returning the value corresponding to <paramref name="format"/>.</param>
    public void AddLazy<T>(DataFormat format, Func<T> getValue)
        => Add(DataTransferItem.CreateLazy(format, getValue));

    /// <summary>
    /// Creates and adds a new <see cref="IDataTransferItem"/> for a single format,
    /// with its value created asynchronously on demand.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="format">The format.</param>
    /// <param name="getValueAsync">A function returning the value corresponding to <paramref name="format"/>.</param>
    public void AddAsyncLazy<T>(DataFormat format, Func<Task<T>> getValueAsync)
        => Add(DataTransferItem.CreateAsyncLazy(format, getValueAsync));

    void IDisposable.Dispose()
    {
    }
}
