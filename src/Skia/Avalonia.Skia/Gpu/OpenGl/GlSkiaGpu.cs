using System.Collections.Generic;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Imaging;
using SkiaSharp;

namespace Avalonia.Skia
{
    class GlSkiaGpu : IOpenGlAwareSkiaGpu
    {
        private GRContext _grContext;

        public GlSkiaGpu(IWindowingPlatformGlFeature gl, long? maxResourceBytes)
        {
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
    }
}
