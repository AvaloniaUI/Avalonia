using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Platform;
using Avalonia.Threading;
using static Avalonia.X11.XLib;

namespace Avalonia.X11
{
    unsafe class X11PlatformThreading : IPlatformThreadingInterface
    {
        private readonly AvaloniaX11Platform _platform;
        private readonly IntPtr _display;
        private readonly Dictionary<IntPtr, Action<XEvent>> _eventHandlers;
        private Thread _mainThread;

        [StructLayout(LayoutKind.Explicit)]
        struct epoll_data
        {
            [FieldOffset(0)]
            public IntPtr ptr;
            [FieldOffset(0)]
            public int fd;
            [FieldOffset(0)]
            public uint u32;
            [FieldOffset(0)]
            public ulong u64;
        }

        private const int EPOLLIN = 1;
        private const int EPOLL_CTL_ADD = 1;
        private const int O_NONBLOCK = 2048;
        
        [StructLayout(LayoutKind.Sequential)]
        struct epoll_event
        {
            public uint events;
            public epoll_data data;
        }
        
        [DllImport("libc")]
        extern static int epoll_create1(int size);

        [DllImport("libc")]
        extern static int epoll_ctl(int epfd, int op, int fd, ref epoll_event __event);

        [DllImport("libc")]
        extern static int epoll_wait(int epfd, epoll_event* events, int maxevents, int timeout);

        [DllImport("libc")]
        extern static int pipe2(int* fds, int flags);
        [DllImport("libc")]
        extern static IntPtr write(int fd, void* buf, IntPtr count);
        
        [DllImport("libc")]
        extern static IntPtr read(int fd, void* buf, IntPtr count);
        
        enum EventCodes
        {
            X11 = 1,
            Signal =2
        }

        private int _sigread, _sigwrite;
        private object _lock = new object();
        private bool _signaled;
        private DispatcherPriority _signaledPriority;
        private int _epoll;
        private Stopwatch _clock = Stopwatch.StartNew();

        class X11Timer : IDisposable
        {
            private readonly X11PlatformThreading _parent;

            public X11Timer(X11PlatformThreading parent, DispatcherPriority prio, TimeSpan interval, Action tick)
            {
                _parent = parent;
                Priority = prio;
                Tick = tick;
                Interval = interval;
                Reschedule();
            }
            
            public DispatcherPriority Priority { get; }
            public TimeSpan NextTick { get; private set; }
            public TimeSpan Interval { get; }
            public Action Tick { get; }
            public bool Disposed { get; private set; }

            public void Reschedule()
            {
                NextTick = _parent._clock.Elapsed + Interval;
            }

            public void Dispose()
            {
                Disposed = true;
                lock (_parent._lock)
                    _parent._timers.Remove(this);
            }
        }

        List<X11Timer> _timers = new List<X11Timer>();
        
        public X11PlatformThreading(AvaloniaX11Platform platform)
        {
            _platform = platform;
            _display = platform.Display;
            _eventHandlers = platform.Windows;
            _mainThread = Thread.CurrentThread;
            var fd = XLib.XConnectionNumber(_display);
            var ev = new epoll_event()
            {
                events = EPOLLIN,
                data = {u32 = (int)EventCodes.X11}
            };
            _epoll = epoll_create1(0);
            if (_epoll == -1)
                throw new X11Exception("epoll_create1 failed");

            if (epoll_ctl(_epoll, EPOLL_CTL_ADD, fd, ref ev) == -1)
                throw new X11Exception("Unable to attach X11 connection handle to epoll");

            var fds = stackalloc int[2];
            pipe2(fds, O_NONBLOCK);
            _sigread = fds[0];
            _sigwrite = fds[1];
            
            ev = new epoll_event
            {
                events = EPOLLIN,
                data = {u32 = (int)EventCodes.Signal}
            };
            if (epoll_ctl(_epoll, EPOLL_CTL_ADD, _sigread, ref ev) == -1)
                throw new X11Exception("Unable to attach signal pipe to epoll");
        }

