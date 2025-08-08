using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalonia.Input;

/// <summary>
/// Wraps a <see cref="IAsyncDataTransferItem"/> into a <see cref="IDataTransferItem"/>.
/// </summary>
/// <param name="asyncDataTransferItem">The async item to wrap.</param>
/// <remarks>Using this type should be a last resort!</remarks>
internal sealed class AsyncToSyncDataTransferItem(IAsyncDataTransferItem asyncDataTransferItem)
    : IDataTransferItem, IAsyncDataTransferItem
{
    private readonly IAsyncDataTransferItem _asyncDataTransferItem = asyncDataTransferItem;

    public IReadOnlyList<DataFormat> Formats
        => _asyncDataTransferItem.Formats;

    public object? TryGet(DataFormat format)
        => _asyncDataTransferItem.TryGetAsync(format).GetAwaiter().GetResult();

    public Task<object?> TryGetAsync(DataFormat format)
        => _asyncDataTransferItem.TryGetAsync(format);
}
