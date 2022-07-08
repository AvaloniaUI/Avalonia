using System;
using Avalonia.Native.Interop;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Surfaces;

namespace Avalonia.NativeGraphics.Backend
{
    internal class GlRenderTargetWrapper : CallbackBase, IAvgGlPlatformSurfaceRenderTarget
    {
        private readonly IGlPlatformSurfaceRenderTarget _inner;

        public GlRenderTargetWrapper(IGlPlatformSurfaceRenderTarget inner)
        {
            _inner = inner;
        }

        class SessionWrapper : CallbackBase, IAvgGlPlatformSurfaceRenderSession
        {
            private readonly IGlPlatformSurfaceRenderingSession _inner;

            public SessionWrapper(IGlPlatformSurfaceRenderingSession inner)
            {
                _inner = inner;
            }
            public unsafe void GetPixelSize(AvgPixelSize* rv) => *rv = _inner.Size.ToAvgPixelSize();

            public double Scaling => _inner.Scaling;
            public int SampleCount => _inner.Context.SampleCount;
            public int StencilSize => _inner.Context.StencilSize;
            public int FboId
            {
                get
                {
                    _inner.Context.GlInterface.GetIntegerv(GlConsts.GL_FRAMEBUFFER_BINDING, out var fb);
                    return fb;
                }
            }

            public int IsYFlipped => _inner.IsYFlipped ? 1 : 0;

            public override void OnUnreferencedFromNative()
            {
                _inner.Dispose();
                base.OnUnreferencedFromNative();
            }
        }
        
        public IAvgGlPlatformSurfaceRenderSession BeginDraw()
        {
            return new SessionWrapper(_inner.BeginDraw());
        }
    }
}