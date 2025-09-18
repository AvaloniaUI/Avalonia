using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalonia.Input.Platform;

/// <summary>
/// Abstract implementation of <see cref="IAsyncDataTransferItem"/> used by platform implementations.
/// </summary>
/// <remarks>Use this class when the platform can only provide the underlying data asynchronously.</remarks>
internal abstract class PlatformAsyncDataTransferItem : IAsyncDataTransferItem
{
    private DataFormat[]? _formats;

    public DataFormat[] Formats
        => _formats ??= ProvideFormats();

    IReadOnlyList<DataFormat> IAsyncDataTransferItem.Formats
        => Formats;

    protected abstract DataFormat[] ProvideFormats();

    public bool Contains(DataFormat format)
        => Array.IndexOf(Formats, format) >= 0;

    public Task<object?> TryGetRawAsync(DataFormat format)
        => Contains(format) ? TryGetRawCoreAsync(format) : Task.FromResult<object?>(null);

    protected abstract Task<object?> TryGetRawCoreAsync(DataFormat format);
}
