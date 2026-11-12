using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Metal;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using NImpeller;

namespace Avalonia.Impeller;

/// <summary>
/// An Impeller context over Metal. Impeller uses the system default device, which is also the one Avalonia's
/// Metal surfaces are created with.
/// </summary>
internal sealed class ImpellerMetalBackendContext : ImpellerBackendContext
{
    private readonly IMetalDevice _device;

    private ImpellerMetalBackendContext(IMetalDevice device, ImpellerContext impellerContext)
        : base(impellerContext) =>
        _device = device;

    public static ImpellerMetalBackendContext Create(IMetalDevice device)
    {
        using (device.EnsureCurrent())
        {
            var impellerContext = ImpellerContext.CreateMetalNew()
                ?? throw new PlatformNotSupportedException("Failed to create an Impeller Metal context.");
            return new ImpellerMetalBackendContext(device, impellerContext);
        }
    }

    internal override IImpellerOffscreenLayer CreateTextureLayer(PixelSize size, Vector dpi) =>
        new ImpellerSkiaRasterLayer(size, dpi);

    public override IDrawingContextLayerImpl CreateOffscreenRenderTarget(PixelSize pixelSize, Vector scaling,
        bool enableTextAntialiasing) => new ImpellerRecordedLayer(pixelSize, scaling * 96);

    // The render thread has no run loop to drain what Impeller autoreleases.
    internal override IDisposable EnterNativeScope() => new ObjCAutoreleasePool();

    public override IRenderTarget CreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces)
    {
        foreach (var surface in surfaces)
        {
            if (surface is IMetalPlatformSurface metalSurface)
                return new ImpellerMetalRenderTarget(this, metalSurface.CreateMetalRenderTarget(_device));
        }

        throw new NotSupportedException("The Impeller Metal backend needs an IMetalPlatformSurface.");
    }

    public override bool IsReadyToCreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces) =>
        surfaces.Any(s => s is IMetalPlatformSurface { IsReady: true });

    public override bool IsLost => _device.IsLost;
}
