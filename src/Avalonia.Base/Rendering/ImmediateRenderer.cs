using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Rendering;

/// <summary>
/// This class is used to render the visual tree into a DrawingContext by doing
/// a simple tree traversal.
/// It's currently used mostly for RenderTargetBitmap.Render and VisualBrush
/// </summary>
internal class ImmediateRenderer
{
    /// <summary>
    /// Renders a visual to a drawing context.
    /// </summary>
    /// <param name="visual">The visual.</param>
    /// <param name="context">The drawing context.</param>
    public static void Render(DrawingContext context, Visual visual)
        => Render(context, visual, new Rect(visual.Bounds.Size));

    public static void Render(DrawingContext context, Visual visual, Rect clipRect)
    {
        using (context.PushTransform(Matrix.CreateTranslation(-clipRect.Position.X, -clipRect.Position.Y)))
        using (context.PushClip(clipRect))
        {
            Render(context, visual, new Rect(visual.Bounds.Size), Matrix.Identity, new Rect(clipRect.Size));
        }
    }

    private static void Render(DrawingContext context, Visual visual, Rect bounds, Matrix parentTransform, Rect clipRect)
    {
        if (!visual.IsVisible || visual.Opacity is not (var opacity and > 0))
        {
            return;
        }

        var rect = new Rect(bounds.Size);
        Matrix transform;

        if (visual.RenderTransform?.Value is { } rt)
        {
            var origin = visual.RenderTransformOrigin.ToPixels(visual.Bounds.Size);
            var offset = Matrix.CreateTranslation(origin);
            transform = (-offset) * rt * (offset) * Matrix.CreateTranslation(bounds.Position);
        }
        else
        {
            transform = Matrix.CreateTranslation(bounds.Position);
        }

        using (visual.RenderOptions != default ? context.PushRenderOptions(visual.RenderOptions) : default(DrawingContext.PushedState?))
        using (context.PushTransform(transform))
        using (visual.HasMirrorTransform ? context.PushTransform(new Matrix(-1.0, 0.0, 0.0, 1.0, visual.Bounds.Width, 0)) : default(DrawingContext.PushedState?))
        using (context.PushOpacity(opacity))
        using (visual switch
        {
            { ClipToBounds: true } and IVisualWithRoundRectClip roundClipVisual => context.PushClip(new RoundedRect(rect, roundClipVisual.ClipToBoundsRadius)),
            { ClipToBounds: true } => context.PushClip(rect),
            _ => default(DrawingContext.PushedState?)
        })
        using (visual.Clip is { } clip ? context.PushGeometryClip(clip) : default(DrawingContext.PushedState?))
        using (visual.OpacityMask is { } opctMask ? context.PushOpacityMask(opctMask, rect) : default(DrawingContext.PushedState?))
        {
            var totalTransform = transform * parentTransform;
            var visualBounds = rect.TransformToAABB(totalTransform);

            if (visualBounds.Intersects(clipRect))
            {
                visual.Render(context);
            }

            IEnumerable<Visual> childrenEnumerable = visual.HasNonUniformZIndexChildren
                ? visual.VisualChildren.OrderBy(x => x, ZIndexComparer.Instance)
                : visual.VisualChildren;

            if (visual.ClipToBounds)
            {
                totalTransform = Matrix.Identity;
                clipRect = rect;
            }

            foreach (var child in childrenEnumerable)
            {
                Render(context, child, child.Bounds, totalTransform, clipRect);
            }
        }
    }
}
