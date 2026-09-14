using System;

namespace Avalonia.OpenGL.Egl
{
    /// <summary>
    /// Wraps a native <c>EGLImageKHR</c> handle and destroys it via <c>eglDestroyImageKHR</c> on disposal.
    /// </summary>
    public sealed class EglImage : IDisposable
    {
        private readonly EglDisplay _display;
        private IntPtr _handle;

        /// <summary>
        /// Gets the native <c>EGLImageKHR</c> handle, or <see cref="IntPtr.Zero"/> once disposed.
        /// </summary>
        public IntPtr Handle => _handle;

        /// <summary>
        /// Initializes a new <see cref="EglImage"/> wrapping an existing <c>EGLImageKHR</c> handle.
        /// </summary>
        /// <param name="display">The EGL display that owns the image.</param>
        /// <param name="handle">A valid (non-zero) <c>EGLImageKHR</c> handle.</param>
        /// <exception cref="ArgumentNullException"><paramref name="display"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="handle"/> is <see cref="IntPtr.Zero"/>.</exception>
        public EglImage(EglDisplay display, IntPtr handle)
        {
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _handle = handle;
            if (handle == IntPtr.Zero)
                throw new ArgumentException("Invalid EGLImage handle", nameof(handle));
        }

        /// <summary>
        /// Destroys the underlying <c>EGLImageKHR</c>. Safe to call more than once.
        /// </summary>
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
