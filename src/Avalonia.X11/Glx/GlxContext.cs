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
        private readonly GlxContext _sharedWith;
        private readonly X11Info _x11;
        private readonly IntPtr _defaultXid;
        private readonly bool _ownsPBuffer;
        private readonly object _lock = new object();

        public GlxContext(GlxInterface glx, IntPtr handle, GlxDisplay display,
            GlxContext sharedWith,
            GlVersion version, int sampleCount, int stencilSize,
            X11Info x11, IntPtr defaultXid,
            bool ownsPBuffer)
        {
            Handle = handle;
            Glx = glx;
            _sharedWith = sharedWith;
            _x11 = x11;
            _defaultXid = defaultXid;
            _ownsPBuffer = ownsPBuffer;
            Display = display;
            Version = version;
            SampleCount = sampleCount;
            StencilSize = stencilSize;
            using (MakeCurrent())
                GlInterface = new GlInterface(version, GlxInterface.SafeGetProcAddress);
        }
        
        public GlxDisplay Display { get; }
        public GlVersion Version { get; }
        public GlInterface GlInterface { get; }
        public int SampleCount { get; }
        public int StencilSize { get; }
        
        class RestoreContext : IDisposable
        {
            private GlxInterface _glx;
            private IntPtr _defaultDisplay;
            private readonly object _l;
            private IntPtr _display;
            private IntPtr _context;
            private IntPtr _read;
            private IntPtr _draw;

            public RestoreContext(GlxInterface glx, IntPtr defaultDisplay, object l)
            {
                _glx = glx;
                _defaultDisplay = defaultDisplay;
                _l = l;
                _display = _glx.GetCurrentDisplay();
                _context = _glx.GetCurrentContext();
                _read = _glx.GetCurrentReadDrawable();
                _draw = _glx.GetCurrentDrawable();
            }

            public void Dispose()
            {
                var disp = _display == IntPtr.Zero ? _defaultDisplay : _display;
                _glx.MakeContextCurrent(disp, _draw, _read, _context);
                Monitor.Exit(_l);
            }
        }
        
        public IDisposable MakeCurrent() => MakeCurrent(_defaultXid);
        public IDisposable EnsureCurrent()
        {
            if(IsCurrent)
                return Disposable.Empty;
            return MakeCurrent();
        }

        public bool IsSharedWith(IGlContext context)
        {
            var c = (GlxContext)context;
            return c == this
                   || c._sharedWith == this
                   || _sharedWith == context
                   || _sharedWith != null && _sharedWith == c._sharedWith;
        }

        public IDisposable MakeCurrent(IntPtr xid)
        {
            Monitor.Enter(_lock);
            var success = false;
            try
            {
                var old = new RestoreContext(Glx, _x11.Display, _lock);
                if (!Glx.MakeContextCurrent(_x11.Display, xid, xid, Handle))
                    throw new OpenGlException("glXMakeContextCurrent failed ");

                success = true;
                return old;
            }
            finally
            {
                if (!success)
                    Monitor.Exit(_lock);
            }
        }

        public bool IsCurrent => Glx.GetCurrentContext() == Handle;

        public void Dispose()
        {
            Glx.DestroyContext(_x11.Display, Handle);
            if (_ownsPBuffer)
                Glx.DestroyPbuffer(_x11.Display, _defaultXid);
        }
    }
}
