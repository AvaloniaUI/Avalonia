using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Utilities;

namespace Avalonia.Input;

/// <summary>
/// A mutable implementation of <see cref="IDataTransferItem"/>.
/// This class also provides several static methods to easily create a <see cref="DataTransferItem"/> for common usages.
/// For advanced usages, consider implementing <see cref="IDataTransferItem"/> directly.
/// </summary>
public sealed class DataTransferItem : IDataTransferItem
{
    private Dictionary<DataFormat, DataAccessor>? _accessorByFormat; // used for 2+ formats
    private KeyValuePair<DataFormat, DataAccessor>? _singleItem; // used for the common single format case
    private DataFormat[]? _formats;

    /// <inheritdoc />
    public IReadOnlyList<DataFormat> Formats
    {
        get
        {
            return _formats ??= ComputeFormats();

            DataFormat[] ComputeFormats()
            {
                if (_accessorByFormat is not null)
                    return _accessorByFormat.Keys.ToArray();
                if (_singleItem is { } singleItem)
                    return [singleItem.Key];
                return [];
            }
        }
    }

    /// <inheritdoc />
    public Task<object?> TryGetAsync(DataFormat format)
        => FindAccessor(format) is { } accessor ? accessor.GetValueAsync() : Task.FromResult<object?>(null);

    private DataAccessor? FindAccessor(DataFormat format)
    {
        if (_accessorByFormat is not null)
            return _accessorByFormat.TryGetValue(format, out var accessor) ? accessor : null;

        if (_singleItem is { } singleItem)
            return singleItem.Value;

        return null;
    }

    /// <summary>
    /// Sets the value for a given format.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="value">
    /// The value corresponding to <paramref name="format"/>.
    /// If null, the format won't be part of the <see cref="DataTransferItem"/>.
    /// </param>
    public void Set<T>(DataFormat format, T value)
    {
        ThrowHelper.ThrowIfNull(format);

        if (value is null)
            RemoveCore(format);
        else
            SetCore(format, new DataAccessor(Task.FromResult!, value));
    }

    /// <summary>
    /// Sets a value created synchronously on demand for a given format.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="format">The format.</param>
    /// <param name="getValue">A function returning the value corresponding to <paramref name="format"/>.</param>
    public void Set<T>(DataFormat format, Func<T> getValue)
    {
        ThrowHelper.ThrowIfNull(format);
        ThrowHelper.ThrowIfNull(getValue);

        SetCore(format, new DataAccessor(GetValueAsync, getValue));

        static Task<object?> GetValueAsync(object state)
        {
            try
            {
                return Task.FromResult<object?>(((Func<T>)state)());
            }
            catch (Exception ex)
            {
                return Task.FromException<object?>(ex);
            }
        }
    }

    /// <summary>
    /// Sets a value created asynchronously on demand for a given format.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="format">The format.</param>
    /// <param name="getValueAsync">A function returning the value corresponding to <paramref name="format"/>.</param>
    public void Set<T>(DataFormat format, Func<Task<T>> getValueAsync)
    {
        ThrowHelper.ThrowIfNull(format);
        ThrowHelper.ThrowIfNull(getValueAsync);

        Func<object, Task<object?>> untypedGetValueAsync = typeof(T) == typeof(object) ?
            static state => ((Func<Task<object?>>)state)() :
            static async state => await ((Func<Task<T>>)state)().ConfigureAwait(false);

        SetCore(format, new DataAccessor(untypedGetValueAsync, getValueAsync));
    }

    private void SetCore(DataFormat format, DataAccessor accessor)
    {
        if (_accessorByFormat is not null)
            _accessorByFormat[format] = accessor;
        else if (_singleItem is { } singleItem && !singleItem.Key.Equals(format))
        {
            _accessorByFormat = new()
            {
                [singleItem.Key] = singleItem.Value,
                [format] = accessor
            };
            _singleItem = null;
        }
        else
            _singleItem = new(format, accessor);

        _formats = null;
    }

    private void RemoveCore(DataFormat format)
    {
        bool removed;

        if (_accessorByFormat is not null)
            removed = _accessorByFormat.Remove(format);
        else if (_singleItem is { } singleItem && singleItem.Key.Equals(format))
        {
            _singleItem = null;
            removed = true;
        }
        else
            removed = false;

        if (removed)
            _formats = null;
    }


    /// <summary>
    /// Sets the value for the <see cref="DataFormat.Text"/> format.
    /// </summary>
    /// <param name="value">
    /// The value corresponding to the <see cref="DataFormat.Text"/> format.
    /// If null, the format won't be part of the <see cref="DataTransferItem"/>.
    /// </param>
    public void SetText(string? value)
        => Set(DataFormat.Text, value);

    /// <summary>
    /// Sets the value for the <see cref="DataFormat.File"/> format.
    /// </summary>
    /// <param name="value">
    /// The value corresponding to the <see cref="DataFormat.File"/> format.
    /// If null, the format won't be part of the <see cref="DataTransferItem"/>.
    /// </param>
    public void SetFile(IStorageItem? value)
        => Set(DataFormat.File, value);

    /// <summary>
    /// Creates a new <see cref="DataTransferItem"/> for a single format with a given value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="format">The format.</param>
    /// <param name="value">The value corresponding to <paramref name="format"/>.</param>
    /// <returns>A <see cref="DataTransferItem"/> instance.</returns>
    public static DataTransferItem Create<T>(DataFormat format, T value)
    {
        var item = new DataTransferItem();
        item.Set(format, value);
        return item;
    }

    private readonly struct DataAccessor(Func<object, Task<object?>> getValueAsync, object state)
    {
        public Task<object?> GetValueAsync()
            => getValueAsync(state);
    }
}

