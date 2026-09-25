using System;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using Avalonia.Browser.Interop;
using Avalonia.Threading;

namespace Avalonia.Browser;

// Dispatcher backend for single-threaded WASM. The browser event loop is the only loop:
// wake-ups are posted as macrotasks and everything runs on the main thread, so no locking is needed.
// Pending input is deliberately not queried: the browser dispatches input between our tasks on its own,
// and gating low-priority jobs on isInputPending starves them for as long as the pointer keeps moving.
internal partial class BrowserSingleThreadedDispatcherImpl : IDispatcherImplWithExplicitBackgroundProcessing
{
    private static BrowserSingleThreadedDispatcherImpl? s_instance;

    private readonly Thread _thread = Thread.CurrentThread;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private bool _signaled;
    private bool _backgroundRequested;
    private bool _timerSet;

    public BrowserSingleThreadedDispatcherImpl()
    {
        s_instance = this;
    }

    public bool CurrentThreadIsLoopThread => Thread.CurrentThread == _thread;

    public long Now => _clock.ElapsedMilliseconds;

    public event Action? Signaled;
    public event Action? Timer;
    public event Action? ReadyForBackgroundProcessing;

    public void Signal()
    {
        if (_signaled)
            return;
        _signaled = true;
        JsSignal();
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
        if (s_instance is not { } impl)
            return;
        impl._signaled = false;
        impl.Signaled?.Invoke();
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
