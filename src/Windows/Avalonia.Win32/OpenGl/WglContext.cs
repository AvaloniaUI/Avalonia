using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Reactive;
using Avalonia.Win32.Interop;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32.OpenGl
{
    internal class WglContext : IGlContext
    {
        private readonly object _lock = new();
        private readonly WglContext? _sharedWith;
        private readonly IntPtr _context;
        private readonly IntPtr _hWnd;
        private readonly IntPtr _dc;
        private readonly int _pixelFormat;
        private readonly PixelFormatDescriptor _formatDescriptor;

        public IntPtr Handle => _context;

        public WglContext(WglContext? sharedWith, GlVersion version, IntPtr context, IntPtr hWnd, IntPtr dc, int pixelFormat,
            PixelFormatDescriptor formatDescriptor)
        {
            Version = version;
            _sharedWith = sharedWith;
            _context = context;
            _hWnd = hWnd;
            _dc = dc;
            _pixelFormat = pixelFormat;
            _formatDescriptor = formatDescriptor;
            StencilSize = formatDescriptor.StencilBits;
            using (MakeCurrent())
                GlInterface = new GlInterface(version, proc =>
                {
                    var ext = wglGetProcAddress(proc);
                    if (ext != IntPtr.Zero)
                        return ext;
                    return GetProcAddress(WglDisplay.OpenGl32Handle, proc);
                });

        }

        public void Dispose()
        {
            wglDeleteContext(_context);
            WglGdiResourceManager.ReleaseDC(_hWnd, _dc);
            WglGdiResourceManager.DestroyWindow(_hWnd);
            IsLost = true;
        }

        public GlVersion Version { get; }
        public GlInterface GlInterface { get; }
        public int SampleCount => 0;
        public int StencilSize { get; }

        private bool IsCurrent => wglGetCurrentContext() == _context && wglGetCurrentDC() == _dc;
        public IDisposable MakeCurrent()
        {
            if (IsLost)
                throw new PlatformGraphicsContextLostException();
            if(IsCurrent)
                return Disposable.Empty;
            return new WglRestoreContext(_dc, _context, _lock);
        }

        public bool IsLost { get; private set; }
        public IDisposable EnsureCurrent() => MakeCurrent();

        internal IDisposable Lock()
        {
            Monitor.Enter(_lock);
            return Disposable.Create(_lock, Monitor.Exit);
        }


        public IntPtr CreateConfiguredDeviceContext(IntPtr hWnd)
        {
            var dc = WglGdiResourceManager.GetDC(hWnd);
            var fmt = _formatDescriptor;
            SetPixelFormat(dc, _pixelFormat, ref fmt);
            return dc;
        }
        
        public IDisposable MakeCurrent(IntPtr hdc) => new WglRestoreContext(hdc, _context, _lock);

        public bool IsSharedWith(IGlContext context)
        {
            var c = (WglContext)context;
            return c == this
                   || c._sharedWith == this
                   || _sharedWith == context
                   || _sharedWith != null && _sharedWith == c._sharedWith;
        }

        public bool CanCreateSharedContext => true;
        public IGlContext? CreateSharedContext(IEnumerable<GlVersion>? preferredVersions = null)
        {
            var versions = preferredVersions?.Append(Version).ToArray() ?? new[] { Version };
            return WglDisplay.CreateContext(versions, _sharedWith ?? this);
        }

        public object? TryGetFeature(Type featureType) => null;
    }
}