        int TimerComparer(X11Timer t1, X11Timer t2)
        {
            return t2.Priority - t1.Priority;
        }

        void CheckSignaled()
        {
            int buf = 0;
            while (read(_sigread, &buf, new IntPtr(4)).ToInt64() > 0)
            {
            }

            DispatcherPriority prio;
            lock (_lock)
            {
                if (!_signaled)
                    return;
                _signaled = false;
                prio = _signaledPriority;
                _signaledPriority = DispatcherPriority.MinValue;
            }

            Signaled?.Invoke(prio);
        }

        void HandleX11(CancellationToken cancellationToken)
        {
            while (XPending(_display) != 0)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                XNextEvent(_display, out var xev);
                if (xev.type == XEventName.GenericEvent)
                    XGetEventData(_display, &xev.GenericEventCookie);
                try
                {
                    if (xev.type == XEventName.GenericEvent)
                    {
                        if (_platform.XI2 != null && _platform.Info.XInputOpcode ==
                            xev.GenericEventCookie.extension)
                        {
                            _platform.XI2.OnEvent((XIEvent*)xev.GenericEventCookie.data);
                        }
                    }
                    else if (_eventHandlers.TryGetValue(xev.AnyEvent.window, out var handler))
                        handler(xev);
                }
                finally
                {
                    if (xev.type == XEventName.GenericEvent && xev.GenericEventCookie.data != null)
                        XFreeEventData(_display, &xev.GenericEventCookie);
                }
            }

            Dispatcher.UIThread.RunJobs();
        }
        
        public void RunLoop(CancellationToken cancellationToken)
        {
            var readyTimers = new List<X11Timer>();
            while (!cancellationToken.IsCancellationRequested)
            {
                var now = _clock.Elapsed;
                TimeSpan? nextTick = null;
                readyTimers.Clear();
                lock(_timers)
                    foreach (var t in _timers)
                    {
                        if (nextTick == null || t.NextTick < nextTick.Value)
                            nextTick = t.NextTick;
                        if (t.NextTick < now)
                            readyTimers.Add(t);
                    }
                
                readyTimers.Sort(TimerComparer);
                
                foreach (var t in readyTimers)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;
                    t.Tick();
                    if(!t.Disposed)
                    {
                        t.Reschedule();
                        if (nextTick == null || t.NextTick < nextTick.Value)
                            nextTick = t.NextTick;
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                    return;
                //Flush whatever requests were made to XServer
                XFlush(_display);
                epoll_event ev;
                if (XPending(_display) == 0)
                    epoll_wait(_epoll, &ev, 1,
                        nextTick == null ? -1 : Math.Max(1, (int)(nextTick.Value - _clock.Elapsed).TotalMilliseconds));
                if (cancellationToken.IsCancellationRequested)
                    return;
                CheckSignaled();
                HandleX11(cancellationToken);
            }
        }

        

        public void Signal(DispatcherPriority priority)
        {
            lock (_lock)
            {
                if (priority > _signaledPriority)
                    _signaledPriority = priority;
                
                if(_signaled)
                    return;
                _signaled = true;
                int buf = 0;
                write(_sigwrite, &buf, new IntPtr(1));
            }
        }

        public bool CurrentThreadIsLoopThread => Thread.CurrentThread == _mainThread;
        public event Action<DispatcherPriority?> Signaled;
        
        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
        {
            if (_mainThread != Thread.CurrentThread)
                throw new InvalidOperationException("StartTimer can be only called from UI thread");
            if (interval <= TimeSpan.Zero)
                throw new ArgumentException("Interval must be positive", nameof(interval));
            
            // We assume that we are on the main thread and outside of epoll_wait, so there is no need for wakeup signal
            
            var timer = new X11Timer(this, priority, interval, tick);
            lock(_timers)
                _timers.Add(timer);
            return timer;
        }
    }
}
