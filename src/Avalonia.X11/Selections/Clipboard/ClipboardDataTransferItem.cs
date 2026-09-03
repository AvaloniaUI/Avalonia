using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.X11.Selections.Clipboard;

/// <summary>
/// Implementation of <see cref="IAsyncDataTransferItem"/> for the X11 clipboard.
/// </summary>
/// <param name="reader">The object used to read values.</param>
/// <param name="formats">The formats.</param>
internal sealed class ClipboardDataTransferItem(ClipboardDataReader reader, DataFormat[] formats)
    : PlatformAsyncDataTransferItem
{
    protected override DataFormat[] ProvideFormats()
        => formats;

    protected override Task<object?> TryGetRawCoreAsync(DataFormat format)
        => reader.TryGetAsync(format);
}
