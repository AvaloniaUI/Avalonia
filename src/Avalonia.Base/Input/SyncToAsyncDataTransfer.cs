using System.Collections.Generic;

namespace Avalonia.Input;

/// <summary>
/// Wraps a <see cref="IDataTransfer"/> into a <see cref="IAsyncDataTransfer"/>.
/// </summary>
/// <param name="dataTransfer">The sync object to wrap.</param>
internal sealed class SyncToAsyncDataTransfer(IDataTransfer dataTransfer)
    : IDataTransfer, IAsyncDataTransfer
{
    private SyncToAsyncDataTransferItem[]? _items;

    public IReadOnlyList<DataFormat> Formats
        => dataTransfer.Formats;

    public IReadOnlyList<SyncToAsyncDataTransferItem> Items
        => _items ??= ProvideItems();

    IReadOnlyList<IDataTransferItem> IDataTransfer.Items
        => dataTransfer.Items;

    IReadOnlyList<IAsyncDataTransferItem> IAsyncDataTransfer.Items
        => Items;

    private SyncToAsyncDataTransferItem[] ProvideItems()
    {
        var asyncItems = dataTransfer.Items;
        var count = asyncItems.Count;
        var syncItems = new SyncToAsyncDataTransferItem[count];

        for (var i = 0; i < count; ++i)
        {
            var asyncItem = asyncItems[i];
            syncItems[i] = new SyncToAsyncDataTransferItem(asyncItem);
        }

        return syncItems;
    }

    public void Dispose()
        => dataTransfer.Dispose();
}
