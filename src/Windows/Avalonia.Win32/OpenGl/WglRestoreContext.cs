using System;
using System.Threading;
using Avalonia.OpenGL;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32.OpenGl
{
    internal class WglRestoreContext : IDisposable
    {
        private readonly object _monitor;
        private readonly IntPtr _oldDc;
        private readonly IntPtr _oldContext;

        public WglRestoreContext(IntPtr gc, IntPtr context, object monitor, bool takeMonitor = true)
        {
            _monitor = monitor;
            _oldDc = wglGetCurrentDC();
            _oldContext = wglGetCurrentContext();

            if (monitor != null && takeMonitor) 
                Monitor.Enter(monitor);

            if (!wglMakeCurrent(gc, context))
            {
                if(monitor != null && takeMonitor)
                    Monitor.Exit(monitor);
                throw new OpenGlException("Unable to make the context current");
            }
        }

        public void Dispose()
        {
            if (!wglMakeCurrent(_oldDc, _oldContext))
                wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
            if (_monitor != null)
                Monitor.Exit(_monitor);
        }
    }
}
