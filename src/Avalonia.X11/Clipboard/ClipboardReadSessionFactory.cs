using System;
using static Avalonia.X11.XLib;

namespace Avalonia.X11.Clipboard;

internal static class ClipboardReadSessionFactory
{
    public static SelectionReadSession CreateSession(AvaloniaX11Platform platform)
    {
        var window = new EventStreamWindow(platform);
        XSelectInput(platform.Display, window.Handle, new IntPtr((int)XEventMask.PropertyChangeMask));

        return new SelectionReadSession(
            platform.Display,
            window.Handle,
            platform.Info.Atoms.CLIPBOARD,
            window,
            platform.Info.Atoms);
    }
}
