using System;
using Avalonia.Input;

namespace Avalonia.X11.Selections.DragDrop;

internal sealed class DragDropDataProvider : SelectionDataProvider
{
    public DragDropDataProvider(AvaloniaX11Platform platform, IAsyncDataTransfer dataTransfer)
        : base(platform, platform.Info.Atoms.XdndSelection)
    {
        DataTransfer = dataTransfer;
    }

    public new IntPtr GetOwner()
        => base.GetOwner();

    public new void SetOwner(IntPtr window)
        => base.SetOwner(window);

    public override void Dispose()
    {
        DataTransfer?.Dispose();
        DataTransfer = null;
    }
}
