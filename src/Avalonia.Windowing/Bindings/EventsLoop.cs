using System;
using System.Runtime.InteropServices;

namespace Avalonia.Windowing.Bindings
{
    public class EventsLoop : IDisposable
    {
        [DllImport("winit_wrapper")]
        private static extern IntPtr winit_events_loop_new(out IntPtr eventsLoopProxyHandle);

        [DllImport("winit_wrapper")]
        private static extern void winit_events_loop_destroy(IntPtr handle);

        [DllImport("winit_wrapper")]
        private static extern void winit_events_loop_run
        (
            IntPtr handle, 
            EventNotifier notifier
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
        public delegate void TimerDel();

        [DllImport("winit_wrapper")]
        private static extern void winit_events_loop_timer
        (
            TimerDel del2
        );

        public IntPtr Handle { get; private set;  }
        private readonly EventsLoopProxy _eventsLoopProxy;
        private readonly EventNotifier _notifier;

        public event KeyboardEventCallback OnKeyboardEvent;
        public event MouseEventCallback OnMouseEvent;
        public event AwakenedEventCallback OnAwakened;
        public event ResizeEventCallback OnResized;

        public EventsLoop()
        {
            Handle = winit_events_loop_new(out var elpHandle);
            _eventsLoopProxy = new EventsLoopProxy(elpHandle); 
            _notifier = new EventNotifier()
            {
                OnMouseEvent = (IntPtr windowId, MouseEvent mouseEvent) => OnMouseEvent?.Invoke(windowId, mouseEvent),
                OnKeyboardEvent = (IntPtr windowId, KeyboardEvent keyboardEvent) => OnKeyboardEvent?.Invoke(windowId, keyboardEvent),
                OnResized = (IntPtr windowId, ResizeEvent resizeEvent) => OnResized?.Invoke(windowId, resizeEvent),
                OnAwakened = () => OnAwakened?.Invoke()
            };
        }

        public void Run()
        {
            winit_events_loop_run(
                Handle,
                _notifier
            );
        }

        public void RunTimer(TimerDel del)
        {
            winit_events_loop_timer(del);   
        }

        public void Wakeup()
        {
            _eventsLoopProxy.Wakeup();
        }

        public void Dispose()
        {
            _eventsLoopProxy.Dispose();
            winit_events_loop_destroy(Handle);
        }

        public void GetAvailableMonitors() 
        {
            // TODO
        }

        private class EventsLoopProxy : IDisposable
        {
            [DllImport("winit_wrapper")]
            private static extern void winit_events_loop_proxy_destroy(IntPtr handle);

            [DllImport("winit_wrapper")]
            private static extern void winit_events_loop_proxy_wakeup(IntPtr handle);

            private readonly IntPtr _handle;
            public EventsLoopProxy(IntPtr handle)
            {
                _handle = handle;
            }

            public void Wakeup()
            {
                winit_events_loop_proxy_wakeup(_handle);
            }

            public void Dispose()
            {
                winit_events_loop_proxy_destroy(_handle);
            }
        }
    }
}
