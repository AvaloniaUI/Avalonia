using System;
using System.Collections.Generic;
using System.Threading;
using static Avalonia.X11.XLib;
namespace Avalonia.X11;

internal class X11EventDispatcher
{
    private readonly AvaloniaX11Platform _platform;
    private readonly IntPtr _display;

    public delegate void EventHandler(ref XEvent xev);
    public int Fd { get; }
    private readonly Dictionary<IntPtr, EventHandler> _eventHandlers;

    public X11EventDispatcher(AvaloniaX11Platform platform)
    {
        _platform = platform;
        _display = platform.Display;
        _eventHandlers = platform.Windows;
        Fd = XLib.XConnectionNumber(_display);
    }

    public bool IsPending => XPending(_display) != 0;
    
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
                if (xev.type == XEventName.GenericEvent)
                {
                    if (_platform.XI2 != null && _platform.Info.XInputOpcode ==
                        xev.GenericEventCookie.extension)
                    {
                        _platform.XI2.OnEvent((XIEvent*)xev.GenericEventCookie.data);
                    }
                }
                else if (_eventHandlers.TryGetValue(xev.AnyEvent.window, out var handler))
                    handler(ref xev);
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
}