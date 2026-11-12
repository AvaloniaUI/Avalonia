using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Skia;
using NImpeller;
using SkiaSharp;

namespace Avalonia.Impeller;

/// <summary>
/// A layer drawn by Skia on the CPU and uploaded when sampled, for backends where Impeller can't render to
/// textures.
/// </summary>
internal sealed class ImpellerSkiaRasterLayer : IImpellerOffscreenLayer, IDrawableBitmapImpl
{
    private readonly RenderTargetBitmapImpl _bitmap;
    private readonly ImpellerTextureCache _textures = new();

    public ImpellerSkiaRasterLayer(PixelSize size, Vector dpi)
    {
        _bitmap = new RenderTargetBitmapImpl(new PixelSize(System.Math.Max(1, size.Width),
            System.Math.Max(1, size.Height)), dpi);
    }

    public PixelSize PixelSize => _bitmap.PixelSize;
    public Vector Dpi => _bitmap.Dpi;
    public int Version => _bitmap.Version;
    public bool CanBlit => true;
    public bool IsCorrupted => false;

    public IDrawingContextImpl CreateDrawingContext() => _bitmap.CreateDrawingContext();

    public ImpellerTexture? GetTexture(ImpellerBackendContext context) =>
        _textures.Get(context, Version, c =>
        {
            using var image = _bitmap.GetSnapshot();
            return ImpellerBitmapTextures.Upload(c, image);
        });

    public SKImage Snapshot()
    {
        // The snapshot shares the bitmap's pixels, which go away with the layer.
        using var image = _bitmap.GetSnapshot();
        using var pixels = image.PeekPixels();
        return SKImage.FromPixelCopy(pixels);
    }

    public void Blit(IDrawingContextImpl context)
    {
        var dip = new Rect(0, 0, PixelSize.Width / (Dpi.X / 96.0), PixelSize.Height / (Dpi.Y / 96.0));
        context.DrawBitmap(this, 1.0, new Rect(0, 0, PixelSize.Width, PixelSize.Height), dip);
    }

    public void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKSamplingOptions samplingOptions,
        SKPaint paint) => _bitmap.Draw(context, sourceRect, destRect, samplingOptions, paint);

    public void Save(Stream stream, BitmapEncoderOptions options) => _bitmap.Save(stream, options);

    public void Dispose()
    {
        _textures.Dispose();
        _bitmap.Dispose();
    }
}
