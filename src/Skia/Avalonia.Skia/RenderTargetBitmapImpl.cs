using System.IO;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia;

internal class RenderTargetBitmapImpl : WriteableBitmapImpl,
    IRenderTargetBitmapImpl,
    IFramebufferPlatformSurface
{
    private readonly FramebufferRenderTarget _renderTarget;
    
    public RenderTargetBitmapImpl(PixelSize size, Vector dpi) : base(size, dpi, 
        SKImageInfo.PlatformColorType == SKColorType.Rgba8888 ? PixelFormats.Rgba8888 : PixelFormat.Bgra8888,
        Platform.AlphaFormat.Premul)
    {
        _renderTarget = new FramebufferRenderTarget(this);
    }

    IDrawingContextImpl IRenderTarget.CreateDrawingContext(bool useScaledDrawing) =>
        _renderTarget.CreateDrawingContext(useScaledDrawing);

    public bool IsCorrupted => false;
    
    public override void Dispose()
    {
        _renderTarget.Dispose();
        base.Dispose();
    }

    public IFramebufferRenderTarget CreateFramebufferRenderTarget() => new FuncFramebufferRenderTarget(Lock);
}