using System.Threading.Tasks;

namespace Avalonia.Input.Platform;

/// <summary>
/// Implementation of <see cref="IClipboard"/>
/// </summary>
internal sealed class Clipboard(IClipboardImpl clipboardImpl) : IClipboard
{
    private readonly IClipboardImpl _clipboardImpl = clipboardImpl;
    private IAsyncDataTransfer? _lastDataTransfer;

    public Task ClearAsync()
    {
        _lastDataTransfer?.Dispose();
        _lastDataTransfer = null;

        return _clipboardImpl.ClearAsync();
    }

    public Task SetDataAsync(IAsyncDataTransfer? dataTransfer)
    {
        if (dataTransfer is null)
            return ClearAsync();

        if (_clipboardImpl is IOwnedClipboardImpl)
            _lastDataTransfer = dataTransfer;

        return _clipboardImpl.SetDataAsync(dataTransfer);
    }

    public Task FlushAsync()
        => _clipboardImpl is IFlushableClipboardImpl flushable ? flushable.FlushAsync() : Task.CompletedTask;

    public Task<IAsyncDataTransfer?> TryGetDataAsync()
        => _clipboardImpl.TryGetDataAsync();

    public async Task<IAsyncDataTransfer?> TryGetInProcessDataAsync()
    {
        if (_lastDataTransfer is null || _clipboardImpl is not IOwnedClipboardImpl ownedClipboardImpl)
            return null;

        if (!await ownedClipboardImpl.IsCurrentOwnerAsync())
            _lastDataTransfer = null;

        return _lastDataTransfer;
    }
}
