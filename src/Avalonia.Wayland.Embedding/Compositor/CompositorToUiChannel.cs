using System.Collections.Concurrent;
using System.Threading;
using Avalonia.Media;
using Avalonia.Threading;

namespace Avalonia.Wayland.Embedding.Compositor;

/// <summary>
/// The compositor→UI message channel. The compositor thread <see cref="Enqueue"/>s events; the UI thread
/// applies them in <see cref="Drain"/>, which runs from three triggers: the <c>BeforeRender</c> dispatcher
/// post (normal frames), the <c>MediaContext.RenderCore</c> hook (pre-pulse force-pull), and the synchronous
/// roundtrip pump in <see cref="WaylandCompositorWorker"/>. A monotonic signal version + monitor wakes
/// roundtrip waiters on either a push or a completion without losing edges across concurrent waiters.
/// </summary>
internal sealed class CompositorToUiChannel
{
    private readonly ConcurrentQueue<CompositorToUiEvent> _queue = new();
    private readonly object _signalLock = new();
    private long _signalVersion;
    private int _drainScheduled;
    private int _hookRegistered;

    /// <summary>Compositor thread: enqueue an event and wake the UI side (drain + roundtrip waiters).</summary>
    public void Enqueue(CompositorToUiEvent ev)
    {
        _queue.Enqueue(ev);
        ScheduleUiDrain();
        SignalWaiters();
    }

    /// <summary>
    /// Wake any UI thread parked in a synchronous roundtrip. Bumps the signal version under the lock so a
    /// waiter that snapshotted an older version will re-check instead of parking (no lost wakeup).
    /// </summary>
    public void SignalWaiters()
    {
        lock (_signalLock)
        {
            _signalVersion++;
            Monitor.PulseAll(_signalLock);
        }
    }

    /// <summary>UI thread: apply every queued event in order.</summary>
    public void Drain()
    {
        while (_queue.TryDequeue(out var ev))
        {
            try { ev.Apply(); }
            catch { /* TODO(P1): route to Avalonia logging instead of swallowing */ }
        }
    }

    /// <summary>Snapshot the current signal version; pass it to <see cref="WaitForSignal"/> after draining.</summary>
    public long SignalVersion
    {
        get { lock (_signalLock) return _signalVersion; }
    }

    /// <summary>
    /// Park until the next signal, unless one already arrived since <paramref name="sinceVersion"/> was
    /// snapshotted — in which case return immediately so the caller re-drains and re-checks. This closes the
    /// gap between the caller's drain/predicate-check and the wait.
    /// </summary>
    public void WaitForSignal(long sinceVersion)
    {
        lock (_signalLock)
        {
            if (_signalVersion == sinceVersion)
                Monitor.Wait(_signalLock);
        }
    }

    /// <summary>
    /// Pre-pulse force-pull: drain compToUi as the very first action of <c>MediaContext.RenderCore</c>,
    /// in-process (no network round-trip), so this frame's layout sees the latest already-delivered state.
    /// Idempotent.
    /// </summary>
    public void RegisterRenderHook()
    {
        if (Interlocked.Exchange(ref _hookRegistered, 1) == 0)
            MediaContext.BeforeRenderCore += Drain;
    }

    private void ScheduleUiDrain()
    {
        if (Interlocked.Exchange(ref _drainScheduled, 1) != 0)
            return;

        // Resolve the REAL UI dispatcher. TryGetUIThread never fabricates a dispatcher on the calling
        // (compositor) thread the way Dispatcher.UIThread would, which would silently strand the drain.
        var ui = Dispatcher.TryGetUIThread();
        if (ui is null)
        {
            // Avalonia's UI dispatcher isn't up yet; let a later Enqueue (or the render hook, once Avalonia
            // renders its first frame) drain instead. The event stays queued.
            Interlocked.Exchange(ref _drainScheduled, 0);
            return;
        }

        ui.Post(RunScheduledDrain, DispatcherPriority.BeforeRender);
    }

    private void RunScheduledDrain()
    {
        // Reset BEFORE draining: a producer that enqueues after our last dequeue will observe
        // _drainScheduled == 0 and arm a fresh drain, so no event waits for an unrelated frame.
        Interlocked.Exchange(ref _drainScheduled, 0);
        Drain();
        if (!_queue.IsEmpty)
            ScheduleUiDrain();
    }
}
