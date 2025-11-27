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
    public static void Render(Visual visual, DrawingContext context)
    {
        RenderInternal(context, visual, new Rect(visual.Bounds.Size));
    }

    public static void Render(DrawingContext context, Visual visual, Rect clipRect)
    {
        using (context.PushTransform(new TranslateTransform(-clipRect.Position.X, -clipRect.Position.Y).Value))
        using (context.PushClip(clipRect))
        {
            Render(visual, context);
        }
    }

    private static void RenderInternal(DrawingContext context, Visual visual, Rect bounds)
    {
        if (!visual.IsVisible || visual.Opacity is not (var opacity and > 0))
        {
            return;
        }

        var renderTransform = default(Matrix?);
        if (visual.RenderTransform != null)
        {
            var origin = visual.RenderTransformOrigin.ToPixels(new Size(visual.Bounds.Width, visual.Bounds.Height));
            var offset = Matrix.CreateTranslation(origin);
            renderTransform = (-offset) * visual.RenderTransform.Value * (offset);
        }

        var clipBounds = new Rect(bounds.Size);

        using (visual.RenderOptions != default ? context.PushRenderOptions(visual.RenderOptions) : default(DrawingContext.PushedState?))
        using (context.PushTransform(new TranslateTransform(bounds.Position.X, bounds.Position.Y).Value))
        using (renderTransform is { } matrix ? context.PushTransform(matrix) : default(DrawingContext.PushedState?))
        using (visual.HasMirrorTransform ? context.PushTransform(new Matrix(-1.0, 0.0, 0.0, 1.0, visual.Bounds.Width, 0)) : default(DrawingContext.PushedState?))
        using (context.PushOpacity(opacity))
        using (visual switch
        {
            { ClipToBounds: true } and IVisualWithRoundRectClip roundClipVisual => context.PushClip(new RoundedRect(clipBounds, roundClipVisual.ClipToBoundsRadius)),
            { ClipToBounds: true } => context.PushClip(clipBounds),
            _ => default(DrawingContext.PushedState?)
        })
        using (visual.Clip != null ? context.PushGeometryClip(visual.Clip) : default(DrawingContext.PushedState?))
        using (visual.OpacityMask != null ? context.PushOpacityMask(visual.OpacityMask, bounds) : default(DrawingContext.PushedState?))
        {
            visual.Render(context);

            IEnumerable<Visual> childrenEnumerable = visual.HasNonUniformZIndexChildren
                ? visual.VisualChildren.OrderBy(x => x, ZIndexComparer.Instance)
                : visual.VisualChildren;

            foreach (var child in childrenEnumerable)
            {
                RenderInternal(context, child, child.Bounds);
            }
        }
    }
}
