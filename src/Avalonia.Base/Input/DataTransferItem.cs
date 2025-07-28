using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Avalonia.Utilities;

namespace Avalonia.Input;

/// <summary>
/// Simple implementation of <see cref="IDataTransferItem"/>.
/// This class provides several static methods to easily create a <see cref="DataTransferItem"/> for common usages.
/// For advanced usages, consider implementing <see cref="IDataTransferItem"/> directly.
/// </summary>
public sealed class DataTransferItem : IDataTransferItem
{
    private readonly DataFormat[] _formats;
    private readonly Func<object?, Task<object?>> _getValueAsync;
    private readonly object? _state;

    private DataTransferItem(DataFormat format, Func<object?, Task<object?>> getValueAsync, object? state)
    {
        _formats = [format];
        _getValueAsync = getValueAsync;
        _state = state;
    }

    /// <inheritdoc />
    public IReadOnlyList<DataFormat> Formats
        => _formats;

    /// <inheritdoc />
    public Task<object?> TryGetAsync(DataFormat format)
        => _formats[0].Equals(format) ? _getValueAsync(_state) : Task.FromResult<object?>(null);

    /// <summary>
    /// Creates a new <see cref="DataTransferItem"/> for a single format with a given value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="format">The format.</param>
    /// <param name="value">The value corresponding to <paramref name="format"/>.</param>
    /// <returns>A <see cref="DataTransferItem"/> instance.</returns>
    public static DataTransferItem Create<T>(DataFormat format, T value)
    {
        ThrowHelper.ThrowIfNull(format);

        return new DataTransferItem(format, Task.FromResult, value);
    }

    /// <summary>
    /// Creates a new <see cref="DataTransferItem"/> for a single format
    /// with its value created synchronously on demand.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="format">The format.</param>
    /// <param name="getValue">A function returning the value corresponding to <paramref name="format"/>.</param>
    /// <returns>A <see cref="DataTransferItem"/> instance.</returns>
    public static DataTransferItem CreateLazy<T>(DataFormat format, Func<T> getValue)
    {
        ThrowHelper.ThrowIfNull(format);
        ThrowHelper.ThrowIfNull(getValue);

        return new DataTransferItem(
            format,
            static state =>
            {
                try
                {
                    return Task.FromResult<object?>(((Func<T>)state!)());
                }
                catch (Exception ex)
                {
                    return Task.FromException<object?>(ex);
                }
            },
            getValue);
    }

    /// <summary>
    /// Creates a new <see cref="DataTransferItem"/> for a single format,
    /// with its value created asynchronously on demand.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="format">The format.</param>
    /// <param name="getValueAsync">A function returning the value corresponding to <paramref name="format"/>.</param>
    /// <returns>A <see cref="DataTransferItem"/> instance.</returns>
    public static DataTransferItem CreateAsyncLazy<T>(DataFormat format, Func<Task<T>> getValueAsync)
    {
        ThrowHelper.ThrowIfNull(format);
        ThrowHelper.ThrowIfNull(getValueAsync);

        Func<object?, Task<object?>> untypedGetValueAsync = typeof(T) == typeof(object) ?
            static state => ((Func<Task<object?>>)state!)() :
            static async state => await ((Func<Task<T>>)state!)().ConfigureAwait(false);

        return new(format, untypedGetValueAsync, getValueAsync);
    }
}

