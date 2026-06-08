using System;

namespace Avalonia.OpenGL.Egl
{
    public class EglImage : IDisposable
    {
        private readonly EglDisplay _display;
        private IntPtr _handle;

        public IntPtr Handle => _handle;
        
        public EglImage(EglDisplay display, IntPtr handle)
        {
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _handle = handle;
            if (handle == IntPtr.Zero)
                throw new ArgumentException("Invalid EGLImage handle", nameof(handle));
        }

        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
            {
                using (_display.Lock())
                    _display.EglInterface.DestroyImageKHR(_display.Handle, _handle);
                _handle = IntPtr.Zero;
            }
        }
    }
}
