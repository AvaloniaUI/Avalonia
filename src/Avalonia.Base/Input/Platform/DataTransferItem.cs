using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Avalonia.Input.Platform;

/// <summary>
/// Base implementation of <see cref="IDataTransferItem"/>.
/// This class provides several static methods to easily create a <see cref="DataTransferItem"/> for common usages.
/// </summary>
public abstract class DataTransferItem : IDataTransferItem
{
    /// <inheritdoc />
    public abstract IEnumerable<DataFormat> GetFormats();

    /// <inheritdoc />
    public virtual bool Contains(DataFormat format)
        => GetFormats().Contains(format);

    /// <inheritdoc />
    public abstract Task<object?> TryGetAsync(DataFormat format);

    /// <summary>
    /// Creates a new <see cref="DataTransferItem"/> for a single format with a given value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="format">The format.</param>
    /// <param name="value">The value corresponding to <paramref name="format"/>.</param>
    /// <returns>A <see cref="DataTransferItem"/> instance.</returns>
    public static DataTransferItem Create<T>(DataFormat format, T value)
        => new SimpleDataTransferItem(format, value);

    /// <summary>
    /// Creates a new <see cref="DataTransferItem"/> for a single format,
    /// with its value created synchronously on demand.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="format">The format.</param>
    /// <param name="getValue">A function returning the value corresponding to <paramref name="format"/>.</param>
    /// <returns>A <see cref="DataTransferItem"/> instance.</returns>
    public static DataTransferItem Create<T>(DataFormat format, Func<T> getValue)
        => new AsyncDataTransferItem(
            format,
            static state => Task.FromResult<object?>(((Func<T>)state)()),
            getValue);

    /// <summary>
    /// Creates a new <see cref="DataTransferItem"/> for a single format,
    /// with its value created asynchronously on demand.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="format">The format.</param>
    /// <param name="getValueAsync">A function returning the value corresponding to <paramref name="format"/>.</param>
    /// <returns>A <see cref="DataTransferItem"/> instance.</returns>
    public static DataTransferItem Create<T>(DataFormat format, Func<Task<T>> getValueAsync)
    {
        Func<object, Task<object?>> untypedGetValueAsync = typeof(T) == typeof(object) ?
            static state => ((Func<Task<object?>>)state)() :
            static async state => await ((Func<Task<T>>)state)().ConfigureAwait(false);

        return new AsyncDataTransferItem(format, untypedGetValueAsync, getValueAsync);
    }

    /// <summary>
    /// Creates a new <see cref="DataTransferItem"/> from a given dictionary.
    /// </summary>
    /// <param name="values">A dictionary containing the supported formats with their values.</param>
    /// <returns>A <see cref="DataTransferItem"/> instance.</returns>
    public static DataTransferItem Create(IReadOnlyDictionary<DataFormat, object?> values)
        => new DictionaryDataTransferItem(values);

    private sealed class SimpleDataTransferItem(DataFormat format, object? value)
        : DataTransferItem
    {
        private readonly DataFormat _format = format;
        private readonly object? _value = value;

        public override IEnumerable<DataFormat> GetFormats()
            => [_format];

        public override bool Contains(DataFormat format)
            => _format.Equals(format);

        public override Task<object?> TryGetAsync(DataFormat format)
            => Task.FromResult(_format.Equals(format) ? _value : null);
    }

    private sealed class AsyncDataTransferItem(DataFormat format, Func<object, Task<object?>> getValueAsync, object state)
        : DataTransferItem
    {
        private readonly DataFormat _format = format;
        private readonly Func<object, Task<object?>> _getValueAsync = getValueAsync;
        private readonly object _state = state;
        private Task<object?>? _cachedResult;

        public override IEnumerable<DataFormat> GetFormats()
            => [_format];

        public override bool Contains(DataFormat format)
            => _format.Equals(format);

        public override Task<object?> TryGetAsync(DataFormat format)
        {
            if (_format.Equals(format))
                return _cachedResult ??= _getValueAsync(_state);

            return Task.FromResult<object?>(null);
        }
    }

    private sealed class DictionaryDataTransferItem(IReadOnlyDictionary<DataFormat, object?> values)
        : DataTransferItem
    {
        private readonly IReadOnlyDictionary<DataFormat, object?> _values = values;

        public override IEnumerable<DataFormat> GetFormats()
            => _values.Keys;

        public override Task<object?> TryGetAsync(DataFormat format)
        {
            if (!_values.TryGetValue(format, out var value))
                value = null;

            return Task.FromResult(value);
        }
    }
}

