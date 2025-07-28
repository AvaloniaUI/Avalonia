using System.Collections.Generic;

namespace Avalonia.Input.Platform;

/// <summary>
/// Abstract implementation of <see cref="IDataTransfer"/> used by platform implementations.
/// </summary>
internal abstract class PlatformDataTransfer : IDataTransfer
{
    private DataFormat[]? _formats;
    private IDataTransferItem[]? _items;

    public DataFormat[] Formats
        => _formats ??= ProvideFormats();

    IReadOnlyList<DataFormat> IDataTransfer.Formats
        => Formats;

    protected bool AreFormatsInitialized
        => _formats is not null;

    public IDataTransferItem[] Items
        => _items ??= ProvideItems();

    IReadOnlyList<IDataTransferItem> IDataTransfer.Items
        => Items;

    protected bool AreItemsInitialized
        => _items is not null;

    protected abstract DataFormat[] ProvideFormats();

    protected abstract IDataTransferItem[] ProvideItems();

    public abstract void Dispose();
}
