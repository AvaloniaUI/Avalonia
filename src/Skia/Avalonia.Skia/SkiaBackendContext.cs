using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.OpenGL;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia;

internal class SkiaContext : IPlatformRenderInterfaceContext
{
    private ISkiaGpu? _gpu;

    public SkiaContext(ISkiaGpu? gpu)
    {
        _gpu = gpu;

        var features = new Dictionary<Type, object>();
        
        if (gpu != null)
        {
            void TryFeature<T>() where T : class
            {
                if (gpu!.TryGetFeature<T>() is { } feature)
                    features!.Add(typeof(T), feature);
            }
            // TODO12: extend ISkiaGpu with PublicFeatures instead
            TryFeature<IOpenGlTextureSharingRenderInterfaceContextFeature>();
            TryFeature<IExternalObjectsRenderInterfaceContextFeature>();
        }

        PublicFeatures = features;
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

    public IDrawingContextLayerImpl CreateOffscreenRenderTarget(PixelSize pixelSize, double scaling)
    {
        using (var gr = (_gpu as ISkiaGpuWithPlatformGraphicsContext)?.TryGetGrContext())
        {
            var createInfo = new SurfaceRenderTarget.CreateInfo
            {
                Width = pixelSize.Width,
                Height = pixelSize.Height,
                Dpi = new Vector(96 * scaling, 96 * scaling),
                Format = null,
                DisableTextLcdRendering = false,
                GrContext = gr?.Value,
                Gpu = _gpu,
                DisableManualFbo = true,
                Session = null
            };

            return new SurfaceRenderTarget(createInfo);
        }
    }

    public bool IsLost => _gpu?.IsLost ?? false;
    public IReadOnlyDictionary<Type, object> PublicFeatures { get; }

    public object? TryGetFeature(Type featureType) => _gpu?.TryGetFeature(featureType);
}
