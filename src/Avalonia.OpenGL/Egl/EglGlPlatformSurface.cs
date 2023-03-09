using System;
using Avalonia.OpenGL.Surfaces;

namespace Avalonia.OpenGL.Egl
{
    public class EglGlPlatformSurface : EglGlPlatformSurfaceBase
    {
        public interface IEglWindowGlPlatformSurfaceInfo
        {
            IntPtr Handle { get; }
            PixelSize Size { get; }
            double Scaling { get; }
        }
        
        private readonly IEglWindowGlPlatformSurfaceInfo _info;
        
        public EglGlPlatformSurface(IEglWindowGlPlatformSurfaceInfo info)
        {
            _info = info;
        }

        public override IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context)
        {
            var eglContext = (EglContext)context;
            
            var glSurface = eglContext.Display.CreateWindowSurface(_info.Handle);
            return new RenderTarget(glSurface, eglContext, _info);
        }

        private class RenderTarget : EglPlatformSurfaceRenderTargetBase
        {
            private EglSurface? _glSurface;
            private readonly IEglWindowGlPlatformSurfaceInfo _info;
            private PixelSize _currentSize;
            private readonly IntPtr _handle;

            public RenderTarget(EglSurface glSurface, EglContext context, IEglWindowGlPlatformSurfaceInfo info) : base(context)
            {
                _glSurface = glSurface;
                _info = info;
                _currentSize = info.Size;
                _handle = _info.Handle;
            }

            public override void Dispose() => _glSurface?.Dispose();

            public override IGlPlatformSurfaceRenderingSession BeginDrawCore()
            {
                if (_info.Size != _currentSize 
                    || _handle != _info.Handle
                    || _glSurface == null)
                {
                    _glSurface?.Dispose();
                    _glSurface = null;
                    _glSurface = Context.Display.CreateWindowSurface(_info.Handle);
                    _currentSize = _info.Size;
                }
                return base.BeginDraw(_glSurface, _info.Size, _info.Scaling);
            }
        }
    }
}

