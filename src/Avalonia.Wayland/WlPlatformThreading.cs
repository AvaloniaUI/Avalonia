using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Threading;

namespace Avalonia.Wayland
{
    internal class WlPlatformThreading : IControlledDispatcherImpl
    {
        private readonly AvaloniaWaylandPlatform _platform;
        private readonly Thread _mainThread;
        private readonly Stopwatch _clock;
        private readonly object _lock;
        private readonly int _displayFd;
        private readonly int _sigRead;
        private readonly int _sigWrite;

        private bool _signaled;
        private bool _wakeupRequested;
        private long? _nextTimer;

        public unsafe WlPlatformThreading(AvaloniaWaylandPlatform platform)
        {
            _platform = platform;
            _mainThread = Thread.CurrentThread;
            _clock = Stopwatch.StartNew();
            _lock = new object();
            _displayFd = platform.WlDisplay.GetFd();
            var fds = stackalloc int[2];
            LibC.pipe2(fds, FileDescriptorFlags.O_NONBLOCK);
            _sigRead = fds[0];
            _sigWrite = fds[1];
        }

        public event Action? Signaled;

        public event Action? Timer;

        public long Now => _clock.ElapsedMilliseconds;

        public bool CanQueryPendingInput => true;

        public bool HasPendingInput => _platform.WlRawEventGrouper.HasJobs;

        public bool CurrentThreadIsLoopThread => Thread.CurrentThread == _mainThread;

        public unsafe void RunLoop(CancellationToken cancellationToken)
        {
            try
            {
                var pollfds = stackalloc pollfd[]
                {
                    new pollfd { fd = _displayFd, events = EpollEvents.EPOLLIN },
                    new pollfd { fd = _sigRead, events = EpollEvents.EPOLLIN }
                };

                while (!cancellationToken.IsCancellationRequested)
                {
                    DispatchTimers();

                    while (_platform.WlDisplay.PrepareRead() != 0)
                        _platform.WlDisplay.DispatchPending();

                    _platform.WlDisplay.Flush();

                    LibC.poll(pollfds, 2, Timeout);

                    if (pollfds[1].revents.HasAllFlags(EpollEvents.EPOLLIN))
                    {
                        DrainPipe();
                        _platform.WlDisplay.CancelRead();
                    }
                    else
                    {
                        if (pollfds[0].revents.HasAllFlags(EpollEvents.EPOLLIN))
                            _platform.WlDisplay.ReadEvents();
                        else
                            _platform.WlDisplay.CancelRead();
                    }

                    while (_platform.WlRawEventGrouper.HasJobs)
                    {
                        CheckSignaled();
                        _platform.WlRawEventGrouper.DispatchNext();
                    }

                    CheckSignaled();
                }
            }
            finally
            {
                _platform.Dispose();
                LibC.close(_sigRead);
                LibC.close(_sigWrite);
            }
        }

        private int Timeout => _nextTimer is null ? -1 : Math.Max(0, (int)(_nextTimer.Value - _clock.ElapsedMilliseconds));

        public void UpdateTimer(long? dueTimeInMs)
        {
            _nextTimer = dueTimeInMs;
            if (_nextTimer is not null)
                Wakeup();
        }

        public void Signal()
        {
            lock (_lock)
            {
                if(_signaled)
                    return;
                _signaled = true;
                Wakeup();
            }
        }

        private unsafe void Wakeup()
        {
            lock (_lock)
            {
                if(_wakeupRequested)
                    return;
                _wakeupRequested = true;
                var buf = 0;
                LibC.write(_sigWrite, new IntPtr(&buf), 1);
            }
        }

        private void DispatchTimers()
        {
            if (_clock.ElapsedMilliseconds >= _nextTimer)
                Timer?.Invoke();
        }

        private unsafe void DrainPipe()
        {
            lock (_lock)
            {
                var buffer = 0;
                while (LibC.read(_sigRead, new IntPtr(&buffer), 4) > 0) { }
                _wakeupRequested = false;
            }
        }

        private void CheckSignaled()
        {
            lock (_lock)
            {
                if (!_signaled)
                    return;
                _signaled = false;
            }

            Signaled?.Invoke();
        }
    }
}
