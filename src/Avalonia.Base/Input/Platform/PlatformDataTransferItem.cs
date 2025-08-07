using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalonia.Input.Platform;

/// <summary>
/// Abstract implementation of <see cref="IDataTransferItem"/> used by platform implementations.
/// </summary>
/// <remarks>Use this class when the platform can only provide the underlying data synchronously.</remarks>
internal abstract class PlatformDataTransferItem : IDataTransferItem, IAsyncDataTransferItem
{
    private DataFormat[]? _formats;

    public DataFormat[] Formats
        => _formats ??= ProvideFormats();

    IReadOnlyList<DataFormat> IDataTransferItem.Formats
        => Formats;

    IReadOnlyList<DataFormat> IAsyncDataTransferItem.Formats
        => Formats;

    protected abstract DataFormat[] ProvideFormats();

    public bool Contains(DataFormat format)
        => Array.IndexOf(Formats, format) >= 0;

    public object? TryGet(DataFormat format)
        => Contains(format) ? TryGetCore(format) : Task.FromResult<object?>(null);

    public Task<object?> TryGetAsync(DataFormat format)
    {
        if (!Contains(format))
            return Task.FromResult<object?>(null);

        try
        {
            return Task.FromResult(TryGetCore(format));
        }
        catch (Exception ex)
        {
            return Task.FromException<object?>(ex);
        }
    }

    protected abstract object? TryGetCore(DataFormat format);

    public static PlatformDataTransferItem Create(DataFormat format, object value)
        => new SingleFormatItem(format, value);

    private sealed class SingleFormatItem(DataFormat format, object value) : PlatformDataTransferItem
    {
        private readonly DataFormat _format = format;
        private readonly object _value = value;

        protected override DataFormat[] ProvideFormats()
            => [_format];

        protected override object? TryGetCore(DataFormat format)
            => _format.Equals(format) ? _value : null;
    }
}
