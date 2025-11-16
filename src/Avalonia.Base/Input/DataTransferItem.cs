using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Utilities;

namespace Avalonia.Input;

/// <summary>
/// A mutable implementation of <see cref="IDataTransferItem"/> and <see cref="IAsyncDataTransferItem"/>.
/// This class also provides several static methods to easily create a <see cref="DataTransferItem"/> for common usages.
/// </summary>
/// <remarks>
/// While it also implements <see cref="IAsyncDataTransferItem"/>, this class always returns data synchronously.
/// For advanced usages, consider implementing <see cref="IAsyncDataTransferItem"/> directly.
/// </remarks>
public sealed class DataTransferItem : IDataTransferItem, IAsyncDataTransferItem
{
    private Dictionary<DataFormat, DataAccessor>? _accessorByFormat; // used for 2+ formats
    private KeyValuePair<DataFormat, DataAccessor>? _singleItem; // used for the common single format case
    private DataFormat[]? _formats;

    /// <inheritdoc cref="IDataTransferItem.Formats" />
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
    public object? TryGetRaw(DataFormat format)
        => FindAccessor(format)?.GetValue();

    Task<object?> IAsyncDataTransferItem.TryGetRawAsync(DataFormat format)
    {
        try
        {
            return Task.FromResult(TryGetRaw(format));
        }
        catch (Exception ex)
        {
            return Task.FromException<object?>(ex);
        }
    }

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
    public void Set<T>(DataFormat<T> format, T? value)
        where T : class
    {
        ThrowHelper.ThrowIfNull(format);

        if (value is null)
            RemoveCore(format);
        else
            SetCore(format, new DataAccessor(static state => state, value));
    }

    /// <summary>
    /// Sets a value created on demand for a given format.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="format">The format.</param>
    /// <param name="getValue">A function returning the value corresponding to <paramref name="format"/>.</param>
    public void Set<T>(DataFormat<T> format, Func<T?> getValue)
        where T : class
    {
        ThrowHelper.ThrowIfNull(format);
        ThrowHelper.ThrowIfNull(getValue);

        SetCore(format, new DataAccessor(static state => ((Func<T?>)state)(), getValue));
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
    /// Sets the value for the <see cref="DataFormat.Bitmap"/> format.
    /// </summary>
    /// <param name="value">
    /// The value corresponding to the <see cref="DataFormat.Bitmap"/> format.
    /// If null, the format won't be part of the <see cref="DataTransferItem"/>.
    /// </param>
    public void SetBitmap(Bitmap? value)
        => Set(DataFormat.Bitmap, value);

    /// <summary>
    /// Creates a new <see cref="DataTransferItem"/> for a single format with a given value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="format">The format.</param>
    /// <param name="value">
    /// The value corresponding to <paramref name="format"/>.
    /// If null, the format won't be part of the <see cref="DataTransferItem"/>.
    /// </param>
    /// <returns>A <see cref="DataTransferItem"/> instance.</returns>
    public static DataTransferItem Create<T>(DataFormat<T> format, T? value)
        where T : class
    {
        var item = new DataTransferItem();
        item.Set(format, value);
        return item;
    }

    /// <summary>
    /// Creates a new <see cref="DataTransferItem"/> for a single format with a given value created on demand.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="format">The format.</param>
    /// <param name="getValue">A function returning the value corresponding to <paramref name="format"/>.</param>
    /// <returns>A <see cref="DataTransferItem"/> instance.</returns>
    public static DataTransferItem Create<T>(DataFormat<T> format, Func<T?> getValue)
        where T : class
    {
        var item = new DataTransferItem();
        item.Set(format, getValue);
        return item;
    }

    /// <summary>
    /// Creates a new <see cref="DataTransferItem"/> with <see cref="DataFormat.Text"/> as a single format.
    /// </summary>
    /// <param name="value">
    /// The value corresponding to the <see cref="DataFormat.Text"/> format.
    /// If null, the format won't be part of the <see cref="DataTransferItem"/>.
    /// </param>
    /// <returns>A <see cref="DataTransferItem"/> instance.</returns>
    public static DataTransferItem CreateText(string? value)
        => Create(DataFormat.Text, value);

    /// <summary>
    /// Creates a new <see cref="DataTransferItem"/> with <see cref="DataFormat.File"/> as a single format.
    /// </summary>
    /// <param name="value">
    /// The value corresponding to the <see cref="DataFormat.File"/> format.
    /// If null, the format won't be part of the <see cref="DataTransferItem"/>.
    /// </param>
    /// <returns>A <see cref="DataTransferItem"/> instance.</returns>
    public static DataTransferItem CreateFile(IStorageItem? value)
        => Create(DataFormat.File, value);

    private readonly struct DataAccessor(Func<object, object?> getValue, object state)
    {
        public object? GetValue()
            => getValue(state);
    }
}

