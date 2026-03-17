using Avalonia.Input;

namespace Avalonia.X11.Selections.DragDrop;

internal sealed class DragDropDataProvider : SelectionDataProvider
{
    public DragDropDataProvider(AvaloniaX11Platform platform, IAsyncDataTransfer dataTransfer)
        : base(platform, platform.Info.Atoms.XdndSelection)
    {
        DataTransfer = dataTransfer;
    }

    public void SetAsOwner()
        => SetOwner(Window);
}
