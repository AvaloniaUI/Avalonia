using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalonia.Input.Platform;

/// <summary>
/// Abstract implementation of <see cref="IDataTransferItem"/> used by platform implementations.
/// </summary>
internal abstract class PlatformDataTransferItem : IDataTransferItem
{
    private DataFormat[]? _formats;

    public DataFormat[] Formats
        => _formats ??= ProvideFormats();

    IReadOnlyList<DataFormat> IDataTransferItem.Formats
        => Formats;

    protected abstract DataFormat[] ProvideFormats();

    public bool Contains(DataFormat format)
        => Array.IndexOf(Formats, format) >= 0;

    public Task<object?> TryGetAsync(DataFormat format)
        => Contains(format) ? TryGetAsyncCore(format) : Task.FromResult<object?>(null);

    protected abstract Task<object?> TryGetAsyncCore(DataFormat format);
}
