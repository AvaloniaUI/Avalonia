using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Drawing;

internal class ImmediateRenderDataSceneBrushContent : ISceneBrushContent
{
    private RenderDataStream? _stream;

    public ImmediateRenderDataSceneBrushContent(ITileBrush brush, RenderDataStream stream, Rect? rect,
        bool useScalableRasterization)
    {
        Brush = brush;
        _stream = stream;
        UseScalableRasterization = useScalableRasterization;
        Rect = rect ?? ServerCompositionRenderData.ApplyRenderBoundsRounding(stream.CalculateBounds()) ?? default;
    }

    public ITileBrush Brush { get; }
    public Rect Rect { get; }

    public double Opacity => Brush.Opacity;
    public ITransform? Transform => Brush.Transform;
    public RelativePoint TransformOrigin => Brush.TransformOrigin;

    public void Dispose()
    {
        if (_stream == null)
            return;
        _stream.DisposeResources();
        _stream.Dispose();
        _stream = null;
    }

    private void Render(IDrawingContextImpl context) => _stream?.Replay(context);

    public void Render(IDrawingContextImpl context, Matrix? transform)
    {
        if (transform.HasValue)
        {
            var oldTransform = context.Transform;
            context.Transform = transform.Value * oldTransform;
            Render(context);
            context.Transform = oldTransform;
        }
        else
            Render(context);
    }

    public bool UseScalableRasterization { get; }
}
