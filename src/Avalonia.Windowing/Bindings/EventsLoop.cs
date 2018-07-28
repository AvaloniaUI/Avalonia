using System;
using System.Runtime.InteropServices;

namespace Avalonia.Windowing.Bindings
{
    public class EventsLoop : IDisposable
    {
        [DllImport("winit_wrapper")]
        private static extern IntPtr winit_events_loop_new();

        [DllImport("winit_wrapper")]
        private static extern IntPtr winit_events_loop_proxy_new(IntPtr eventsLoopHandle);

        [DllImport("winit_wrapper")]
        private static extern void winit_events_loop_destroy(IntPtr handle);

        [DllImport("winit_wrapper")]
        private static extern void winit_events_loop_run
        (
            IntPtr handle, 
            EventNotifier notifier
        );

        public IntPtr Handle { get; private set;  }

        private readonly EventsLoopProxy _eventsLoopProxy;
        private readonly EventNotifier _notifier;

        public event CharacterEventCallback OnCharacterEvent;
        public event KeyboardEventCallback OnKeyboardEvent;
        public event MouseEventCallback OnMouseEvent;
        public event AwakenedEventCallback OnAwakened;
        public event ResizeEventCallback OnResized;
        public event ShouldExitEventLoopCallback OnShouldExitEventLoop;
        public event CloseRequestedCallback OnCloseRequested;
        public event FocusedCallback OnFocused;

        public EventsLoop()
        {
            Handle = winit_events_loop_new();
            _eventsLoopProxy = new EventsLoopProxy(winit_events_loop_proxy_new(Handle)); 
            _notifier = new EventNotifier()
            {
                OnMouseEvent = (windowId, mouseEvent) => OnMouseEvent?.Invoke(windowId, mouseEvent),
                OnKeyboardEvent = (windowId, keyboardEvent) => OnKeyboardEvent?.Invoke(windowId, keyboardEvent),
                OnCharacterEvent = (windowId, characterEvent) => OnCharacterEvent?.Invoke(windowId, characterEvent),
                OnResized = (windowId, resizeEvent) => OnResized?.Invoke(windowId, resizeEvent),
                OnAwakened = () => OnAwakened?.Invoke(),
                OnShouldExitEventLoop = (windowId) => (byte)OnShouldExitEventLoop?.Invoke(windowId),
                OnCloseRequested = (windowId) => OnCloseRequested?.Invoke(windowId),
                OnFocused = (windowId, focused) => OnFocused?.Invoke(windowId, focused)
            };
        }

        public void Run()
        {
            winit_events_loop_run(
                Handle,
                _notifier
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
