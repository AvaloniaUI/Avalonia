using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;

namespace Avalonia.Skia;

internal class SkiaContext : IPlatformRenderInterfaceContext
{
    private ISkiaGpu? _gpu;

    public SkiaContext(ISkiaGpu? gpu)
    {
        _gpu = gpu;
    }
    
    public void Dispose()
    {
        _gpu?.Dispose();
        _gpu = null;
    }

    /// <inheritdoc />
    public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
    {
        if (surfaces is not IList)
            surfaces = surfaces.ToList();

        if (_gpu?.TryCreateRenderTarget(surfaces) is { } gpuRenderTarget)
        {
            return new SkiaGpuRenderTarget(_gpu, gpuRenderTarget);
        }

        foreach (var surface in surfaces)
        {
            if (surface is IFramebufferPlatformSurface framebufferSurface)
                return new FramebufferRenderTarget(framebufferSurface);
        }

        throw new NotSupportedException(
            "Don't know how to create a Skia render target from any of provided surfaces");
    }

    public bool IsLost => _gpu?.IsLost ?? false;

    public object? TryGetFeature(Type featureType) => _gpu?.TryGetFeature(featureType);
}
