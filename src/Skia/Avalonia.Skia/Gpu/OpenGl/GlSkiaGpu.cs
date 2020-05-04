using System.Collections.Generic;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Imaging;
using SkiaSharp;

namespace Avalonia.Skia
{
    class GlSkiaGpu : IOpenGlAwareSkiaGpu
    {
        private GRContext _grContext;

        public GlSkiaGpu(IWindowingPlatformGlFeature gl)
        {
            var context = gl.MainContext;
            using (context.MakeCurrent())
            {
                using (var iface = context.Version.Type == GlProfileType.OpenGL ?
                    GRGlInterface.AssembleGlInterface((_, proc) => context.GlInterface.GetProcAddress(proc)) :
                    GRGlInterface.AssembleGlesInterface((_, proc) => context.GlInterface.GetProcAddress(proc)))
                {
                    _grContext = GRContext.Create(GRBackend.OpenGL, iface);
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
