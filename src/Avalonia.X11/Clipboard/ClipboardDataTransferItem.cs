using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.X11.Clipboard;

/// <summary>
/// Implementation of <see cref="IAsyncDataTransferItem"/> for the X11 clipboard.
/// </summary>
/// <param name="reader">The object used to read values.</param>
/// <param name="formats">The formats.</param>
internal sealed class ClipboardDataTransferItem(ClipboardDataReader reader, DataFormat[] formats)
    : PlatformAsyncDataTransferItem
{
    private readonly ClipboardDataReader _reader = reader;
    private readonly DataFormat[] _formats = formats;

    protected override DataFormat[] ProvideFormats()
        => _formats;

    protected override Task<object?> TryGetRawCoreAsync(DataFormat format)
        => _reader.TryGetAsync(format);
}
