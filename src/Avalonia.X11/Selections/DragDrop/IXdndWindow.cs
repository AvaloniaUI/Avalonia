using System;
using Avalonia.Input;

namespace Avalonia.X11.Selections.DragDrop;

internal interface IXdndWindow
{
    IntPtr Handle { get; }

    IInputRoot? InputRoot { get; }

    Point PointToClient(PixelPoint point);
}
