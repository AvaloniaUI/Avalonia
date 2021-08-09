using System;
using System.Reactive.Disposables;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Win32.Interop;
using static Avalonia.Win32.Interop.UnmanagedMethods;
using static Avalonia.Win32.OpenGl.WglConsts;

namespace Avalonia.Win32.OpenGl
{
    public class WglContext : IGlContextWithOSTextureSharing
    {
        private object _lock = new object();
        private readonly WglContext _sharedWith;
        private readonly IntPtr _context;
        private readonly IntPtr _hWnd;
        private readonly IntPtr _dc;
        private readonly int _pixelFormat;
        private readonly PixelFormatDescriptor _formatDescriptor;
        public IntPtr Handle => _context;
        private WglD3DInterop _d3dInterop;

        internal WglD3DInterop D3DInterop => _d3dInterop ??= new WglD3DInterop(this);

        internal WglContext(WglContext sharedWith, GlVersion version, IntPtr context, IntPtr hWnd, IntPtr dc, int pixelFormat,
            PixelFormatDescriptor formatDescriptor, WglInterface wgl)
        {
            Version = version;
            WglInterface = wgl;
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
            ReleaseDC(_hWnd, _dc);
            DestroyWindow(_hWnd);
        }

        public GlVersion Version { get; }
        public WglInterface WglInterface { get; }
        public GlInterface GlInterface { get; }
        public int SampleCount { get; }
        public int StencilSize { get; }

        private bool IsCurrent => wglGetCurrentContext() == _context && wglGetCurrentDC() == _dc;
        public IDisposable MakeCurrent()
        {
            if(IsCurrent)
                return Disposable.Empty;
            return new WglRestoreContext(_dc, _context, _lock);
        }

        public IDisposable EnsureCurrent() => MakeCurrent();


        public IntPtr CreateConfiguredDeviceContext(IntPtr hWnd)
        {
            var dc = GetDC(hWnd);
            var fmt = _formatDescriptor;
            SetPixelFormat(dc, _pixelFormat, ref fmt);
            return dc;
        }
        
        public IDisposable MakeCurrent(IntPtr hdc) => new WglRestoreContext(hdc, _context, _lock);

        public bool IsSharedWith(IGlContext context) =>
            context is WglContext c
            && (c == this
                || c._sharedWith == this
                || _sharedWith == context
                || _sharedWith != null && _sharedWith == c._sharedWith);

        private const string DXGITexture2DSharedHandle = "DXGITexture2DSharedHandle";
        public IGlOSSharedTexture CreateOSSharedTexture(string type, int width, int height)
        {
            if (type == DXGITexture2DSharedHandle)
                return new WglDxgiSharedTexture(this, width, height);
            throw new ArgumentException(nameof(type));
        }

        public IGlOSSharedTexture CreateOSSharedTexture(IGlContext compatibleWith, int width, int height)
        {
            if (compatibleWith is IGlContextWithOSTextureSharing sharing
                && sharing.SupportsOSSharedTextureType(DXGITexture2DSharedHandle))
                return CreateOSSharedTexture(DXGITexture2DSharedHandle, width, height);
            throw new InvalidOperationException("Contexts are not compatible");
        }

        public bool SupportsOSSharedTextureType(string type)
        {
            if (type == DXGITexture2DSharedHandle)
                return true;
            return false;
        }

        public IGlOSSharedTexture ImportOSSharedTexture(IGlOSSharedTexture osSharedTexture)
        {
            if (osSharedTexture is IPlatformHandle handle && handle.HandleDescriptor == DXGITexture2DSharedHandle)
                return new WglDxgiSharedTexture(this, handle.Handle, osSharedTexture.Width, osSharedTexture.Height);

            throw new ArgumentException("OS shared texture is not compatible", nameof(osSharedTexture));
        }
        
        public bool AreOSTextureSharingCompatible(IGlContext compatibleWith) =>
            compatibleWith is IGlContextWithOSTextureSharing sharing
            && sharing.SupportsOSSharedTextureType(DXGITexture2DSharedHandle);
    }
}
