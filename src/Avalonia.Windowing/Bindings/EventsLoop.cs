using System;
using System.Runtime.InteropServices;

namespace Avalonia.Windowing.Bindings
{
    public enum MouseEventType : int
    {
        MouseMoved
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MouseEvent 
    {
        public MouseEventType EventType { get; set; }
        public LogicalPosition Position { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ResizeEvent
    {
        public LogicalSize Size { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LogicalPosition
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LogicalSize
    {
        public double Width { get; set; }
        public double Height { get; set; }
    }

    public delegate void MouseEventCallback(IntPtr windowId, MouseEvent mouseEvent);
    public delegate void ResizeEventCallback(IntPtr windowId, ResizeEvent mouseEvent);
    public delegate void AwakenedEventCallback();

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
            MouseEventCallback callback,
            AwakenedEventCallback awakenedEventCallback,
            ResizeEventCallback resizeEventCallback
        );

        public IntPtr Handle { get; private set;  }
        private readonly EventsLoopProxy _eventsLoopProxy;

        public event MouseEventCallback MouseEvent;
        public event AwakenedEventCallback Awakened;
        public event ResizeEventCallback Resized;

        public EventsLoop()
        {
            Handle = winit_events_loop_new(out var elpHandle);
            _eventsLoopProxy = new EventsLoopProxy(elpHandle);
        }

        public void Run()
        {
            // We need a delegate callback here to support talking back to the C# code.
            // Send an event type enum and then unsafely construct the event
            winit_events_loop_run(
                Handle, 
                (windowId, mouseEvent) => {
                    MouseEvent?.Invoke(windowId, mouseEvent);
                },
                () =>
                {
                    Awakened?.Invoke();
                },
                (windowId, ResizeEvent) => {
                    Resized?.Invoke(windowId, ResizeEvent);    
                }
            );
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
