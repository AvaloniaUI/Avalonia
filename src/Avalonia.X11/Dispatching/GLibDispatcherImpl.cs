using System;
using System.Threading;
using static Avalonia.X11.Interop.Glib;
namespace Avalonia.X11.Dispatching;

internal class GlibDispatcherImpl : GlibDispatcherImplBase, IX11PlatformDispatcher
{
    private readonly AvaloniaX11Platform _platform;
    private readonly X11EventDispatcher _x11Events;

    public GlibDispatcherImpl(AvaloniaX11Platform platform)
        : base(platform.Options.ExternalGLibMainLoopExceptionLogger)
    {
        _platform = platform;
        _x11Events = new X11EventDispatcher(platform);
        // A synchronous Xlib call can leave an event queued with no data on the fd.
        // Check that queue before GLib sleeps so the event does not need another wakeup.
        AddBufferedUnixFdSource(G_PRIORITY_DEFAULT, _x11Events.Fd,
            () => _x11Events.IsPending, X11SourceCallback, HandleException);
    }

    protected override void Flush() => _x11Events.Flush();

    public override bool HasPendingInput => _platform.EventGrouperDispatchQueue.HasJobs || _x11Events.IsPending;

    private bool X11SourceCallback(int i, GIOCondition gioCondition)
    {
        CheckSignaled();
        var token = CurrentLoopCancellation;
        try
        {
            // Completely drain X11 socket while we are at it
            while (_x11Events.IsPending)
            {
                // If we don't actually drain our X11 socket, GLib _will_ call us again even if
                // we request the run loop to quit
                _x11Events.DispatchX11Events(CancellationToken.None);
                if (!token.IsCancellationRequested)
                {
                    while (_platform.EventGrouperDispatchQueue.HasJobs)
                    {
                        CheckSignaled();
                        _platform.EventGrouperDispatchQueue.DispatchNext();
                    }

                    _x11Events.Flush();
                }
            }
        }
        catch (Exception e)
        {
            HandleException(e);
        }

        return true;
    }

    public X11EventDispatcher EventDispatcher => _x11Events;
}
