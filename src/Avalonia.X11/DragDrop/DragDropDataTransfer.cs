using System;
using Avalonia.Input;
using Avalonia.Input.Platform;
using static Avalonia.X11.XLib;

namespace Avalonia.X11.DragDrop;

/// <summary>
/// Implementation of <see cref="IDataTransfer"/> for data being dragged into an Avalonia window via Xdnd.
/// </summary>
internal sealed class DragDropDataTransfer(
    IntPtr display,
    IntPtr sourceWindow,
    IntPtr targetWindow,
    IInputRoot inputRoot,
    DataFormat[] dataFormats,
    X11Atoms atoms)
    : PlatformDataTransfer
{
    public IntPtr SourceWindow { get; } = sourceWindow;

    public IInputRoot InputRoot { get; } = inputRoot;

    public Point? LastPosition { get; set; }

    protected override DataFormat[] ProvideFormats()
        => dataFormats;

    protected override PlatformDataTransferItem[] ProvideItems()
    {
        return [];
    }

    public override void Dispose()
    {
        var evt = new XEvent
        {
            ClientMessageEvent = new XClientMessageEvent
            {
                type = XEventName.ClientMessage,
                display = display,
                window = SourceWindow,
                message_type = atoms.XdndFinished,
                format = 32,
                ptr1 = targetWindow,
                ptr2 = 0,
                ptr3 = 0,
                ptr4 = 0,
                ptr5 = 0
            }
        };

        XSendEvent(display, SourceWindow, false, (IntPtr)EventMask.NoEventMask, ref evt);
        XFlush(display);
    }
}
