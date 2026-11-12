using System;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Utilities;
using NImpeller;

namespace Avalonia.Impeller;

partial class ImpellerDrawingContext
{
    /// <summary>Renders tile or scene brush content into a texture and returns it as an image color source.</summary>
    internal ImpellerColorSource? MaterializeTileBrush(IBrush brush, Rect targetBounds)
    {
        if (targetBounds.Width < 1 || targetBounds.Height < 1)
            return null;

        ISceneBrushContent? sceneContent = brush switch
        {
            ISceneBrush sb => sb.CreateContent(),
            ISceneBrushContent c => c,
            _ => null
        };

        var scale = DeviceScale;

        try
        {
            ITileBrush tileBrush;
            IBitmapImpl contentBitmap;
            Size contentSize;
            IImpellerOffscreenLayer? sceneLayer = null;

            if (sceneContent is not null)
            {
                tileBrush = sceneContent.Brush;
                var rect = sceneContent.Rect;
                contentSize = rect.Size;
                if (contentSize.Width < 1 || contentSize.Height < 1)
                    return null;

                sceneLayer = RenderSceneContentToLayer(sceneContent, rect);
                contentBitmap = sceneLayer;
            }
            else if (brush is IImageBrush { Source.Bitmap.Item: { } bmp } image)
            {
                tileBrush = image;
                contentBitmap = bmp;
                // The intermediate is rendered at device scale, as Skia does.
                contentSize = new Size(bmp.PixelSize.Width / scale, bmp.PixelSize.Height / scale);
            }
            else
            {
                return null;
            }

            var calc = new TileBrushCalculator(tileBrush, contentSize, targetBounds.Size);
            if (calc.IntermediateSize.Width < 1 || calc.IntermediateSize.Height < 1)
                return null;

            var interW = Math.Max(1, (int)Math.Ceiling(calc.IntermediateSize.Width * scale));
            var interH = Math.Max(1, (int)Math.Ceiling(calc.IntermediateSize.Height * scale));
            var interLayer = _context.RenderLayerPool.Rent(new PixelSize(interW, interH));

            using (var lc = interLayer.CreateDrawingContext())
            {
                lc.Clear(Colors.Transparent);
                lc.Transform = Matrix.CreateScale(scale, scale);
                lc.PushClip(calc.IntermediateClip);
                lc.Transform = calc.IntermediateTransform * Matrix.CreateScale(scale, scale);
                var srcRect = new Rect(0, 0, contentBitmap.PixelSize.Width, contentBitmap.PixelSize.Height);
                var dstRect = new Rect(0, 0, contentSize.Width, contentSize.Height);
                lc.DrawBitmap(contentBitmap, 1, srcRect, dstRect);
                lc.PopClip();
            }

            // The display list samples these until the frame is done.
            if (sceneLayer is not null)
                _frameResources.Add(ReturnLayer(sceneLayer));
            _frameResources.Add(ReturnLayer(interLayer));

            var texture = interLayer.GetTexture(_context);
            if (texture is null)
                return null;

            var (tileX, tileY) = ToImpellerTileModes(tileBrush.TileMode);
            var tiled = tileBrush.TileMode != TileMode.None;

            // Untiled, the intermediate covers the target; tiled, it holds one tile at DestinationRect.
            var destGeom = tiled
                ? new Rect(targetBounds.X + calc.DestinationRect.X, targetBounds.Y + calc.DestinationRect.Y,
                           calc.DestinationRect.Width, calc.DestinationRect.Height)
                : targetBounds;

            var sx = destGeom.Width / interW;
            var sy = destGeom.Height / interH;
            var transform = Matrix.CreateScale(sx, sy) * Matrix.CreateTranslation(destGeom.X, destGeom.Y);

            transform = ApplyBrushTransform(tileBrush, targetBounds, transform);

            return texture.ColorSourceCreateImageNew(tileX, tileY,
                ImpellerTextureSampling.kImpellerTextureSamplingLinear, transform.ToImpeller());
        }
        finally
        {
            if (brush is ISceneBrush)
                sceneContent?.Dispose();
        }
    }


    private IDisposable ReturnLayer(IImpellerOffscreenLayer layer) =>
        new DisposeAction(() => _context.RenderLayerPool.Return(layer));

    private IImpellerOffscreenLayer RenderSceneContentToLayer(ISceneBrushContent content, Rect rect)
    {
        var scale = DeviceScale;
        var w = Math.Max(1, (int)Math.Ceiling(rect.Width * scale));
        var h = Math.Max(1, (int)Math.Ceiling(rect.Height * scale));
        var layer = _context.RenderLayerPool.Rent(new PixelSize(w, h));

        using var lc = layer.CreateDrawingContext();
        lc.Clear(Colors.Transparent);
        lc.Transform = Matrix.CreateScale(scale, scale);
        var offset = rect.TopLeft == default ? (Matrix?)null : Matrix.CreateTranslation(-rect.X, -rect.Y);
        content.Render(lc, offset);
        return layer;
    }

    private static Matrix ApplyBrushTransform(ITileBrush tileBrush, Rect targetBounds, Matrix transform)
    {
        if (tileBrush.Transform is null)
            return transform;

        var origin = tileBrush.TransformOrigin.ToPixels(targetBounds);
        var offset = Matrix.CreateTranslation(origin);
        var brushTransform = (-offset) * tileBrush.Transform.Value * offset;
        return transform * brushTransform;
    }

    private static (ImpellerTileMode X, ImpellerTileMode Y) ToImpellerTileModes(TileMode mode) => mode switch
    {
        TileMode.None => (ImpellerTileMode.kImpellerTileModeDecal, ImpellerTileMode.kImpellerTileModeDecal),
        TileMode.Tile => (ImpellerTileMode.kImpellerTileModeRepeat, ImpellerTileMode.kImpellerTileModeRepeat),
        TileMode.FlipX => (ImpellerTileMode.kImpellerTileModeMirror, ImpellerTileMode.kImpellerTileModeRepeat),
        TileMode.FlipY => (ImpellerTileMode.kImpellerTileModeRepeat, ImpellerTileMode.kImpellerTileModeMirror),
        TileMode.FlipXY => (ImpellerTileMode.kImpellerTileModeMirror, ImpellerTileMode.kImpellerTileModeMirror),
        _ => (ImpellerTileMode.kImpellerTileModeDecal, ImpellerTileMode.kImpellerTileModeDecal)
    };
}
