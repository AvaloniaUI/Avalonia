using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Drawing;

internal class CompositionRenderDataSceneBrushContent : ISceneBrushContent
{
    public CompositionRenderData RenderData { get; }
    private readonly Rect? _rect;

    public CompositionRenderDataSceneBrushContent(ITileBrush brush, CompositionRenderData renderData, Rect? rect,
        bool useScalableRasterization)
    {
        Brush = brush;
        _rect = rect;
        UseScalableRasterization = useScalableRasterization;
        RenderData = renderData;
    }

    public ITileBrush Brush { get; }
    public Rect Rect => _rect ?? (RenderData.Server?.Bounds ?? default);

    public double Opacity => Brush.Opacity;
    public ITransform? Transform => Brush.Transform;
    public RelativePoint TransformOrigin => Brush.TransformOrigin;

    public void Dispose()
    {
        // No-op on server
    }

    public void Render(IDrawingContextImpl context, Matrix? transform)
    {
        if (transform.HasValue)
        {
            var oldTransform = context.Transform;
            context.Transform = transform.Value * oldTransform;
            RenderData.Server.Render(context);
            context.Transform = oldTransform;
        }
        else
            RenderData.Server.Render(context);
    }

    public bool UseScalableRasterization { get; }
}