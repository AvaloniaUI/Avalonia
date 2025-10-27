using System;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;
using Avalonia.Utilities;
using SkiaSharp;

namespace Avalonia.Skia;

/// <summary>
/// Renders to an offscreen GPU-backed GrSurface and copies render results into ILockedFramebuffer
/// </summary>
/// <param name="grContext">GrContext</param>
/// <param name="fb">framebuffer</param>
/// <param name="isLost">A callback to check if GrContext is still in valid state</param>
/// <param name="beginDraw">An optional callback to call before accessing GrContext (useful for OpenGL)</param>
/// <param name="disposer">An optional callback for disposing GPU-backed skia objects</param>
class GpuToCpuCopyRenderTarget(
    GRContext grContext,
    IFramebufferRenderTarget fb,
    Func<bool> isLost,
    Func<IDisposable>? beginDraw,
    Action<IDisposable>? disposer)
    : ISkiaGpuRenderTargetWithProperties
{
    private SKSurface? _surface;
    private PixelSize? _surfaceSize;

    public void Dispose()
    {
        fb.Dispose();
        DisposeSurface();
    }

    void DisposeSurface()
    {
        if (_surface != null)
        {
            if (disposer != null)
                disposer(_surface);
            else
                _surface.Dispose();
            _surface = null;
        }
    }


    class Session(GRContext context, SKSurface surface, double scaling, Action disp) : ISkiaGpuRenderSession
    {
        public void Dispose() => disp();

        public GRContext GrContext => context;
        public SKSurface SkSurface => surface;
        public double ScaleFactor => scaling;
        public GRSurfaceOrigin SurfaceOrigin => GRSurfaceOrigin.TopLeft;
    }

    public ISkiaGpuRenderSession BeginRenderingSession()
    {
        if (isLost())
            throw new PlatformGraphicsContextLostException();

        using var locked = fb.Lock().DisposeIfFailed();
        using var drawOp = beginDraw?.Invoke().DisposeIfFailed();

        if (_surfaceSize.HasValue && _surfaceSize.Value != locked.Value.Size)
            DisposeSurface();

        if (_surface == null)
        {
            var info = new SKImageInfo(locked.Value.Size.Width, locked.Value.Size.Height,
                SKColorType.Rgba8888, SKAlphaType.Premul);
            _surface = SKSurface.Create(grContext, false, info);
            if (_surface == null)
                throw new Exception($"Unable to create offscreen surface for rendering");
            _surfaceSize = locked.Value.Size;
        }

        var l = locked.SuccessReturn();
        var endDraw = drawOp?.SuccessReturn();
        return new Session(grContext, _surface, locked.Value.Dpi.X / 96, () =>
        {
            using (l)
            using (endDraw)
                _surface.ReadPixels(new SKImageInfo(l.Size.Width, l.Size.Height,
                        l.Format.ToSkColorType(),
                        l.Format == PixelFormat.Rgb565 ? SKAlphaType.Opaque : SKAlphaType.Premul),
                    l.Address, l.RowBytes, 0, 0);
        });
    }

    public bool IsCorrupted => isLost();

    // It's a persistent offscreen SKSurface, so no need to have an intermediate one on compositor side
    public RenderTargetProperties Properties => new()
    {
        IsSuitableForDirectRendering = true,
        RetainsPreviousFrameContents = true
    };
}