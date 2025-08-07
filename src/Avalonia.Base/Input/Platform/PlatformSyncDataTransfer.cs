using System.Collections.Generic;

namespace Avalonia.Input.Platform;

/// <summary>
/// Abstract implementation of <see cref="ISyncDataTransfer"/> used by platform implementations.
/// </summary>
/// <remarks>Use this class when the platform can only provide the underlying data asynchronously.</remarks>
internal abstract class PlatformSyncDataTransfer : ISyncDataTransfer, IAsyncDataTransfer
{
    private DataFormat[]? _formats;
    private PlatformSyncDataTransferItem[]? _items;

    public DataFormat[] Formats
        => _formats ??= ProvideFormats();

    IReadOnlyList<DataFormat> ISyncDataTransfer.Formats
        => Formats;

    IReadOnlyList<DataFormat> IAsyncDataTransfer.Formats
        => Formats;

    protected bool AreFormatsInitialized
        => _formats is not null;

    public PlatformSyncDataTransferItem[] Items
        => _items ??= ProvideItems();

    IReadOnlyList<ISyncDataTransferItem> ISyncDataTransfer.Items
        => Items;

    IReadOnlyList<IAsyncDataTransferItem> IAsyncDataTransfer.Items
        => Items;

    protected bool AreItemsInitialized
        => _items is not null;

    protected abstract DataFormat[] ProvideFormats();

    protected abstract PlatformSyncDataTransferItem[] ProvideItems();

    public abstract void Dispose();
}
