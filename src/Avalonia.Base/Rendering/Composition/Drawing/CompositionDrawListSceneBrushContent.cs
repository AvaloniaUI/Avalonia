using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Drawing;

internal class CompositionDrawListSceneBrushContent : ISceneBrushContent
{
    private readonly CompositionDrawList _drawList;

    public CompositionDrawListSceneBrushContent(ImmutableTileBrush brush, CompositionDrawList drawList, Rect rect, bool useScalableRasterization)
    {
        Brush = brush;
        Rect = rect;
        UseScalableRasterization = useScalableRasterization;
        _drawList = drawList;
    }

    public ITileBrush Brush { get; }
    public Rect Rect { get; }
    
    public double Opacity => Brush.Opacity;
    public ITransform? Transform => Brush.Transform;
    public RelativePoint TransformOrigin => Brush.TransformOrigin;
    
    public void Dispose() => _drawList.Dispose();

    public void Render(IDrawingContextImpl context, Matrix? transform)
    {
        if (transform.HasValue)
            _drawList.Render(context, transform.Value);
        else
            _drawList.Render(context);
    }

    public bool UseScalableRasterization { get; }
}