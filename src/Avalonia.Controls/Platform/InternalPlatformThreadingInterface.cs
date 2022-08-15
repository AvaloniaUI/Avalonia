using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Controls.Platform
{
    [Unstable]
    public class InternalPlatformThreadingInterface : IPlatformThreadingInterface
    {
        public InternalPlatformThreadingInterface()
        {
            TlsCurrentThreadIsLoopThread = true;
        }

        private readonly AutoResetEvent _signaled = new AutoResetEvent(false);


        public void RunLoop(CancellationToken cancellationToken)
        {
            var handles = new[] { _signaled, cancellationToken.WaitHandle };

            while (!cancellationToken.IsCancellationRequested)
            {
                Signaled?.Invoke(null);
                WaitHandle.WaitAny(handles);
            }
        }


        class TimerImpl : IDisposable
        {
            private readonly DispatcherPriority _priority;
            private readonly TimeSpan _interval;
            private readonly Action _tick;
            private Timer? _timer;
            private GCHandle _handle;

            public TimerImpl(DispatcherPriority priority, TimeSpan interval, Action tick)
            {
                _priority = priority;
                _interval = interval;
                _tick = tick;
                _timer = new Timer(OnTimer, null, interval, Timeout.InfiniteTimeSpan);
                _handle = GCHandle.Alloc(_timer);
            }

            private void OnTimer(object? state)
            {
                if (_timer == null)
                    return;
                Dispatcher.UIThread.Post(() =>
                {
                    
                    if (_timer == null)
                        return;
                    _tick();
                    _timer?.Change(_interval, Timeout.InfiniteTimeSpan);
                });
            }


            public void Dispose()
            {
                _handle.Free();
                _timer?.Dispose();
                _timer = null;
            }
        }

        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
        {
            return new TimerImpl(priority, interval, tick);
        }

        public void Signal(DispatcherPriority prio)
        {
            _signaled.Set();
        }

        [ThreadStatic] private static bool TlsCurrentThreadIsLoopThread;

        public bool CurrentThreadIsLoopThread => TlsCurrentThreadIsLoopThread;
        public event Action<DispatcherPriority?>? Signaled;
#pragma warning disable CS0067
        public event Action<TimeSpan>? Tick;
#pragma warning restore CS0067

    }
}
