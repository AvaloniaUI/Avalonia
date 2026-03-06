using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.X11.Clipboard;

/// <summary>
/// Implementation of <see cref="IAsyncDataTransfer"/> for the X11 clipboard.
/// </summary>
/// <param name="reader">The object used to read values.</param>
/// <param name="formats">The formats.</param>
/// <param name="items">The items.</param>
/// <remarks>
/// Formats and items are pre-populated because we don't want to do some sync-over-async calls.
/// Note that this does not pre-populate values, which are still retrieved asynchronously on demand.
/// </remarks>
internal sealed class ClipboardDataTransfer(
    ClipboardDataReader reader,
    DataFormat[] formats,
    IAsyncDataTransferItem[] items)
    : PlatformAsyncDataTransfer
{
    private readonly ClipboardDataReader _reader = reader;
    private readonly DataFormat[] _formats = formats;
    private readonly IAsyncDataTransferItem[] _items = items;

    protected override DataFormat[] ProvideFormats()
        => _formats;

    protected override IAsyncDataTransferItem[] ProvideItems()
        => _items;

    public override void Dispose()
        => _reader.Dispose();
}
