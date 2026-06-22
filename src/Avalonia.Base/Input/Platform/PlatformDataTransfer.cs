using System.Collections.Generic;

namespace Avalonia.Input.Platform;

/// <summary>
/// Abstract implementation of <see cref="IDataTransfer"/> used by platform implementations.
/// </summary>
/// <remarks>Use this class when the platform can only provide the underlying data asynchronously.</remarks>
internal abstract class PlatformDataTransfer : IDataTransfer, IAsyncDataTransfer
{
    private DataFormat[]? _formats;
    private PlatformDataTransferItem[]? _items;

    public DataFormat[] Formats
        => _formats ??= ProvideFormats();

    IReadOnlyList<DataFormat> IDataTransfer.Formats
        => Formats;

    IReadOnlyList<DataFormat> IAsyncDataTransfer.Formats
        => Formats;

    protected bool AreFormatsInitialized
        => _formats is not null;

    public PlatformDataTransferItem[] Items
        => _items ??= ProvideItems();

    IReadOnlyList<IDataTransferItem> IDataTransfer.Items
        => Items;

    IReadOnlyList<IAsyncDataTransferItem> IAsyncDataTransfer.Items
        => Items;

    protected bool AreItemsInitialized
        => _items is not null;

    protected abstract DataFormat[] ProvideFormats();

    protected abstract PlatformDataTransferItem[] ProvideItems();

    public abstract void Dispose();
}
