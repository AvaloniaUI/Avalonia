using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using Avalonia.Logging;
using Avalonia.Threading;
using static Avalonia.X11.Interop.Glib;
namespace Avalonia.X11.Dispatching;

/// <summary>
/// Platform-agnostic GLib (GMainLoop/GSource) based dispatcher implementation. It maps Avalonia's dispatcher model
/// onto GLib so Avalonia code can share a GLib main loop with GLib/GTK based libraries on the UI thread.
/// It owns no platform event source by itself; backends derive from it to attach their own (e.g. the X11 socket).
/// </summary>
internal abstract class GlibDispatcherImplBase :
    IDispatcherImplWithExplicitBackgroundProcessing,
    IControlledDispatcherImpl
{
    /*
        GLib priorities and Avalonia priorities are a bit different. Avalonia follows the WPF model when there
            are "background" and "foreground" priority groups. Foreground jobs are executed before any user input processing,
            background jobs are executed strictly after user input processing.

        GLib has numeric priorities that are used in the following way:
        -100    G_PRIORITY_HIGH - "high" priority sources, not really used by GLib/GTK
        0       G_PRIORITY_DEFAULT - polling X11 events (GTK) and default value for g_timeout_add
        100     G_PRIORITY_HIGH_IDLE without a clear definition, used as an anchor value of sorts
        110     Resize/layout operations (GTK)
        120     Render operations (GTK)
        200     G_PRIORITY_DEFAULT_IDLE - "idle" priority sources

        So, unlike Avalonia, GTK puts way higher priority on input processing, then does resize/layout/render

        So, to map our model to GLib we do the following:
        - foreground jobs (including grouped user events) are executed with (-1) priority (_before_ any normal GLib jobs)
        - the platform event source is polled with G_PRIORITY_DEFAULT, all events are read until the socket is empty,
            we also group input events at that stage (this matches our epoll-based dispatcher)
        - background jobs are executed with G_PRIORITY_DEFAULT_IDLE, so they would have lower priority than GTK
            foreground jobs

        Unfortunately we can't detect if there are pending _non-idle_ GLib jobs using g_main_context_pending, since
        - g_main_context_pending doesn't accept max_priority argument
        - even if it did, that would still involve a syscall to the kernel to poll for fds anyway

        So we just report that we don't support pending input query and let the dispatcher to
        call RequestBackgroundProcessing every time, which results in g_idle_add call for every background job.
        Background jobs are expected to be relatively expensive to execute since on Windows
        MsgWaitForMultipleObjectsEx results isn't really free too.

        For signaling (aka waking up dispatcher for processing _high_ priority jobs we are using
        g_idle_add_full with (-1) priority. While the naming suggests that it would enqueue an idle job,
        it actually adds an always-triggered source that would be called before other sources with lower priority.

        For timers we are using a simple g_timeout_add_full and discard the previous one when dispatcher requests
        an update

        Since GLib dispatches event sources in batches, we force-check for "signaled" flag to run high-prio jobs
        whenever we get control back from GLib. We can still occasionally get GTK code to run before high-prio
        Avalonia-jobs, but that should be fine since the point is to keep Avalonia-based jobs ordered properly
        and to not have our low-priority jobs to prevent GLib-based code from running its own "foreground" jobs

        Another implementation note here is that GLib (just as any other C library) is NOT aware of C# exceptions,
        so we are NOT allowed to have exceptions to escape native->managed call boundary. So we have exception handlers
        that try to propagate those to the nearest run loop frame that was initiated by Avalonia.

        If there is no such frame, we have no choice but to log/swallow those
     */

    // Note that we can't use g_main_context_is_owner outside a run loop, since context doesn't really have an
    // inherent owner when run loop is not running and the context isn't explicitly "locked", so we just assume that
    // the app author is initializing Avalonia on the intended UI thread and won't migrate the default run loop
    // to a different thread
    private readonly Thread _mainThread = Thread.CurrentThread;

    private readonly Action<Exception>? _externalExceptionLogger;
    private bool _signaled;
    private bool _signaledSourceAdded;
    private readonly object _signalLock = new();
    private readonly Stack<ManagedLoopFrame> _runLoopStack = new();

    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private uint? _glibTimerSourceTag;

    protected GlibDispatcherImplBase(Action<Exception>? externalExceptionLogger)
    {
        _externalExceptionLogger = externalExceptionLogger;
    }

    public bool CurrentThreadIsLoopThread => _mainThread == Thread.CurrentThread;

    public event Action? Signaled;
    public void Signal()
    {
        lock (_signalLock)
        {
            if(_signaled)
                return;
            _signaled = true;
            if(_signaledSourceAdded)
                return;
            _signaledSourceAdded = true;
        }
        g_idle_add_full(G_PRIORITY_DEFAULT - 1, SignalSourceCallback);
    }

    protected void CheckSignaled()
    {
        lock (_signalLock)
        {
            if (!_signaled)
                return;
            _signaled = false;
        }

        try
        {
            Signaled?.Invoke();
        }
        catch (Exception e)
        {
            HandleException(e);
        }
        Flush();
    }

    private bool SignalSourceCallback()
    {
        lock (_signalLock)
        {
            _signaledSourceAdded = false;
        }
        CheckSignaled();
        return false;
    }

    public event Action? Timer;
    public long Now => _stopwatch.ElapsedMilliseconds;

    public void UpdateTimer(long? dueTimeInMs)
    {
        if (_glibTimerSourceTag.HasValue)
        {
            g_source_remove(_glibTimerSourceTag.Value);
            _glibTimerSourceTag = null;
        }

        if (dueTimeInMs == null)
            return;

        var interval = (uint)Math.Max(0, (int)Math.Min(int.MaxValue, dueTimeInMs.Value - Now));
        _glibTimerSourceTag = g_timeout_add_once(interval, TimerCallback);
    }

    private void TimerCallback()
    {
        try
        {
            Timer?.Invoke();
        }
        catch (Exception e)
        {
            HandleException(e);
        }
        Flush();
    }

    public event Action? ReadyForBackgroundProcessing;

    public void RequestBackgroundProcessing() =>
        g_idle_add_once(() => ReadyForBackgroundProcessing?.Invoke());

    public virtual bool CanQueryPendingInput => false;
    public virtual bool HasPendingInput => false;

    /// <summary>
    /// Flush any pending output to the platform event source after Avalonia jobs ran. Called whenever control
    /// returns to GLib from a signaled/timer callback. The base implementation does nothing; backends that own a
    /// socket (e.g. X11) override it to flush their connection.
    /// </summary>
    protected virtual void Flush()
    {
    }

    /// <summary>The cancellation token of the innermost Avalonia-controlled run loop frame, or None when no frame
    /// is running. Backends use it to stop draining their event source once a loop frame asked to quit.</summary>
    protected CancellationToken CurrentLoopCancellation =>
        _runLoopStack.Count > 0 ? _runLoopStack.Peek().Cancelled : CancellationToken.None;

    public void RunLoop(CancellationToken token)
    {
        if(token.IsCancellationRequested)
            return;

        using var loop = new ManagedLoopFrame(token);
        _runLoopStack.Push(loop);
        loop.Run();
        _runLoopStack.Pop();

        // Propagate any managed exceptions that we've captured from this frame
        if(loop.Exceptions.Count == 1)
            loop.Exceptions[0].Throw();
        else if (loop.Exceptions.Count > 1)
            throw new AggregateException(loop.Exceptions.Select(x => x.SourceException));
    }

    protected void HandleException(Exception e)
    {
        if (_runLoopStack.Count > 0)
        {
            var frame = _runLoopStack.Peek();
            frame.Exceptions.Add(ExceptionDispatchInfo.Capture(e));
            frame.Stop();
        }
        else
        {
            if (_externalExceptionLogger != null)
                _externalExceptionLogger.Invoke(e);
            else
                Logger.TryGet(LogEventLevel.Error, LogArea.Control)
                    ?.Log("Dispatcher", "Unhandled exception: {exception}", e);
        }
    }

    private class ManagedLoopFrame : IDisposable
    {
        private readonly CancellationToken _externalToken;
        private CancellationTokenSource? _internalTokenSource;
        public CancellationToken Cancelled { get; private set; }

        private readonly IntPtr _loop = g_main_loop_new(IntPtr.Zero, 1);
        public List<ExceptionDispatchInfo> Exceptions { get; } = new();
        private readonly object _destroyLock = new();
        private bool _disposed;

        public ManagedLoopFrame(CancellationToken token)
        {
            _externalToken = token;
        }

        public void Stop()
        {
            try
            {
                _internalTokenSource?.Cancel();
            }
            catch
            {
                // Ignore
            }
        }

        public void Run()
        {
            if (_externalToken.IsCancellationRequested)
                return;
            using (_internalTokenSource = new())
            using (var composite =
                   CancellationTokenSource.CreateLinkedTokenSource(_externalToken, _internalTokenSource.Token))
            {
                Cancelled = composite.Token;
                using (Cancelled.Register(() =>
                       {
                           lock (_destroyLock)
                           {
                               if (_disposed)
                                   return;
                               g_main_loop_quit(_loop);
                           }
                       }))
                {
                    g_main_loop_run(_loop);
                }
            }
        }

        public void Dispose()
        {
            lock (_destroyLock)
            {
                if(_disposed)
                    return;
                _disposed = true;
                g_main_loop_unref(_loop);
            }
        }
    }
}
