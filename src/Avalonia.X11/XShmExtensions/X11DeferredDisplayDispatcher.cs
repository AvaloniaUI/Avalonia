using System;
using System.Collections.Generic;
using Avalonia.Logging;
using ShmSeg = System.UInt64;

namespace Avalonia.X11.XShmExtensions;

/// <summary>
/// Drains events from the <c>DeferredDisplay</c> connection used for XShm rendering and dispatches
/// the MIT-SHM completion events. A single instance is shared by every window on the platform, so it
/// also owns the unified <c>shmseg -&gt; image</c> registry used to route completions back to the
/// swapchain that submitted them.
/// </summary>
/// <remarks>
/// All members are expected to be called from the thread that renders (submits XShmPutImage), which is
/// the same thread that drains the completion events. The connection is never touched from the UI event
/// loop, so completions can be awaited by blocking on this connection regardless of which thread renders.
/// </remarks>
internal unsafe class X11DeferredDisplayDispatcher
{
    public X11DeferredDisplayDispatcher(IntPtr deferredDisplay)
    {
        _deferredDisplay = deferredDisplay;

        // From https://www.x.org/releases/X11R7.5/doc/Xext/mit-shm.html
        // > int CompletionType = XShmGetEventBase (display) + ShmCompletion;
        const int ShmCompletion = 0;
        _xShmCompletionType = XShm.XShmGetEventBase(deferredDisplay) + ShmCompletion;
    }

    private readonly IntPtr _deferredDisplay;
    private readonly int _xShmCompletionType;
    private readonly Dictionary<ShmSeg, X11ShmImage> _inFlightImages = new();

    /// <summary>
    /// Registers an image that has just been submitted to the server, so the matching completion event
    /// can later be routed back to its owning swapchain.
    /// </summary>
    public void RegisterInFlight(X11ShmImage image) => _inFlightImages[image.ShmSeg] = image;

    /// <summary>
    /// Dispatches every event currently pending on the connection without blocking.
    /// </summary>
    public void DrainPendingEvents()
    {
        while (XLib.XPending(_deferredDisplay) != 0)
        {
            XLib.XNextEvent(_deferredDisplay, out var ev);
            Dispatch(ref ev);
        }
    }

    /// <summary>
    /// Makes progress on the event queue, blocking for at most one event. If events are already pending
    /// they are all drained; otherwise this blocks until a single event arrives and then drains the rest.
    /// </summary>
    public void DrainEventsBlockingAtMostOnce()
    {
        if (XLib.XPending(_deferredDisplay) != 0)
        {
            DrainPendingEvents();
        }
        else
        {
            XLib.XNextEvent(_deferredDisplay, out var ev);
            Dispatch(ref ev);
            DrainPendingEvents();
        }
    }

    private void Dispatch(ref XEvent ev)
    {
        if ((int)ev.type != _xShmCompletionType)
        {
            // The DeferredDisplay connection only renders; any non-SHM event is unexpected but harmless.
            return;
        }

        ShmSeg shmseg;
        fixed (XEvent* p = &ev)
        {
            shmseg = ((XShmCompletionEvent*)p)->shmseg;
        }

        Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?.Log(this,
            "[X11DeferredDisplayDispatcher] XShmCompletion shmseg={ShmSeg}", shmseg);

        if (_inFlightImages.Remove(shmseg, out var image))
        {
            image.Owner.OnXShmCompletion(image);
        }
        else
        {
            // Unexpected case, all the X11ShmImage should be registered before submission.
            Logger.TryGet(LogEventLevel.Warning, LogArea.X11Platform)?.Log(this,
                "[X11DeferredDisplayDispatcher] Can not find shmseg={ShmSeg} in registry", shmseg);
        }
    }
}
