using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Threading;
using Avalonia.X11.Dispatching;
using static Avalonia.X11.XLib;

namespace Avalonia.X11
{
    internal unsafe class X11PlatformThreading : IControlledDispatcherImpl, IX11PlatformDispatcher
    {
        private readonly AvaloniaX11Platform _platform;
        private Thread _mainThread = Thread.CurrentThread;

        [StructLayout(LayoutKind.Sequential)]
        public struct fd_set
        {
            public const int FD_SETSIZE = 1024;
            public fixed uint fds[FD_SETSIZE / 32];

            public void Set(int fd)
            {
                var idx = fd / 32;
                if (idx >= FD_SETSIZE / 32)
                    throw new ArgumentOutOfRangeException(nameof(fd));
                var bit = fd % 32;
                fds[idx] |= 1u << bit;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct timeval
        {
            public int tv_sec;
            public int tv_usec;

            public timeval(int milliseconds)
            {
                tv_sec = milliseconds / 1000;
                tv_usec = (milliseconds % 1000) * 1000;
            }
        }

        private const int O_NONBLOCK = 2048;

        [DllImport("libc", EntryPoint = "select")]
        private static extern int select(
            int nfds,
            ref fd_set readfds,
            ref fd_set writefds,
            ref fd_set exceptfds,
            timeval* timeout);

        [DllImport("libc")]
        private extern static int pipe2(int* fds, int flags);

        [DllImport("libc")]
        private extern static IntPtr write(int fd, void* buf, IntPtr count);

        [DllImport("libc")]
        private extern static IntPtr read(int fd, void* buf, IntPtr count);

        private readonly int _sigread, _sigwrite, _x11Fd;
        private object _lock = new object();
        private bool _signaled;
        private bool _wakeupRequested;
        private long? _nextTimer;
        private Stopwatch _clock = Stopwatch.StartNew();
        private readonly X11EventDispatcher _x11Events;

        public X11PlatformThreading(AvaloniaX11Platform platform)
        {
            _platform = platform;
            _x11Events = new X11EventDispatcher(platform);
            _x11Fd = XConnectionNumber(platform.Display);

            var fds = stackalloc int[2];
            pipe2(fds, O_NONBLOCK);
            _sigread = fds[0];
            _sigwrite = fds[1];
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

        public void RunLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var now = _clock.ElapsedMilliseconds;
                if (_nextTimer.HasValue && now > _nextTimer.Value)
                {
                    Timer?.Invoke();
                }

                if (cancellationToken.IsCancellationRequested)
                    return;

                _x11Events.Flush();

                if (!_x11Events.IsPending)
                {
                    now = _clock.ElapsedMilliseconds;
                    if (_nextTimer < now)
                        continue;

                    fd_set readFds = default, writeFds = default, exceptFds = default;
                    readFds.Set(_x11Fd);
                    readFds.Set(_sigread);

                    var timeoutMs = _nextTimer == null ? (int?)null : Math.Max(1, (int)(_nextTimer.Value - now));
                    var timeout = timeoutMs.HasValue ? new timeval(timeoutMs.Value) : default;
                    select(Math.Max(_x11Fd, _sigread) + 1,
                        ref readFds,
                        ref writeFds,
                        ref exceptFds,
                        timeoutMs.HasValue ? &timeout : null);

                    int buf = 0;
                    while (read(_sigread, &buf, new IntPtr(4)).ToInt64() > 0)
                    {
                    }

                    lock (_lock)
                        _wakeupRequested = false;
                }

                if (cancellationToken.IsCancellationRequested)
                    return;

                CheckSignaled();
                _x11Events.DispatchX11Events(cancellationToken);
                while (_platform.EventGrouperDispatchQueue.HasJobs)
                {
                    CheckSignaled();
                    _platform.EventGrouperDispatchQueue.DispatchNext();
                }
            }
        }

        private void Wakeup()
        {
            lock (_lock)
            {
                if (_wakeupRequested)
                    return;
                _wakeupRequested = true;
                int buf = 0;
                write(_sigwrite, &buf, new IntPtr(1));
            }
        }

        public void Signal()
        {
            lock (_lock)
            {
                if (_signaled)
                    return;
                _signaled = true;
                Wakeup();
            }
        }

        public bool CurrentThreadIsLoopThread => Thread.CurrentThread == _mainThread;

        public event Action? Signaled;
        public event Action? Timer;

        public void UpdateTimer(long? dueTimeInMs)
        {
            _nextTimer = dueTimeInMs;
            if (_nextTimer != null)
                Wakeup();
        }

        public long Now => _clock.ElapsedMilliseconds;
        public bool CanQueryPendingInput => true;

        public bool HasPendingInput => _platform.EventGrouperDispatchQueue.HasJobs || _x11Events.IsPending;
        public X11EventDispatcher EventDispatcher => _x11Events;
    }
}
