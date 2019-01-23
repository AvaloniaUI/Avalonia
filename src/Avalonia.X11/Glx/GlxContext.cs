using System;
using System.Reactive.Disposables;
using System.Threading;
using Avalonia.OpenGL;
namespace Avalonia.X11.Glx
{
    class GlxContext : IGlContext
    {
        public  IntPtr Handle { get; }
        public GlxInterface Glx { get; }
        private readonly X11Info _x11;
        private readonly IntPtr _defaultXid;
        private readonly object _lock = new object();

        public GlxContext(GlxInterface glx, IntPtr handle, GlxDisplay display, X11Info x11, IntPtr defaultXid)
        {
            Handle = handle;
            Glx = glx;
            _x11 = x11;
            _defaultXid = defaultXid;
            Display = display;
        }
        
        public GlxDisplay Display { get; }
        IGlDisplay IGlContext.Display => Display;
        
        public IDisposable Lock()
        {
            Monitor.Enter(_lock);
            return Disposable.Create(() => Monitor.Exit(_lock));
        }
        
        public void MakeCurrent() => MakeCurrent(_defaultXid);

        public void MakeCurrent(IntPtr xid)
        {
            if (!Glx.MakeContextCurrent(_x11.Display, xid, xid, Handle))
                throw new OpenGlException("glXMakeContextCurrent failed ");
        }
    }
}
