using System.Collections.Generic;

namespace Avalonia.Input;

/// <summary>
/// Wraps a <see cref="IAsyncDataTransfer"/> into a <see cref="IDataTransfer"/>.
/// </summary>
/// <param name="asyncDataTransfer">The async object to wrap.</param>
/// <remarks>Using this type should be a last resort!</remarks>
internal sealed class AsyncToSyncDataTransfer(IAsyncDataTransfer asyncDataTransfer)
    : IDataTransfer, IAsyncDataTransfer
{
    private readonly IAsyncDataTransfer _asyncDataTransfer = asyncDataTransfer;
    private AsyncToSyncDataTransferItem[]? _items;

    public IReadOnlyList<DataFormat> Formats
        => _asyncDataTransfer.Formats;

    public IReadOnlyList<AsyncToSyncDataTransferItem> Items
        => _items ??= ProvideItems();

    IReadOnlyList<IDataTransferItem> IDataTransfer.Items
        => Items;

    IReadOnlyList<IAsyncDataTransferItem> IAsyncDataTransfer.Items
        => _asyncDataTransfer.Items;

    private AsyncToSyncDataTransferItem[] ProvideItems()
    {
        var asyncItems = _asyncDataTransfer.Items;
        var count = asyncItems.Count;
        var syncItems = new AsyncToSyncDataTransferItem[count];

        for (var i = 0; i < count; ++i)
        {
            var asyncItem = asyncItems[i];
            syncItems[i] = new AsyncToSyncDataTransferItem(asyncItem);
        }

        return syncItems;
    }

    public void Dispose()
        => _asyncDataTransfer.Dispose();
}
