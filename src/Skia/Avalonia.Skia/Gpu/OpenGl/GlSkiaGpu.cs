using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Logging;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using SkiaSharp;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.Skia
{
    internal class GlSkiaGpu : ISkiaGpu, IOpenGlTextureSharingRenderInterfaceContextFeature,
        ISkiaGpuWithPlatformGraphicsContext
    {
        private readonly GRContext _grContext;
        private readonly IGlContext _glContext;
        public GRContext GrContext => _grContext;
        public IGlContext GlContext => _glContext;
        private readonly List<Action> _postDisposeCallbacks = new();
        private bool? _canCreateSurfaces;
        private readonly IExternalObjectsRenderInterfaceContextFeature? _externalObjectsFeature;

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

                context.TryGetFeature<IGlContextExternalObjectsFeature>(out var externalObjects);
                _externalObjectsFeature = new GlSkiaExternalObjectsFeature(this, externalObjects);
            }
        }

        private class SurfaceWrapper : IGlPlatformSurface
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

        public ISkiaGpuRenderTarget? TryCreateRenderTarget(IEnumerable<object> surfaces)
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

        public ISkiaSurface? TryCreateSurface(PixelSize size, ISkiaGpuRenderSession? session)
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
                var surface = new FboSkiaSurface(this, _grContext, _glContext, size, 
                    session?.SurfaceOrigin ?? GRSurfaceOrigin.TopLeft);
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

        public IGlContext? CreateSharedContext(IEnumerable<GlVersion>? preferredVersions = null) =>
            _glContext.CreateSharedContext(preferredVersions);

        public ICompositionImportableOpenGlSharedTexture CreateSharedTextureForComposition(IGlContext context, PixelSize size)
        {
            if (!context.IsSharedWith(_glContext))
                throw new InvalidOperationException("Contexts do not belong to the same share group");
            
            using (context.EnsureCurrent())
            {
                var gl = context.GlInterface;
                gl.GetIntegerv(GL_TEXTURE_BINDING_2D, out int oldTexture);
                var tex = gl.GenTexture();

                var format = context.Version.Type == GlProfileType.OpenGLES && context.Version.Major == 2
                    ? GL_RGBA
                    : GL_RGBA8;
                
                gl.BindTexture(GL_TEXTURE_2D, tex);
                gl.TexImage2D(GL_TEXTURE_2D, 0,
                    format, size.Width, size.Height,
                    0, GL_RGBA, GL_UNSIGNED_BYTE, IntPtr.Zero);

                gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
                gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
                gl.BindTexture(GL_TEXTURE_2D, oldTexture);
                
                return new GlSkiaSharedTextureForComposition(context, tex, format, size);
            }
        }

        public void Dispose()
        {
            if (_glContext.IsLost)
                _grContext.AbandonContext();
            else
                _grContext.AbandonContext(true);
            _grContext.Dispose();
            
            lock(_postDisposeCallbacks)
                foreach (var cb in _postDisposeCallbacks)
                    cb();
        }

        public bool IsLost => _glContext.IsLost;
        public IDisposable EnsureCurrent() => _glContext.EnsureCurrent();
        public IPlatformGraphicsContext? PlatformGraphicsContext => _glContext;

        public object? TryGetFeature(Type featureType)
        {
            if (featureType == typeof(IOpenGlTextureSharingRenderInterfaceContextFeature))
                return this;
            if (featureType == typeof(IExternalObjectsRenderInterfaceContextFeature))
                return _externalObjectsFeature;
            return null;
        }
        
        public void AddPostDispose(Action dispose)
        {
            lock (_postDisposeCallbacks)
                _postDisposeCallbacks.Add(dispose);
        }
    }
}
