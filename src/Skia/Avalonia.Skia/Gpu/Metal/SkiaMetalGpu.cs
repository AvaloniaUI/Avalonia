using System;
using System.Collections.Generic;
using Avalonia.Metal;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using SkiaSharp;

namespace Avalonia.Skia.Metal;

internal class SkiaMetalGpu : ISkiaGpu
{
    private GRContext? _context;
    private readonly IMetalDevice _device;
    private readonly SkiaMetalExternalObjectsFeature? _externalObjects;

    internal GRContext GrContext => _context ?? throw new ObjectDisposedException(nameof(SkiaMetalGpu));

    public SkiaMetalGpu(IMetalDevice device, long? maxResourceBytes)
    {
        _context = GRContext.CreateMetal(
                       new GRMtlBackendContext { DeviceHandle = device.Device, QueueHandle = device.CommandQueue, },
                       new GRContextOptions { AvoidStencilBuffers = true })
                   ?? throw new InvalidOperationException("Unable to create GRContext from Metal device.");
        _device = device;
        if (device.TryGetFeature<IMetalExternalObjectsFeature>() is { } externalObjects)
            _externalObjects = new SkiaMetalExternalObjectsFeature(this, externalObjects);
        if (maxResourceBytes.HasValue)
            _context.SetResourceCacheLimit(maxResourceBytes.Value);
    }

    public void Dispose()
    {
        _context?.Dispose();
        _context = null;
    }

    public object? TryGetFeature(Type featureType)
    {
        if (featureType == typeof(IExternalObjectsHandleWrapRenderInterfaceContextFeature))
            return _device.TryGetFeature(featureType);
        if (featureType == typeof(IExternalObjectsRenderInterfaceContextFeature))
            return _externalObjects;
        return null;
    }

    public bool IsLost => false;
    public IDisposable EnsureCurrent() => _device.EnsureCurrent();
    public IPlatformGraphicsContext? PlatformGraphicsContext => _device;

    public IScopedResource<GRContext> TryGetGrContext() =>
        ScopedResource<GRContext>.Create(GrContext, EnsureCurrent().Dispose);

    public ISkiaGpuRenderTarget? TryCreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces)
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

    public bool IsReadyToCreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces)
    {
        foreach (var surface in surfaces)
        {
            if (surface is IMetalPlatformSurface)
            {
                return surface.IsReady;
            }
        }

        return false;
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

        public ISkiaGpuRenderSession BeginRenderingSession(IRenderTarget.RenderTargetSceneInfo sceneInfo)
        {
            // TODO: use expectedPixelSize
            var session = (_target ?? throw new ObjectDisposedException(nameof(SkiaMetalRenderTarget))).BeginRendering();
            var backendTarget = new GRBackendRenderTarget(session.Size.Width, session.Size.Height,
                new GRMtlTextureInfo(session.Texture));

            var surface = SKSurface.Create(_gpu._context!, backendTarget,
                session.IsYFlipped ? GRSurfaceOrigin.BottomLeft : GRSurfaceOrigin.TopLeft,
                SKColorType.Bgra8888);

            return new SkiaMetalRenderSession(_gpu, surface, session, backendTarget);
        }

        public bool IsCorrupted => false;

        public bool IsReady => _target?.IsReady ?? false;
    }
    
    internal class SkiaMetalRenderSession : ISkiaGpuRenderSession
    {
        private readonly SkiaMetalGpu _gpu;
        private SKSurface? _surface;
        private IMetalPlatformSurfaceRenderingSession? _session;
        private GRBackendRenderTarget? _backendTarget;

        public SkiaMetalRenderSession(SkiaMetalGpu gpu,
            SKSurface surface,
            IMetalPlatformSurfaceRenderingSession session,
            GRBackendRenderTarget backendTarget)
        {
            _gpu = gpu;
            _surface = surface;
            _session = session;
            _backendTarget = backendTarget;
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
            _backendTarget?.Dispose();
            _backendTarget = null;
        }

        public GRContext GrContext => _gpu._context!;
        public SKSurface SkSurface => _surface!;
        public double ScaleFactor => _session!.Scaling;

        public GRSurfaceOrigin SurfaceOrigin =>
            _session!.IsYFlipped ? GRSurfaceOrigin.BottomLeft : GRSurfaceOrigin.TopLeft;
    }


    public ISkiaSurface? TryCreateSurface(PixelSize size, ISkiaGpuRenderSession? session) => null;
}
