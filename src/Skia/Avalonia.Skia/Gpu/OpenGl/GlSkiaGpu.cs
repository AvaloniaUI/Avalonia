using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Logging;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Imaging;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    class GlSkiaGpu : IOpenGlAwareSkiaGpu, IOpenGlTextureSharingRenderInterfaceContextFeature
    {
        private GRContext _grContext;
        private IGlContext _glContext;
        private bool? _canCreateSurfaces;

        public GlSkiaGpu(IGlContext context, long? maxResourceBytes)
        {
            _glContext = context;
            using (_glContext.EnsureCurrent())
            {
                using (var iface = context.Version.Type == GlProfileType.OpenGL ?
                    GRGlInterface.CreateOpenGl(proc => context.GlInterface.GetProcAddress(proc)) :
                    GRGlInterface.CreateGles(proc => context.GlInterface.GetProcAddress(proc)))
                {
                    _grContext = GRContext.CreateGl(iface, new GRContextOptions { AvoidStencilBuffers = true });
                    if (maxResourceBytes.HasValue)
                    {
                        _grContext.SetResourceCacheLimit(maxResourceBytes.Value);
                    }
                }
            }
        }

        class SurfaceWrapper : IGlPlatformSurface
        {
            private readonly object _surface;

            public SurfaceWrapper( object surface)
            {
                _surface = surface;
            }

            public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context)
            {
                var feature = context.TryGetFeature<IGlPlatformSurfaceRenderTargetFactory>()!;
                return feature.CreateRenderTarget(context, _surface);
            }
        }

        public ISkiaGpuRenderTarget TryCreateRenderTarget(IEnumerable<object> surfaces)
        {
            var customRenderTargetFactory = _glContext.TryGetFeature<IGlPlatformSurfaceRenderTargetFactory>();
            foreach (var surface in surfaces)
            {
                if (customRenderTargetFactory?.CanRenderToSurface(_glContext, surface) == true)
                {
                    return new GlRenderTarget(_grContext, _glContext, new SurfaceWrapper(surface));
                }
                if (surface is IGlPlatformSurface glSurface)
                {
                    return new GlRenderTarget(_grContext, _glContext, glSurface);
                }
            }

            return null;
        }

        public ISkiaSurface TryCreateSurface(PixelSize size, ISkiaGpuRenderSession session)
        {
            // Only windows platform needs our FBO trickery
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return null;

            // Blit feature requires glBlitFramebuffer
            if (!_glContext.GlInterface.IsBlitFramebufferAvailable)
                return null;
            
            size = new PixelSize(Math.Max(size.Width, 1), Math.Max(size.Height, 1));
            if (_canCreateSurfaces == false)
                return null;
            try
            {
                var surface = new FboSkiaSurface(_grContext, _glContext, size, session?.SurfaceOrigin ?? GRSurfaceOrigin.TopLeft);
                _canCreateSurfaces = true;
                return surface;
            }
            catch (Exception)
            {
                Logger.TryGet(LogEventLevel.Error, "OpenGL")
                    ?.Log(this, "Unable to create a Skia-compatible FBO manually");
                _canCreateSurfaces ??= false;
                return null;
            }
        }

        public bool CanCreateSharedContext => _glContext.CanCreateSharedContext;

        public IGlContext CreateSharedContext(IEnumerable<GlVersion> preferredVersions = null) =>
            _glContext.CreateSharedContext(preferredVersions);

        public IOpenGlBitmapImpl CreateOpenGlBitmap(PixelSize size, Vector dpi) => new GlOpenGlBitmapImpl(_glContext, size, dpi);
        
        public void Dispose()
        {
            if (_glContext.IsLost)
                _grContext.AbandonContext();
            else
                _grContext.AbandonContext(true);
            _grContext.Dispose();
        }

        public bool IsLost => _glContext.IsLost;
        public IDisposable EnsureCurrent() => _glContext.EnsureCurrent();

        public object TryGetFeature(Type featureType)
        {
            if (featureType == typeof(IOpenGlTextureSharingRenderInterfaceContextFeature))
                return this;
            return null;
        }
    }
}
