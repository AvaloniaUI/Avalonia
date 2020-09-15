using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Imaging;
using SkiaSharp;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.Skia
{
    class GlSkiaGpu : IOpenGlAwareSkiaGpu
    {
        private GRContext _grContext;
        private IWindowingPlatformGlFeature _gl;

        public GlSkiaGpu(IWindowingPlatformGlFeature gl, long? maxResourceBytes)
        {
            _gl = gl;
            var context = gl.MainContext;
            using (context.MakeCurrent())
            {
                using (var iface = context.Version.Type == GlProfileType.OpenGL ?
                    GRGlInterface.CreateOpenGl(proc => context.GlInterface.GetProcAddress(proc)) :
                    GRGlInterface.CreateGles(proc => context.GlInterface.GetProcAddress(proc)))
                {
                    _grContext = GRContext.CreateGl(iface);
                    if (maxResourceBytes.HasValue)
                    {
                        _grContext.SetResourceCacheLimit(maxResourceBytes.Value);
                    }
                }
            }
        }

        public ISkiaGpuRenderTarget TryCreateRenderTarget(IEnumerable<object> surfaces)
        {
            foreach (var surface in surfaces)
            {
                if (surface is IGlPlatformSurface glSurface)
                {
                    return new GlRenderTarget(_grContext, glSurface);
                }
            }

            return null;
        }

        public IOpenGlTextureBitmapImpl CreateOpenGlTextureBitmap() => new OpenGlTextureBitmapImpl();

        public IControlledSurface CreateControlledSurface(PixelSize size)
        {
            return new GlSkiaGpuControlledSurface(_grContext, _gl, size);
        }

        class GlSkiaGpuControlledSurface : IControlledSurface
        {
            private SKSurface _surface;
            private GRBackendTexture _texture;
            private IDisposable _disposable;
            private int _textureId;

            public GlSkiaGpuControlledSurface(GRContext context, IWindowingPlatformGlFeature glFeature, PixelSize size)
            {
                var gl = glFeature.MainContext.GlInterface;

                var oneArr = new int[1];
                gl.GenTextures(1, oneArr);

                _textureId = oneArr[0];

                gl.BindTexture(GL_TEXTURE_2D, _textureId);
                gl.TexImage2D(GL_TEXTURE_2D, 0,
                    glFeature.MainContext.Version.Type == GlProfileType.OpenGLES ? GL_RGBA : GL_RGBA8,
                    size.Width, size.Height, 0, GL_RGBA, GL_UNSIGNED_BYTE, IntPtr.Zero);
                gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
                gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);

                _texture = new GRBackendTexture(size.Width, size.Height, false,
                    new GRGlTextureInfo(
                        GL_TEXTURE_2D, (uint)_textureId,
                        (uint)GL_RGBA8));

                _surface = SKSurface.Create(context, _texture, GRSurfaceOrigin.TopLeft,
                    SKColorType.Rgba8888);

                _disposable = Disposable.Create(() =>
                {
                    gl.DeleteTextures(1, new[] { _textureId });
                });

            }

            public SKSurface Surface => _surface;

            public void Dispose()
            {
                _surface.Dispose();
                _texture.Dispose();
                _disposable.Dispose();
            }
        }
    }
}
