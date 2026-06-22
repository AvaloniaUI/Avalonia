using System.Collections.Generic;

namespace Avalonia.Input.Platform;

/// <summary>
/// Abstract implementation of <see cref="IAsyncDataTransfer"/> used by platform implementations.
/// </summary>
/// <remarks>Use this class when the platform can only provide the underlying data asynchronously.</remarks>
internal abstract class PlatformAsyncDataTransfer : IAsyncDataTransfer
{
    private DataFormat[]? _formats;
    private IAsyncDataTransferItem[]? _items;

    public DataFormat[] Formats
        => _formats ??= ProvideFormats();

    IReadOnlyList<DataFormat> IAsyncDataTransfer.Formats
        => Formats;

    protected bool AreFormatsInitialized
        => _formats is not null;

    public IAsyncDataTransferItem[] Items
        => _items ??= ProvideItems();

    IReadOnlyList<IAsyncDataTransferItem> IAsyncDataTransfer.Items
        => Items;

    protected bool AreItemsInitialized
        => _items is not null;

    protected abstract DataFormat[] ProvideFormats();

    protected abstract IAsyncDataTransferItem[] ProvideItems();

    public abstract void Dispose();
}
