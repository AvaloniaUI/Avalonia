using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.OpenGL;
using Avalonia.Platform;

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
            using (var gr = gpu.TryGetGrContext())
            {
                var renderTargetSize = gr?.Value.MaxRenderTargetSize;
                if (renderTargetSize.HasValue)
                    MaxOffscreenRenderTargetPixelSize =
                        new PixelSize(renderTargetSize.Value, renderTargetSize.Value);
            }
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


    public PixelSize? MaxOffscreenRenderTargetPixelSize { get; }
    
    public IDrawingContextLayerImpl CreateOffscreenRenderTarget(PixelSize pixelSize, Vector scaling,
        bool enableTextAntialiasing)
    {
        using (var gr = _gpu?.TryGetGrContext())
        {
            var createInfo = new SurfaceRenderTarget.CreateInfo
            {
                Width = pixelSize.Width,
                Height = pixelSize.Height,
                Dpi = scaling * 96,
                Format = null,
                DisableTextLcdRendering = !enableTextAntialiasing,
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
