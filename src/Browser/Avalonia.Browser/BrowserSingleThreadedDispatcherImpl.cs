using System;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using Avalonia.Browser.Interop;
using Avalonia.Threading;

namespace Avalonia.Browser;

// Dispatcher backend for single-threaded WASM. The browser event loop is the only loop:
// wake-ups are posted as macrotasks and everything runs on the main thread, so no locking is needed.
// Pending input is the browser input ring (see BrowserInputQueue), not navigator.scheduling.isInputPending:
// the latter stays true for as long as the pointer keeps moving and starves low-priority jobs, while
// the ring only reports input that was queued since the last input wake.
internal partial class BrowserSingleThreadedDispatcherImpl : IDispatcherImplWithPendingInput,
    IDispatcherImplWithExplicitBackgroundProcessing
{
    private static BrowserSingleThreadedDispatcherImpl? s_instance;

    private readonly Thread _thread = Thread.CurrentThread;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private bool _signaled;
    private bool _backgroundRequested;
    private bool _timerSet;
    private bool _inInputWake;

    public BrowserSingleThreadedDispatcherImpl()
    {
        s_instance = this;
    }

    public bool CurrentThreadIsLoopThread => Thread.CurrentThread == _thread;

    public long Now => _clock.ElapsedMilliseconds;

    public event Action? Signaled;
    public event Action? Timer;
    public event Action? ReadyForBackgroundProcessing;

    public bool CanQueryPendingInput => true;

    /// <summary>
    /// Input that JS has queued but C# has not decoded yet.
    /// </summary>
    public bool HasPendingInput => BrowserInputQueue.HasPendingInput;

    public void Signal()
    {
        if (_signaled)
            return;
        _signaled = true;

        // RunSignaled is guaranteed to be called at the end of the input wake.
        if (_inInputWake)
            return;

        JsSignal();
    }

    /// <summary>
    /// Runs <paramref name="drainInput"/> followed by a single Signaled call, in the same browser task.
    /// </summary>
    public void RunInputWake(Action drainInput)
    {
        _inInputWake = true;
        try
        {
            drainInput();
        }
        finally
        {
            _inInputWake = false;
        }

        RunSignaled();
    }

    private void RunSignaled()
    {
        _signaled = false;
        Signaled?.Invoke();
    }

    public void RequestBackgroundProcessing()
    {
        if (_backgroundRequested)
            return;
        _backgroundRequested = true;
        JsRequestBackgroundProcessing();
    }

    public void UpdateTimer(long? dueTimeInMs)
    {
        if (_timerSet)
        {
            JsClearTimer();
            _timerSet = false;
        }

        if (dueTimeInMs is { } dueTime)
        {
            JsSetTimer((int)Math.Clamp(dueTime - Now, 0, int.MaxValue));
            _timerSet = true;
        }
    }

    [JSImport("SingleThreadedDispatcherHelper.signal", AvaloniaModule.MainModuleName)]
    private static partial void JsSignal();

    [JSImport("SingleThreadedDispatcherHelper.requestBackgroundProcessing", AvaloniaModule.MainModuleName)]
    private static partial void JsRequestBackgroundProcessing();

    [JSImport("SingleThreadedDispatcherHelper.setTimer", AvaloniaModule.MainModuleName)]
    private static partial void JsSetTimer(int delayMs);

    [JSImport("SingleThreadedDispatcherHelper.clearTimer", AvaloniaModule.MainModuleName)]
    private static partial void JsClearTimer();

    [JSExport]
    public static void OnSignaled()
    {
        s_instance?.RunSignaled();
    }

    [JSExport]
    public static void OnReadyForBackgroundProcessing()
    {
        if (s_instance is not { } impl)
            return;
        impl._backgroundRequested = false;
        impl.ReadyForBackgroundProcessing?.Invoke();
    }

    [JSExport]
    public static void OnTimer()
    {
        if (s_instance is not { } impl)
            return;
        impl._timerSet = false;
        impl.Timer?.Invoke();
    }
}
