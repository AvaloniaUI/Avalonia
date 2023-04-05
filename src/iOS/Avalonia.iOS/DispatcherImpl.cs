#nullable enable

using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Threading;
using CoreFoundation;
using Foundation;
using ObjCRuntime;
using CFIndex = System.IntPtr;

namespace Avalonia.iOS;

internal class DispatcherImpl : IDispatcherImplWithExplicitBackgroundProcessing
{
    // CFRunLoopTimerSetNextFireDate docs recommend to "create a repeating timer with an initial
    // firing time in the distant future (or the initial firing time) and a very large repeat
    // interval—on the order of decades or more"
    private const double DistantFutureInterval = (double)50*365*24*3600;
    internal static readonly DispatcherImpl Instance = new();

    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly Action _checkSignaledAction;
    private readonly Action _wakeUpLoopAction;
    private readonly IntPtr _timer;
    private Thread? _loopThread;
    private bool _backgroundProcessingRequested, _signaled;

    private DispatcherImpl()
    {
        _checkSignaledAction = CheckSignaled;
        _wakeUpLoopAction = () =>
        {
            // This is needed to wakeup the loop if we are called from inside of BeforeWait hook
        };

        var observerBlock = new BlockLiteral();
        observerBlock.SetupBlock((Interop.CFRunLoopObserverCallback)ObserverCallback, null);
        var observer = Interop.CFRunLoopObserverCreateWithHandler(IntPtr.Zero,
            Interop.CFOptionFlags.kCFRunLoopAfterWaiting | Interop.CFOptionFlags.kCFRunLoopBeforeSources |
            Interop.CFOptionFlags.kCFRunLoopBeforeWaiting,
            true,
            0,
            ref observerBlock);
        Interop.CFRunLoopAddObserver(CFRunLoop.Main.Handle, observer, Interop.kCFRunLoopCommonModes);

        var timerBlock = new BlockLiteral();
        timerBlock.SetupBlock((Interop.CFRunLoopTimerCallback)TimerCallback, null);
        _timer = Interop.CFRunLoopTimerCreateWithHandler(IntPtr.Zero,
            Interop.CFAbsoluteTimeGetCurrent() + DistantFutureInterval,
            DistantFutureInterval, 0, 0, ref timerBlock);
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

    public void Signal()
    {
        lock (this) {
            if(_signaled)
                return;
            _signaled = true;

            DispatchQueue.MainQueue.DispatchAsync(_checkSignaledAction);
            CFRunLoop.Main.WakeUp();
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
    
    public void RequestBackgroundProcessing()
    {
        if(_backgroundProcessingRequested)
            return;
        _backgroundProcessingRequested = true;
        DispatchQueue.MainQueue.DispatchAsync(_wakeUpLoopAction);
    }

    private void CheckSignaled()
    {
        bool signaled;
        lock (this)
        {
            signaled = _signaled;
            _signaled = false;
        }

        if (signaled)
        {
            Signaled?.Invoke();
        }
    }

    [MonoPInvokeCallback(typeof(Interop.CFRunLoopObserverCallback))]
    private static void ObserverCallback(IntPtr observer, Interop.CFOptionFlags activity)
    {
        if (activity == Interop.CFOptionFlags.kCFRunLoopBeforeWaiting)
        {
            bool triggerProcessing;
            lock (Instance)
            {
                triggerProcessing = Instance._backgroundProcessingRequested;
                Instance._backgroundProcessingRequested = false;
            }

            if (triggerProcessing) Instance.ReadyForBackgroundProcessing?.Invoke();
        }

        Instance.CheckSignaled();
    }

    [MonoPInvokeCallback(typeof(Interop.CFRunLoopTimerCallback))]
    private static void TimerCallback(IntPtr timer)
    {
        Instance.Timer?.Invoke();
    }
}
