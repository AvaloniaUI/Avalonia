using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Metal;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia.Metal;

internal class SkiaMetalGpu : ISkiaGpu, ISkiaGpuWithPlatformGraphicsContext
{
    private SkiaMetalApi _api = new();
    private GRContext? _context;
    private readonly IMetalDevice _device;

    public SkiaMetalGpu(IMetalDevice device, long? maxResourceBytes)
    {
        _context = _api.CreateContext(device.Device, device.CommandQueue,
            new GRContextOptions() { AvoidStencilBuffers = true });
        _device = device;
        if (maxResourceBytes.HasValue)
            _context.SetResourceCacheLimit(maxResourceBytes.Value);
    }

    public void Dispose()
    {
        _context?.Dispose();
        _context = null;
    }

    public object? TryGetFeature(Type featureType) => null;

    public bool IsLost => false;
    public IDisposable EnsureCurrent() => _device.EnsureCurrent();
    public IPlatformGraphicsContext? PlatformGraphicsContext => _device;

    public ISkiaGpuRenderTarget? TryCreateRenderTarget(IEnumerable<object> surfaces)
    {
        foreach (var surface in surfaces)
        {
            if (surface is IMetalPlatformSurface metalSurface)
            {
                var target = metalSurface.CreateMetalRenderTarget(_device);
                return new SkiaMetalRenderTarget(this, target);
            }
        }

        return null;
    }

    public class SkiaMetalRenderTarget : ISkiaGpuRenderTarget
    {
        private readonly SkiaMetalGpu _gpu;
        private IMetalPlatformSurfaceRenderTarget? _target;

        public SkiaMetalRenderTarget(SkiaMetalGpu gpu, IMetalPlatformSurfaceRenderTarget target)
        {
            _gpu = gpu;
            _target = target;
        }

        public void Dispose()
        {
            _target?.Dispose();
            _target = null;
        }

        public ISkiaGpuRenderSession BeginRenderingSession()
        {
            var session = (_target ?? throw new ObjectDisposedException(nameof(SkiaMetalRenderTarget))).BeginRendering();
            var backendTarget = _gpu._api.CreateBackendRenderTarget(session.Size.Width, session.Size.Height,
                1, session.Texture);

            var surface = SKSurface.Create(_gpu._context!, backendTarget,
                session.IsYFlipped ? GRSurfaceOrigin.BottomLeft : GRSurfaceOrigin.TopLeft,
                SKColorType.Bgra8888);
            
            return new SkiaMetalRenderSession(_gpu, surface, session);
        }

        public bool IsCorrupted => false;
    }
    
    internal class SkiaMetalRenderSession : ISkiaGpuRenderSession
    {
        private readonly SkiaMetalGpu _gpu;
        private SKSurface? _surface;
        private IMetalPlatformSurfaceRenderingSession? _session;

        public SkiaMetalRenderSession(SkiaMetalGpu gpu, 
            SKSurface surface,
            IMetalPlatformSurfaceRenderingSession session)
        {
            _gpu = gpu;
            _surface = surface;
            _session = session;
        }

        public void Dispose()
        {
            _surface?.Canvas.Flush();
            _surface?.Flush();
            _gpu._context?.Flush();
            
            _surface?.Dispose();
            _surface = null;
            _session?.Dispose();
            _session = null;
        }

        public GRContext GrContext => _gpu._context!;
        public SKSurface SkSurface => _surface!;
        public double ScaleFactor => _session!.Scaling;

        public GRSurfaceOrigin SurfaceOrigin =>
            _session!.IsYFlipped ? GRSurfaceOrigin.BottomLeft : GRSurfaceOrigin.TopLeft;
    }


    public ISkiaSurface? TryCreateSurface(PixelSize size, ISkiaGpuRenderSession? session) => null;
}
