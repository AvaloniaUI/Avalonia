using Avalonia.Platform.Surfaces;
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
        _renderTarget = new FramebufferRenderTarget(this, true);
    }
    
    public IDrawingContextImpl CreateDrawingContext()
    {
        return _renderTarget.CreateDrawingContext(new IRenderTarget.RenderTargetSceneInfo(
            PixelSize, Dpi.X / 96.0), out _);
    }


    public bool IsCorrupted => false;
    
    public override void Dispose()
    {
        _renderTarget.Dispose();
        base.Dispose();
    }
    
    public IFramebufferRenderTarget CreateFramebufferRenderTarget() => new FuncFramebufferRenderTarget(Lock);
}
