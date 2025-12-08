using System;
using System.Diagnostics;
using Android.OS;
using Avalonia.Controls.Documents;
using Avalonia.Threading;
using Java.Lang;
using App = Android.App.Application;
using Object = Java.Lang.Object;

namespace Avalonia.Android
{
    internal sealed class AndroidDispatcherImpl : IDispatcherImplWithExplicitBackgroundProcessing,
        IDispatcherImplWithPendingInput
    {
        [ThreadStatic] private static bool? s_isUIThread;
        private readonly Looper _mainLooper;
        private readonly Handler _handler;
        private readonly Runnable _signaler;
        private readonly Runnable _timerSignaler;
        private readonly Runnable _wakeupSignaler;
        private readonly MessageQueue _queue;
        private readonly object _lock = new();
        private bool _signaled;
        private bool _backgroundProcessingRequested;
        

        public AndroidDispatcherImpl()
        {
            _mainLooper = App.Context.MainLooper ??
                          throw new InvalidOperationException(
                              "Application.Context.MainLooper was not expected to be null.");
            if (!CurrentThreadIsLoopThread)
                throw new InvalidOperationException("This class should be instanciated from the UI thread");
            _handler = new Handler(_mainLooper);
            _signaler = new Runnable(OnSignaled);
            _timerSignaler = new Runnable(OnTimer);
            _wakeupSignaler = new Runnable(() => { });
            _queue = Looper.MyQueue();
            Looper.MyQueue().AddIdleHandler(new IdleHandler(this));
            CanQueryPendingInput = OperatingSystem.IsAndroidVersionAtLeast(23);
        }
        
        public event Action? Timer;
        private void OnTimer() => Timer?.Invoke();

        public event Action? Signaled;
        private void OnSignaled()
        {
            lock (_lock)
                _signaled = false;
            Signaled?.Invoke();
        }

        public bool CurrentThreadIsLoopThread
        {
            get
            {
                if (s_isUIThread.HasValue)
                    return s_isUIThread.Value;
                var uiThread = OperatingSystem.IsAndroidVersionAtLeast(23)
                    ? _mainLooper.IsCurrentThread
                    : _mainLooper.Thread.Equals(Java.Lang.Thread.CurrentThread());

                s_isUIThread = uiThread;
                return uiThread;
            }
        }

        public void Signal()
        {
            lock (_lock)
            {
                if(_signaled)
                    return;
                _signaled = true;
                _handler.Post(_signaler);
            }
        }

        readonly Stopwatch _clock = Stopwatch.StartNew();
        public long Now => _clock.ElapsedMilliseconds;
        
        public void UpdateTimer(long? dueTimeInMs)
        {
            _handler.RemoveCallbacks(_timerSignaler);
            if (dueTimeInMs.HasValue)
            {
                var delay = dueTimeInMs.Value - Now;
                if (delay > 0)
                    _handler.PostDelayed(_timerSignaler, delay);
                else
                    _handler.Post(_timerSignaler);
            }
        }

        class IdleHandler : Object, MessageQueue.IIdleHandler
        {
            private readonly AndroidDispatcherImpl _parent;

            public IdleHandler(AndroidDispatcherImpl parent)
            {
                _parent = parent;
            }
            
            public bool QueueIdle()
            {
                _parent.OnIdle();

                return true;
            }
        }

        public event Action? ReadyForBackgroundProcessing;
        
        public void RequestBackgroundProcessing()
        {
            _backgroundProcessingRequested = true;
        }

        void OnIdle()
        {
            tailCall:
            if (_backgroundProcessingRequested)
            {
                _backgroundProcessingRequested = false;
                ReadyForBackgroundProcessing?.Invoke();
            }
            
            if (_backgroundProcessingRequested)
            {
                // Dispatcher requested background processing again, however if the queue is empty and we 
                // just return here, Android's Looper will go to sleep and won't call us again and we'll have
                // "background" jobs not being processed
                // So we need to examine the queue state to prevent that scenario
                
                lock (_lock)
                {
                    // There are higher priority jobs enqueued, we'll be called again
                    if (_signaled)
                        return;
                }
                
                if (CanQueryPendingInput)
                {
                    if (!HasPendingInput)
                        // There are no events in the queue, so if we just return here, Looper will go to sleep,
                        // so just run our logic again
                        goto tailCall;
                    // Nothing to do otherwise, we'll be called again after higher priority events get processed
                }
                else
                {
                    // On this API level we can't check if there is pending input,
                    // so we explicitly wake up the Looper to make sure that it will call idle hooks again
                    // before going to sleep
                    _handler.Post(_wakeupSignaler);
                }
            }
        }

        public bool CanQueryPendingInput { get; }
        
        // See check in ctor
#pragma warning disable CA1416
        public bool HasPendingInput => !_queue.IsIdle;
#pragma warning restore CA1416
    }
}
