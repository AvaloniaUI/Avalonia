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

        public delegate void EventHandler(ref XEvent xev);
        private readonly Dictionary<IntPtr, EventHandler> _eventHandlers;
        private Thread _mainThread;
        
        
        [StructLayout(LayoutKind.Sequential)]
        public struct fd_set
        {
            public fixed uint fds[FD_SETSIZE];
            public const int FD_SETSIZE = 64;

            public void Set(int fd)
            {
                var idx = fd / 32;
                if (idx >= FD_SETSIZE)
                    throw new ArgumentException();
                var bit = (fd % 32);
                fds[idx] = 1u << bit;
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
                tv_usec = milliseconds % 1000 * 1000;
            }
        }
        
        private const int O_NONBLOCK = 2048;
        
        [DllImport("libc", EntryPoint = "select")]
        private static extern int select(
            int nfds,
            [In] [Out] ref fd_set readfds,
            [In] [Out] ref fd_set writefds,
            [In] [Out] ref fd_set exceptfds,
            timeval* timeout);

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

        private readonly int _sigread, _sigwrite, _x11Fd;
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
            _x11Fd = XLib.XConnectionNumber(_display);
            var pipeFds = stackalloc int[2];
            pipe2(pipeFds, O_NONBLOCK);
            _sigread = pipeFds[0];
            _sigwrite = pipeFds[1];
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

        unsafe void HandleX11(CancellationToken cancellationToken)
        {
            while (XPending(_display) != 0)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                XNextEvent(_display, out var xev);
                if(XFilterEvent(ref xev, IntPtr.Zero))
                    continue;

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
                        handler(ref xev);
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
                fd_set readFds = default, writeFds = default, exceptFds = default;
                readFds.Set(_x11Fd);
                readFds.Set(_sigread);
                var timeout = nextTick == null
                    ? (int?)null
                    : Math.Max(1, (int)(nextTick.Value - _clock.Elapsed).TotalMilliseconds);
                var timeval = new timeval(timeout ?? 0);

                if (XPending(_display) == 0)
                    select(Math.Max(_x11Fd, _sigread),
                        ref readFds, ref writeFds, ref exceptFds, timeout.HasValue ? &timeval : null);
                
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
