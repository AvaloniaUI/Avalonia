using System;
using System.Diagnostics;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Win32.Interop;
using static Avalonia.OpenGL.GlConsts;
using static Avalonia.Win32.Interop.UnmanagedMethods;
namespace Avalonia.Win32.OpenGl
{
    class WglGlPlatformSurface: IGlPlatformSurface
    {

        private readonly EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo _info;
        
        public WglGlPlatformSurface( EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info)
        {
            _info = info;
        }
        
        public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context)
        {
            return new RenderTarget((WglContext)context, _info);
        }

        class RenderTarget : IGlPlatformSurfaceRenderTarget
        {
            private readonly WglContext _context;
            private readonly EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo _info;
            private IntPtr _hdc;
            public RenderTarget(WglContext context,  EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info)
            {
                _context = context;
                _info = info;
                _hdc = context.CreateConfiguredDeviceContext(info.Handle);
            }

            public void Dispose()
            {
                WglGdiResourceManager.ReleaseDC(_info.Handle, _hdc);
            }

            public IGlPlatformSurfaceRenderingSession BeginDraw()
            {
                var oldContext = _context.MakeCurrent(_hdc);
                
                // Reset to default FBO first
                _context.GlInterface.BindFramebuffer(GL_FRAMEBUFFER, 0);

                return new Session(_context, _hdc, _info, oldContext);
            }
            
            class Session : IGlPlatformSurfaceRenderingSession
            {
                private readonly WglContext _context;
                private readonly IntPtr _hdc;
                private readonly EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo _info;
                private readonly IDisposable _clearContext;
                public IGlContext Context => _context;

                public Session(WglContext context, IntPtr hdc, EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info,
                    IDisposable clearContext)
                {
                    _context = context;
                    _hdc = hdc;
                    _info = info;
                    _clearContext = clearContext;
                }

                public void Dispose()
                {
                    _context.GlInterface.Flush();
                    UnmanagedMethods.SwapBuffers(_hdc);
                    _clearContext.Dispose();
                }

                public PixelSize Size => _info.Size;
                public double Scaling => _info.Scaling;
                public bool IsYFlipped { get; }
            }
        }
    }
}
