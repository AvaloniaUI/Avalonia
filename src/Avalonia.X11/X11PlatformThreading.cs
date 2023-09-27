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
    internal unsafe class X11PlatformThreading : IControlledDispatcherImpl
    {
        private readonly AvaloniaX11Platform _platform;
        private readonly IntPtr _display;

        public delegate void EventHandler(ref XEvent xev);
        private readonly Dictionary<IntPtr, EventHandler> _eventHandlers;
        private Thread _mainThread;

        [StructLayout(LayoutKind.Explicit)]
        private struct epoll_data
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
        private struct epoll_event
        {
            public uint events;
            public epoll_data data;
        }
        
        [DllImport("libc")]
        private extern static int epoll_create1(int size);

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

        private enum EventCodes
        {
            X11 = 1,
            Signal =2
        }

        private int _sigread, _sigwrite;
        private object _lock = new object();
        private bool _signaled;
        private bool _wakeupRequested;
        private long? _nextTimer;
        private int _epoll;
        private Stopwatch _clock = Stopwatch.StartNew();

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

        private unsafe void HandleX11(CancellationToken cancellationToken)
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
                
                //Flush whatever requests were made to XServer
                XFlush(_display);
                epoll_event ev;
                if (XPending(_display) == 0)
                {
                    now = _clock.ElapsedMilliseconds;
                    if (_nextTimer < now)
                        continue;
                    
                    var timeout = _nextTimer == null ? (int)-1 : Math.Max(1, _nextTimer.Value - now);
                    epoll_wait(_epoll, &ev, 1, (int)Math.Min(int.MaxValue, timeout));
                    
                    // Drain the signaled pipe
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
                HandleX11(cancellationToken);
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
                if(_wakeupRequested)
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
                if(_signaled)
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
            _nextTimer = dueTimeInMs;
            if (_nextTimer != null)
                Wakeup();
        }


        public long Now => _clock.ElapsedMilliseconds;
        public bool CanQueryPendingInput => true;

        public bool HasPendingInput => _platform.EventGrouperDispatchQueue.HasJobs || XPending(_display) != 0;
    }
}
