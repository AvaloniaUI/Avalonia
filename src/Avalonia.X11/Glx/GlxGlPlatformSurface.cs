using System;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.X11.Glx
{
    internal class GlxGlPlatformSurface: IGlPlatformSurface
    {

        private readonly X11Window.SurfaceInfo _info;
        
        public GlxGlPlatformSurface(X11Window.SurfaceInfo info)
        {
            _info = info;
        }
        
        public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context)
        {
            return new RenderTarget((GlxContext)context, _info);
        }

        private class RenderTarget : IGlPlatformSurfaceRenderTarget
        {
            private readonly GlxContext _context;
            private readonly X11Window.SurfaceInfo _info;
            private PixelSize? _lastSize;

            public RenderTarget(GlxContext context, X11Window.SurfaceInfo info)
            {
                _context = context;
                _info = info;
            }

            public void Dispose()
            {
                // No-op
            }
            
            public bool IsCorrupted => false;
            public IGlPlatformSurfaceRenderingSession BeginDraw(IRenderTarget.RenderTargetSceneInfo sceneInfo)
            {
                _info.UpdateGtkFrameExtents(
                    _context.Display.X11Info.DeferredDisplay, sceneInfo.ShadowExtents);
                
                var size = sceneInfo.Size;
                //if (expectedSize.HasValue)
                {
                    XLib.XConfigureResizeWindow(_context.Display.X11Info.DeferredDisplay,
                        _info.Handle, size.Width, size.Height);
                    XLib.XFlush(_context.Display.X11Info.DeferredDisplay);

                    if (_lastSize != size)
                    {
                        XLib.XSync(_context.Display.X11Info.DeferredDisplay, true);
                        _lastSize = size;
                    }
                    _context.Glx.WaitX();
                }

                
                var oldContext = _context.MakeCurrent(_info.Handle);
                
                // Reset to default FBO first
                _context.GlInterface.BindFramebuffer(GL_FRAMEBUFFER, 0);
                    
                return new Session(_context, _info, size, oldContext);
            }

            private class Session : IGlPlatformSurfaceRenderingSession
            {
                private readonly GlxContext _context;
                private readonly X11Window.SurfaceInfo _info;
                private readonly PixelSize? _size;
                private readonly IDisposable _clearContext;
                public IGlContext Context => _context;

                public Session(GlxContext context, X11Window.SurfaceInfo info,
                    PixelSize? size,
                    IDisposable clearContext)
                {
                    _context = context;
                    _info = info;
                    _size = size;
                    _clearContext = clearContext;
                }

                public void Dispose()
                {
                    _context.GlInterface.Flush();
                    _context.Glx.WaitGL();
                    _context.Display.SwapBuffers(_info.Handle);
                    _context.Glx.WaitX();
                    _clearContext.Dispose();
                }

                public PixelSize Size => _size ?? _info.Size;
                public double Scaling => _info.Scaling;
                public bool IsYFlipped => false;
            }
        }
    }
}
