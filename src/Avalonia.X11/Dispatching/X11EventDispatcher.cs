using System;
using System.Collections.Generic;
using System.Threading;
using static Avalonia.X11.XLib;
namespace Avalonia.X11;

internal class X11EventDispatcher
{
    private readonly AvaloniaX11Platform _platform;
    private readonly IntPtr _display;
    private readonly Dictionary<IntPtr, X11WindowInfo> _windows;

    public delegate void EventHandler(ref XEvent xev);
    public int Fd { get; }

    public X11EventDispatcher(AvaloniaX11Platform platform)
    {
        _platform = platform;
        _display = platform.Display;
        _windows = platform.Windows;
        Fd = XLib.XConnectionNumber(_display);
    }

    public bool IsPending => XPending(_display) != 0;

    public IEventHook? EventHook { get; set; }
    
    public unsafe void DispatchX11Events(CancellationToken cancellationToken)
    {
        while (IsPending)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
                
            XNextEvent(_display, out var xev);
            if(XFilterEvent(ref xev, IntPtr.Zero))
                continue;

            if (xev.type == XEventName.GenericEvent)
                XGetEventData(_display, &xev.GenericEventCookie);
            try
            {
                if (EventHook?.TryHandleEvent(in xev) == true)
                    continue;

                if (xev.type == XEventName.GenericEvent)
                {
                    if (_platform.XI2 != null && _platform.Info.XInputOpcode ==
                        xev.GenericEventCookie.extension)
                    {
                        _platform.XI2.OnEvent((XIEvent*)xev.GenericEventCookie.data);
                    }
                }
                else if (_windows.TryGetValue(xev.AnyEvent.window, out var windowInfo))
                    windowInfo.EventHandler(ref xev);
            }
            finally
            {
                if (xev.type == XEventName.GenericEvent && xev.GenericEventCookie.data != null)
                    XFreeEventData(_display, &xev.GenericEventCookie);
            }
        }
        Flush();
    }

    public void Flush() => XFlush(_display);

    public interface IEventHook
    {
        bool TryHandleEvent(in XEvent evt);
    }
}
