using System;
using Avalonia.Reactive;
using System.Threading;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Headless
{
    class HeadlessPlatformThreadingInterface : IPlatformThreadingInterface
    {
        public HeadlessPlatformThreadingInterface()
        {
            _thread = Thread.CurrentThread;
        }
        
        private AutoResetEvent _event = new AutoResetEvent(false);
        private Thread _thread;
        private object _lock = new object();
        private DispatcherPriority? _signaledPriority;

        public void RunLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                DispatcherPriority? signaled = null;
                lock (_lock)
                {
                    signaled = _signaledPriority;
                    _signaledPriority = null;
                }
                if(signaled.HasValue)
                    Signaled?.Invoke(signaled);
                WaitHandle.WaitAny(new[] {cancellationToken.WaitHandle, _event}, TimeSpan.FromMilliseconds(20));
            }
        }

        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
        {
            if (interval.TotalMilliseconds < 10)
                interval = TimeSpan.FromMilliseconds(10);

            var stopped = false;
            Timer timer = null;
            timer = new Timer(_ =>
            {
                if (stopped)
                    return;

                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        tick();
                    }
                    finally
                    {
                        if (!stopped)
                            timer.Change(interval, Timeout.InfiniteTimeSpan);
                    }
                });
            },
            null, interval, Timeout.InfiniteTimeSpan);

            return Disposable.Create(() =>
            {
                stopped = true;
                timer.Dispose();
            });
        }

        public void Signal(DispatcherPriority priority)
        {
            lock (_lock)
            {
                if (_signaledPriority == null || _signaledPriority.Value > priority)
                {
                    _signaledPriority = priority;
                }
                _event.Set();
            }
        }

        public bool CurrentThreadIsLoopThread => _thread == Thread.CurrentThread;
        public event Action<DispatcherPriority?> Signaled;
    }
}
