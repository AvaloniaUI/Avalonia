using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Threading;
using CoreFoundation;
using Foundation;

namespace Avalonia.iOS;

internal class DispatcherImpl : IDispatcherImplWithExplicitBackgroundProcessing
{
    // CFRunLoopTimerSetNextFireDate docs recommend to "create a repeating timer with an initial
    // firing time in the distant future (or the initial firing time) and a very large repeat
    // interval—on the order of decades or more"
    private const double DistantFutureInterval = (double)50 * 365 * 24 * 3600;
    internal static readonly DispatcherImpl Instance = new();

    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly object _sync = new();
    private readonly IntPtr _timer;
    private readonly IntPtr _mainLoop;
    private readonly IntPtr _mainQueue;
    private Thread? _loopThread;
    private bool _backgroundProcessingRequested, _signaled;

    private unsafe DispatcherImpl()
    {
        _mainLoop = Interop.CFRunLoopGetMain();
        _mainQueue = DispatchQueue.MainQueue.Handle.Handle;

        var observer = Interop.CFRunLoopObserverCreate(IntPtr.Zero,
            Interop.CFOptionFlags.kCFRunLoopAfterWaiting | Interop.CFOptionFlags.kCFRunLoopBeforeSources |
            Interop.CFOptionFlags.kCFRunLoopBeforeWaiting,
            1, 0, &ObserverCallback, IntPtr.Zero);
        Interop.CFRunLoopAddObserver(_mainLoop, observer, Interop.kCFRunLoopDefaultMode);

        _timer = Interop.CFRunLoopTimerCreate(IntPtr.Zero,
            Interop.CFAbsoluteTimeGetCurrent() + DistantFutureInterval,
            DistantFutureInterval, 0, 0, &TimerCallback, IntPtr.Zero);
        Interop.CFRunLoopAddTimer(_mainLoop, _timer, Interop.kCFRunLoopDefaultMode);
    }

    public event Action? Signaled;
    public event Action? Timer;
    public event Action? ReadyForBackgroundProcessing;

    public bool CurrentThreadIsLoopThread
    {
        get
        {
            if (_loopThread != null)
                return Thread.CurrentThread == _loopThread;
            if (!NSThread.IsMain)
                return false;
            _loopThread = Thread.CurrentThread;
            return true;
        }
    }

    public unsafe void Signal()
    {
        lock (_sync)
        {
            if (_signaled)
                return;
            _signaled = true;

            Interop.dispatch_async_f(_mainQueue, IntPtr.Zero, &CheckSignaled);
            Interop.CFRunLoopWakeUp(_mainLoop);
        }
    }

    public void UpdateTimer(long? dueTimeInMs)
    {
        var ms = dueTimeInMs == null ? -1 : (int)Math.Min(int.MaxValue - 10, Math.Max(1, dueTimeInMs.Value - Now));
        var interval = ms < 0 ? DistantFutureInterval : ((double)ms / 1000);
        Interop.CFRunLoopTimerSetTolerance(_timer, 0);
        Interop.CFRunLoopTimerSetNextFireDate(_timer, Interop.CFAbsoluteTimeGetCurrent() + interval);
    }

    public long Now => _clock.ElapsedMilliseconds;

    public unsafe void RequestBackgroundProcessing()
    {
        if (_backgroundProcessingRequested)
            return;
        _backgroundProcessingRequested = true;
        Interop.dispatch_async_f(_mainQueue, IntPtr.Zero, &WakeUpCallback);
    }

    private void CheckSignaled()
    {
        bool signaled;
        lock (_sync)
        {
            signaled = _signaled;
            _signaled = false;
        }

        if (signaled)
        {
            Signaled?.Invoke();
        }
    }

    [UnmanagedCallersOnly]
    private static void CheckSignaled(IntPtr context)
    {
        Instance.CheckSignaled();
    }

    [UnmanagedCallersOnly]
    private static void WakeUpCallback(IntPtr context)
    {
        
    }

    [UnmanagedCallersOnly]
    private static void ObserverCallback(IntPtr observer, Interop.CFOptionFlags activity, IntPtr info)
    {
        if (activity == Interop.CFOptionFlags.kCFRunLoopBeforeWaiting)
        {
            bool triggerProcessing;
            lock (Instance._sync)
            {
                triggerProcessing = Instance._backgroundProcessingRequested;
                Instance._backgroundProcessingRequested = false;
            }

            if (triggerProcessing) Instance.ReadyForBackgroundProcessing?.Invoke();
        }

        Instance.CheckSignaled();
    }

    [UnmanagedCallersOnly]
    private static void TimerCallback(IntPtr timer, IntPtr info)
    {
        Instance.Timer?.Invoke();
    }
}
