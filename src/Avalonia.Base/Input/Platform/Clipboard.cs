using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Avalonia.Input.Platform;

/// <summary>
/// Implementation of <see cref="IClipboard"/>
/// </summary>
internal sealed class Clipboard(IClipboardImpl clipboardImpl) : IClipboard
{
    private readonly IClipboardImpl _clipboardImpl = clipboardImpl;
    private IDataTransfer? _lastDataTransfer;

    Task<string?> IClipboard.GetTextAsync()
        => this.TryGetTextAsync();

    Task IClipboard.SetTextAsync(string? text)
        => this.SetValueAsync(DataFormat.Text, text);

    public Task ClearAsync()
    {
        _lastDataTransfer?.Dispose();
        _lastDataTransfer = null;

        return _clipboardImpl.ClearAsync();
    }

    Task IClipboard.SetDataObjectAsync(IDataObject data)
        => SetDataAsync(new DataObjectToDataTransferWrapper(data));

    public Task SetDataAsync(IDataTransfer? dataTransfer)
    {
        if (dataTransfer is null)
            return ClearAsync();

        if (_clipboardImpl is IOwnedClipboardImpl)
            _lastDataTransfer = dataTransfer;

        return _clipboardImpl.SetDataAsync(dataTransfer);
    }

    public Task FlushAsync()
        => _clipboardImpl is IFlushableClipboardImpl flushable ? flushable.FlushAsync() : Task.CompletedTask;

    async Task<string[]> IClipboard.GetFormatsAsync()
    {
        var formats = await GetDataFormatsAsync().ConfigureAwait(false);
        return formats.Select(format => format.SystemName).ToArray();
    }

    public Task<DataFormat[]> GetDataFormatsAsync()
        => _clipboardImpl.GetDataFormatsAsync();

    Task<object?> IClipboard.GetDataAsync(string format)
        => this.TryGetValueAsync<object?>(DataFormat.Parse(format));

    public Task<IDataTransfer?> TryGetDataAsync(IEnumerable<DataFormat> formats)
        => _clipboardImpl.TryGetDataAsync(formats);

    async Task<IDataObject?> IClipboard.TryGetInProcessDataObjectAsync()
    {
        var dataObject = await TryGetInProcessDataTransferAsync().ConfigureAwait(false);
        return (dataObject as DataObjectToDataTransferWrapper)?.DataObject;
    }

    public async Task<IDataTransfer?> TryGetInProcessDataTransferAsync()
    {
        if (_lastDataTransfer is null || _clipboardImpl is not IOwnedClipboardImpl ownedClipboardImpl)
            return null;

        if (!await ownedClipboardImpl.IsCurrentOwnerAsync())
            _lastDataTransfer = null;

        return _lastDataTransfer;
    }
}
