using System;
using System.Runtime.InteropServices;

namespace Avalonia.Windowing.Bindings
{
    public class WindowWrapper : IWindowWrapper
    {
        [DllImport("winit_wrapper")]
        private static extern IntPtr winit_window_new(IntPtr eventsLoopHandle);

        [DllImport("winit_wrapper")]
        private static extern IntPtr winit_window_destroy(IntPtr handle);

        private IntPtr _handle;
        public WindowWrapper(EventsLoop eventsLoop) 
        {
            _handle = winit_window_new(eventsLoop.Handle); 
        }

        public void Dispose()
        {
            winit_window_destroy(_handle);
        }

        public void SetTitle(string title) 
        {
            
        }

        public void SetSize(double width, double height)
        {
            throw new NotImplementedException();
        }

        public (double, double) GetSize() 
        {
            return (0, 0);
        }

        public (double, double) GetPosition()
        {
            return (0, 0);
        }

        public void Show()
        {
            
        }
    }
}
