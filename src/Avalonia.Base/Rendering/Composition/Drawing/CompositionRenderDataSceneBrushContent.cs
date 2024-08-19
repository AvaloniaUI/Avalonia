using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Drawing;

internal class CompositionRenderDataSceneBrushContent : ISceneBrushContent
{
    public ServerCompositionRenderData RenderData { get; }
    private readonly Rect? _rect;

    public record Properties(ServerCompositionRenderData RenderData, Rect? Rect, bool UseScalableRasterization);

    public CompositionRenderDataSceneBrushContent(ITileBrush brush, Properties properties)
    {
        Brush = brush;
        _rect = properties.Rect;
        UseScalableRasterization = properties.UseScalableRasterization;
        RenderData = properties.RenderData;
    }

    public ITileBrush Brush { get; }
    public Rect Rect => _rect ?? (RenderData?.Bounds?.ToRect() ?? default);

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
            RenderData.Render(context);
            context.Transform = oldTransform;
        }
        else
            RenderData.Render(context);
    }

    public bool UseScalableRasterization { get; }
}