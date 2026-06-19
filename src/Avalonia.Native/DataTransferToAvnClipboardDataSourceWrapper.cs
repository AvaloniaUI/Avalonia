using System;
using System.Linq;
using Avalonia.Input;
using Avalonia.Native.Interop;

namespace Avalonia.Native;

/// <summary>
/// Wraps a <see cref="IDataTransfer"/> into a <see cref="IAvnClipboardDataSource"/>.
/// This class is called by native code.
/// </summary>
/// <param name="dataTransfer">The data transfer object to wrap.</param>
internal sealed class DataTransferToAvnClipboardDataSourceWrapper(IDataTransfer dataTransfer)
    : NativeOwned, IAvnClipboardDataSource
{
    private IDataTransfer? _dataTransfer = dataTransfer;
    private DataTransferItemToAvnClipboardDataItemWrapper[]? _items;

    private IDataTransfer DataTransfer
        => _dataTransfer ?? throw new ObjectDisposedException(nameof(DataTransferToAvnClipboardDataSourceWrapper));

    private DataTransferItemToAvnClipboardDataItemWrapper[] Items
    {
        get
        {
            if (_items is null)
            {
                _items = GetItemsCore();

                if (_items.Length == 0)
                    Destroyed();
            }

            return _items;

            DataTransferItemToAvnClipboardDataItemWrapper[] GetItemsCore()
                => DataTransfer.Items
                    .Select(static item => new DataTransferItemToAvnClipboardDataItemWrapper(item))
                    .ToArray();
        }
    }

    public int ItemCount
        => Items.Length;

    public IAvnClipboardDataItem GetItem(int index)
        => Items[index];

    protected override void Destroyed()
    {
        _dataTransfer?.Dispose();
        _dataTransfer = null;
    }
}
