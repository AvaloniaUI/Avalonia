using System;
using Avalonia.Metadata;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;

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
        
        //TODO13: merge
        [PrivateApi]
        public interface IEglWindowGlPlatformSurfaceInfoWithWaitPolicy : IEglWindowGlPlatformSurfaceInfo
        {
            public bool SkipWaits { get; }
        }

        /// <summary>
        /// Lets the platform adjust the native window to the scene that is about to be rendered, e.g. resize a
        /// child window used as the render target. Called on the render thread before <see cref="IEglWindowGlPlatformSurfaceInfo.Size"/>
        /// is read for the frame. The returned disposable, if any, is disposed once the frame has been presented.
        /// </summary>
        //TODO13: merge
        [PrivateApi]
        public interface IEglWindowGlPlatformSurfaceInfoWithBeginDraw : IEglWindowGlPlatformSurfaceInfo
        {
            IDisposable? BeginDraw(IRenderTarget.RenderTargetSceneInfo sceneInfo);
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
            private IntPtr _handle;

            public RenderTarget(EglSurface glSurface, EglContext context, IEglWindowGlPlatformSurfaceInfo info) : base(context)
            {
                _glSurface = glSurface;
                _info = info;
                _currentSize = info.Size;
                _handle = _info.Handle;
                SkipWaits = info is IEglWindowGlPlatformSurfaceInfoWithWaitPolicy { SkipWaits: true };
            }

            protected override bool SkipWaits { get; }

            public override void Dispose() => _glSurface?.Dispose();

            public override IGlPlatformSurfaceRenderingSession BeginDrawCore(IRenderTarget.RenderTargetSceneInfo sceneInfo)
            {
                var beginDrawScope = (_info as IEglWindowGlPlatformSurfaceInfoWithBeginDraw)?.BeginDraw(sceneInfo);
                try
                {
                    var handle = _info.Handle;
                    var size = _info.Size;
                    if (size != _currentSize
                        || _handle != handle
                        || _glSurface == null)
                    {
                        _glSurface?.Dispose();
                        _glSurface = null;
                        _glSurface = Context.Display.CreateWindowSurface(handle);
                        _currentSize = size;
                        _handle = handle;
                    }

                    var session = base.BeginDraw(_glSurface, size, _info.Scaling,
                        onFinish: beginDrawScope == null ? null : beginDrawScope.Dispose);
                    beginDrawScope = null;
                    return session;
                }
                finally
                {
                    beginDrawScope?.Dispose();
                }
            }
        }
    }
}

