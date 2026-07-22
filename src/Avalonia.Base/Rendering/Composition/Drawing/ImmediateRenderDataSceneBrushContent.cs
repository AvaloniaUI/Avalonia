using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Drawing;

internal class ImmediateRenderDataSceneBrushContent : ISceneBrushContent
{
    private RenderDataStream? _stream;

    public ImmediateRenderDataSceneBrushContent(ITileBrush brush, RenderDataStream stream, Rect? rect,
        bool useScalableRasterization, bool containsCompositorResources = false, bool containsMutableResources = false)
    {
        Brush = brush;
        _stream = stream;
        UseScalableRasterization = useScalableRasterization;
        ContainsCompositorResources = containsCompositorResources;
        ContainsMutableResources = containsMutableResources;
        Rect = rect ?? ServerCompositionRenderData.ApplyRenderBoundsRounding(stream.CalculateBounds()) ?? default;
    }

    public ITileBrush Brush { get; }
    public Rect Rect { get; }

    /// <summary>
    /// True when the recorded content references compositor-bound render data
    /// (a compositor-bound <see cref="Rendering.Composition.DrawingRecording"/> was
    /// drawn into the content). Such content is only valid transiently on the UI
    /// thread and must not be embedded into an immutable recording.
    /// </summary>
    public bool ContainsCompositorResources { get; }

    /// <summary>
    /// True when the recorded content captured mutable (non-immutable) brushes or
    /// pens. Such content reads live objects when rendered and must not be
    /// embedded into an immutable recording replayed on the render thread.
    /// </summary>
    public bool ContainsMutableResources { get; }

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

/// <summary>
/// Wraps scene-brush content for embedding inside an immutable
/// <see cref="Rendering.Composition.DrawingRecording"/>. The wrapped content's
/// <see cref="ISceneBrushContent.Brush"/> is typically the live mutable brush whose
/// properties cannot be read on the render thread, so the tile-brush properties are
/// snapshotted into an <see cref="ImmutableSceneBrush"/> at record time.
/// </summary>
internal sealed class EmbeddedSceneBrushContent : ISceneBrushContent
{
    private readonly ISceneBrushContent _inner;
    private readonly ImmutableSceneBrush _brush;

    public EmbeddedSceneBrushContent(ISceneBrushContent inner)
    {
        _inner = inner;
        _brush = new ImmutableSceneBrush(inner.Brush);
        if (inner is ImmediateRenderDataSceneBrushContent immediate)
        {
            ContainsCompositorResources = immediate.ContainsCompositorResources;
            ContainsMutableResources = immediate.ContainsMutableResources;
        }
    }

    /// <summary>Pass-through of the wrapped content's resource flags; false for foreign content types.</summary>
    public bool ContainsCompositorResources { get; }

    /// <inheritdoc cref="ContainsCompositorResources"/>
    public bool ContainsMutableResources { get; }

    public ITileBrush Brush => _brush;
    public Rect Rect => _inner.Rect;
    public double Opacity => _brush.Opacity;
    public ITransform? Transform => _brush.Transform;
    public RelativePoint TransformOrigin => _brush.TransformOrigin;
    public bool UseScalableRasterization => _inner.UseScalableRasterization;
    public void Render(IDrawingContextImpl context, Matrix? transform) => _inner.Render(context, transform);
    public void Dispose() => _inner.Dispose();
}
