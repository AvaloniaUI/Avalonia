using System;
using System.Collections.Generic;
using Avalonia.Logging;

namespace Avalonia.X11;

/// <summary>
/// Drains events from the <c>DeferredDisplay</c> connection, which the rendering thread owns and pumps
/// itself (the main UI event loop never reads it). A single instance is shared by every window on the
/// platform. Currently it only routes MIT-SHM completion events, but the draining concept is not
/// SHM-specific.
/// </summary>
/// <remarks>
/// All members are expected to be called from the rendering thread, which both submits work and drains the
/// resulting events off this connection, so completions can be awaited by blocking here regardless of which
/// thread renders.
/// </remarks>
internal unsafe class X11DeferredDisplayDispatcher
{
    public X11DeferredDisplayDispatcher(IntPtr deferredDisplay)
    {
        _deferredDisplay = deferredDisplay;

        // From https://www.x.org/releases/X11R7.5/doc/Xext/mit-shm.html
        // > int CompletionType = XShmGetEventBase (display) + ShmCompletion;
        const int ShmCompletion = 0;
        _xShmCompletionType = XLib.XShmGetEventBase(deferredDisplay) + ShmCompletion;
    }

    private readonly IntPtr _deferredDisplay;
    private readonly int _xShmCompletionType;
    private readonly Dictionary<UIntPtr, Action> _shmCompletionCallbacks = new();

    /// <summary>
    /// Registers a one-shot callback to be invoked when the server signals completion for the given shm
    /// segment. The callback is removed once fired.
    /// </summary>
    public void RegisterForShmCompletion(UIntPtr shmSeg, Action callback) => _shmCompletionCallbacks[shmSeg] = callback;

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

        UIntPtr shmSeg;
        fixed (XEvent* p = &ev)
        {
            shmSeg = ((XShmCompletionEvent*)p)->shmseg;
        }

        Logger.TryGet(LogEventLevel.Debug, LogArea.X11Platform)?.Log(this,
            "[X11DeferredDisplayDispatcher] XShmCompletion shmseg={ShmSeg}", shmSeg);

        if (_shmCompletionCallbacks.Remove(shmSeg, out var callback))
        {
            callback();
        }
        else
        {
            // Unexpected case, all submitted shm segments should be registered before submission.
            Logger.TryGet(LogEventLevel.Warning, LogArea.X11Platform)?.Log(this,
                "[X11DeferredDisplayDispatcher] No completion callback registered for shmseg={ShmSeg}", shmSeg);
        }
    }
}
