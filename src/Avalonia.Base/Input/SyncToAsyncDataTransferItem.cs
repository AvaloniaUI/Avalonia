using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalonia.Input;

/// <summary>
/// Wraps a <see cref="IDataTransferItem"/> into a <see cref="IAsyncDataTransferItem"/>.
/// </summary>
/// <param name="dataTransferItem">The sync item to wrap.</param>
internal sealed class SyncToAsyncDataTransferItem(IDataTransferItem dataTransferItem)
    : IDataTransferItem, IAsyncDataTransferItem
{
    public IReadOnlyList<DataFormat> Formats
        => dataTransferItem.Formats;

    public object? TryGetRaw(DataFormat format)
        => dataTransferItem.TryGetRaw(format);

    public Task<object?> TryGetRawAsync(DataFormat format)
        => Task.FromResult(dataTransferItem.TryGetRaw(format));
}
