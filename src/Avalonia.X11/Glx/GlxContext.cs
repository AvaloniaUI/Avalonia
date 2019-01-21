using System;
using System.Reactive.Disposables;
using System.Threading;
using Avalonia.OpenGL;
using static Avalonia.X11.Glx.Glx;
namespace Avalonia.X11.Glx
{
    class GlxContext : IGlContext
    {
        public  IntPtr Handle { get; }
        private readonly X11Info _x11;
        private readonly object _lock = new object();

        public GlxContext(IntPtr handle, GlxDisplay display, X11Info x11)
        {
            Handle = handle;
            _x11 = x11;
            Display = display;
        }
        
        public GlxDisplay Display { get; }
        IGlDisplay IGlContext.Display => Display;
        
        public IDisposable Lock()
        {
            Monitor.Enter(_lock);
            return Disposable.Create(() => Monitor.Exit(_lock));
        }
        
        public void MakeCurrent() => MakeCurrent(IntPtr.Zero);
        
        public void MakeCurrent(IntPtr xid) => GlxMakeCurrent(_x11.Display, xid, Handle);
    }
}
