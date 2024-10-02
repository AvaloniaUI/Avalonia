using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Controls.Platform;
using Avalonia.Threading;

namespace Avalonia.LinuxFramebuffer;

internal unsafe class EpollDispatcherImpl : IControlledDispatcherImpl
{
    private readonly ManagedDispatcherImpl.IManagedDispatcherInputProvider _inputProvider;
    private Thread _mainThread;

    [StructLayout(LayoutKind.Explicit)]
    private struct epoll_data
    {
        [FieldOffset(0)] public IntPtr ptr;
        [FieldOffset(0)] public int fd;
        [FieldOffset(0)] public uint u32;
        [FieldOffset(0)] public ulong u64;
    }

    private const int CLOCK_MONOTONIC = 1;
    private const int EPOLLIN = 1;
    private const int EPOLL_CTL_ADD = 1;
    private const int O_NONBLOCK = 2048;
    private const int O_CLOEXEC = 0x80000;
    private const int EPOLL_CLOEXEC = 0x80000;

    [StructLayout(LayoutKind.Sequential)]
    private struct epoll_event
    {
        public uint events;
        public epoll_data data;
    }

    [DllImport("libc")]
    private extern static int epoll_create1(int flags);

    [DllImport("libc")]
    private extern static int epoll_ctl(int epfd, int op, int fd, ref epoll_event __event);

    [DllImport("libc")]
    private extern static int epoll_wait(int epfd, epoll_event* events, int maxevents, int timeout);

    [DllImport("libc")]
    private extern static int pipe2(int* fds, int flags);

    [DllImport("libc")]
    private extern static IntPtr write(int fd, void* buf, IntPtr count);

    [DllImport("libc")]
    private extern static IntPtr read(int fd, void* buf, IntPtr count);

#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
    struct timespec
    {
        public IntPtr tv_sec;
        public IntPtr tv_nsec;
    }

    struct itimerspec
    {
        public timespec it_interval; // Interval for periodic timer
        public timespec it_value; // Initial expiration
    };
#pragma warning restore CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.

    [DllImport("libc")]
    private extern static int timerfd_create(int clockid, int flags);

    [DllImport("libc")]
    private extern static int timerfd_settime(int fd, int flags, itimerspec* new_value, itimerspec* old_value);

    private enum EventCodes
    {
        Timer = 1,
        Signal = 2
    }

    private int _sigread, _sigwrite;
    private int _timerfd;
    private object _lock = new();
    private bool _signaled;
    private bool _wakeupRequested;
    private TimeSpan? _nextTimer;
    private int _epoll;
    private Stopwatch _clock = Stopwatch.StartNew();

    public EpollDispatcherImpl(ManagedDispatcherImpl.IManagedDispatcherInputProvider inputProvider)
    {
        _inputProvider = inputProvider;

        _mainThread = Thread.CurrentThread;

        _epoll = epoll_create1(EPOLL_CLOEXEC);
        if (_epoll == -1)
            throw new Win32Exception("epoll_create1 failed");

        var fds = stackalloc int[2];
        pipe2(fds, O_NONBLOCK | O_CLOEXEC);
        _sigread = fds[0];
        _sigwrite = fds[1];

        var ev = new epoll_event
        {
            events = EPOLLIN,
            data = { u32 = (int)EventCodes.Signal }
        };
        if (epoll_ctl(_epoll, EPOLL_CTL_ADD, _sigread, ref ev) == -1)
            throw new Win32Exception("Unable to attach signal pipe to epoll");

        _timerfd = timerfd_create(CLOCK_MONOTONIC, O_NONBLOCK | O_CLOEXEC);
        ev.data.u32 = (int)EventCodes.Timer;
        if (epoll_ctl(_epoll, EPOLL_CTL_ADD, _timerfd, ref ev) == -1)
            throw new Win32Exception("Unable to attach timer fd to epoll");
    }

    private bool CheckSignaled()
    {
        lock (_lock)
        {
            if (!_signaled)
                return false;
            _signaled = false;
        }

        Signaled?.Invoke();
        return true;
    }

    public void RunLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var now = _clock.Elapsed;
            if (_nextTimer.HasValue && now > _nextTimer.Value)
            {
                Timer?.Invoke();
                continue;
            }

            if (CheckSignaled())
                continue;

            if (_inputProvider.HasInput)
            {
                _inputProvider.DispatchNextInputEvent();
                continue;
            }

            epoll_event ev;

            if (_nextTimer != null)
            {
                var waitFor = _nextTimer.Value - now;
                if (waitFor.Ticks < 0)
                    continue;

                itimerspec timer = new()
                {
                    it_value = new()
                    {
                        tv_sec = new IntPtr(Math.Min((int)waitFor.TotalSeconds, 100)),
                        tv_nsec = new IntPtr((waitFor.Ticks % 10000000) * 100)
                    }
                };
                timerfd_settime(_timerfd, 0, &timer, null);
            }
            else
            {
                itimerspec none = default;
                timerfd_settime(_timerfd, 0, &none, null);
            }

            epoll_wait(_epoll, &ev, 1, (int)-1);

            // Drain the signaled pipe
            long buf = 0;
            while (read(_sigread, &buf, new IntPtr(8)).ToInt64() > 0)
            {
            }

            // Drain timer fd
            while (read(_timerfd, &buf, new IntPtr(8)).ToInt64() > 0)
            {
            }

            lock (_lock)
                _wakeupRequested = false;

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

    public event Action Signaled;
    public event Action Timer;

    public void UpdateTimer(long? dueTimeInMs)
    {
        _nextTimer = dueTimeInMs == null ? null : TimeSpan.FromMilliseconds(dueTimeInMs.Value);
        if (_nextTimer != null)
            Wakeup();
    }


    public long Now => _clock.ElapsedMilliseconds;
    public bool CanQueryPendingInput => true;

    public bool HasPendingInput => _inputProvider.HasInput;
}
