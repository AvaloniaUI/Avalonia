using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.X11.DragDrop;

internal sealed class DragDropDataTransferItem(DataFormat[] formats) : PlatformDataTransferItem
{
    protected override DataFormat[] ProvideFormats()
        => formats;

    protected override object? TryGetRawCore(DataFormat format)
    {
        throw new System.NotImplementedException();
    }
}
