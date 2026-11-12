using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Impeller;

/// <summary>An offscreen layer that can be sampled as a texture.</summary>
internal interface IImpellerOffscreenLayer : IDrawingContextLayerImpl, IImpellerTextureSource
{
    /// <summary>Copies the pixels to a raster image.</summary>
    SKImage Snapshot();
}

/// <summary>A bitmap that draws its content straight into the target instead of through a texture.</summary>
internal interface IImpellerInlineBitmap : IBitmapImpl
{
    void Draw(ImpellerDrawingContext context, double opacity, Rect sourceRect, Rect destRect);
}
