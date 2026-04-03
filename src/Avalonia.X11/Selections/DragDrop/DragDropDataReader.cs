using System;
using System.Diagnostics;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.X11.Selections.DragDrop;

/// <summary>
/// An object used to read values, converted to the correct format, from a Xdnd selection.
/// </summary>
internal sealed class DragDropDataReader(
    X11Atoms atoms,
    IntPtr[] textFormatAtoms,
    DataFormat[] dataFormats,
    IntPtr display,
    IntPtr targetWindow)
    : SelectionDataReader<PlatformDataTransferItem>(atoms, textFormatAtoms, dataFormats)
{
    protected override SelectionReadSession CreateReadSession()
    {
        var eventWaiter = new SynchronousXEventWaiter(display);
        return new SelectionReadSession(display, targetWindow, Atoms.XdndSelection, eventWaiter, Atoms);
    }

    protected override PlatformDataTransferItem CreateSingleItem(DataFormat[] nonFileFormats)
        => new DragDropDataTransferItem(this, nonFileFormats);

    public PlatformDataTransferItem[] CreateItems()
    {
        // Note: this doesn't cause any deadlock, CreateItemsAsync() will always complete synchronously
        // thanks to the SynchronousXEventWaiter used in the SelectionReadSession.
        var task = CreateItemsAsync();
        Debug.Assert(task.IsCompleted);
        return task.GetAwaiter().GetResult();
    }

    public object? TryGet(DataFormat format)
    {
        // Note: this doesn't cause any deadlock, TryGetAsync() will always complete synchronously
        // thanks to the SynchronousXEventWaiter used in the SelectionReadSession.
        var task = TryGetAsync(format);
        Debug.Assert(task.IsCompleted);
        return task.GetAwaiter().GetResult();
    }

    public override void Dispose()
    {
    }
}
