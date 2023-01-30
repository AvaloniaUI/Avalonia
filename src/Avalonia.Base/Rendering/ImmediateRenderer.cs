using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    /// <summary>
    /// This class is used to render the visual tree into a DrawingContext by doing
    /// a simple tree traversal.
    /// It's currently used mostly for RenderTargetBitmap.Render and VisualBrush
    /// </summary>
    internal class ImmediateRenderer : IVisualBrushRenderer//, IRenderer
    {
        /// <summary>
        /// Renders a visual to a render target.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="target">The render target.</param>
        public static void Render(Visual visual, IRenderTarget target)
        {
            using var context = new DrawingContext(target.CreateDrawingContext(new ImmediateRenderer()));
            Render(context, visual, visual.Bounds);
        }

        /// <summary>
        /// Renders a visual to a drawing context.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="context">The drawing context.</param>
        public static void Render(Visual visual, DrawingContext context)
        {
            Render(context, visual, visual.Bounds);
        }
        

        /// <inheritdoc/>
        Size IVisualBrushRenderer.GetRenderTargetSize(IVisualBrush brush)
        {
            (brush.Visual as IVisualBrushInitialize)?.EnsureInitialized();
            return brush.Visual?.Bounds.Size ?? default;
        }

        /// <inheritdoc/>
        void IVisualBrushRenderer.RenderVisualBrush(IDrawingContextImpl context, IVisualBrush brush)
        {
            var visual = brush.Visual;
            Render(new DrawingContext(context), visual, visual.Bounds);
        }

        internal static void Render(Visual visual, DrawingContext context, bool updateTransformedBounds)
        {
            Render(context, visual, visual.Bounds);
        }

        private static Rect GetTransformedBounds(Visual visual)
        {
            if (visual.RenderTransform == null)
            {
                return visual.Bounds;
            }
            else
            {
                var origin = visual.RenderTransformOrigin.ToPixels(new Size(visual.Bounds.Width, visual.Bounds.Height));
                var offset = Matrix.CreateTranslation(visual.Bounds.Position + origin);
                var m = (-offset) * visual.RenderTransform.Value * (offset);
                return visual.Bounds.TransformToAABB(m);
            }
        }


        private static void Render(DrawingContext context, Visual visual, Rect clipRect)
        {
            var opacity = visual.Opacity;
            var clipToBounds = visual.ClipToBounds;
            var bounds = new Rect(visual.Bounds.Size);

            if (visual.IsVisible && opacity > 0)
            {
                var m = Matrix.CreateTranslation(visual.Bounds.Position);

                var renderTransform = Matrix.Identity;
                
                // this should be calculated BEFORE renderTransform
                if (visual.HasMirrorTransform)
                {
                    var mirrorMatrix = new Matrix(-1.0, 0.0, 0.0, 1.0, visual.Bounds.Width, 0);
                    renderTransform *= mirrorMatrix;
                }

                if (visual.RenderTransform != null)
                {
                    var origin = visual.RenderTransformOrigin.ToPixels(new Size(visual.Bounds.Width, visual.Bounds.Height));
                    var offset = Matrix.CreateTranslation(origin);
                    var finalTransform = (-offset) * visual.RenderTransform.Value * (offset);
                    renderTransform *= finalTransform;
                }

                m = renderTransform * m;

                if (clipToBounds)
                {
                    if (visual.RenderTransform != null)
                    {
                        clipRect = new Rect(visual.Bounds.Size);
                    }
                    else
                    {
                        clipRect = clipRect.Intersect(new Rect(visual.Bounds.Size));
                    }
                }

                using (context.PushPostTransform(m))
                using (context.PushOpacity(opacity))
                using (clipToBounds
#pragma warning disable CS0618 // Type or member is obsolete
                    ? visual is IVisualWithRoundRectClip roundClipVisual
                        ? context.PushClip(new RoundedRect(bounds, roundClipVisual.ClipToBoundsRadius))
                        : context.PushClip(bounds) 
                    : default)
#pragma warning restore CS0618 // Type or member is obsolete

                using (visual.Clip != null ? context.PushGeometryClip(visual.Clip) : default)
                using (visual.OpacityMask != null ? context.PushOpacityMask(visual.OpacityMask, bounds) : default)
                using (context.PushTransformContainer())
                {
                    visual.Render(context);
                    
                    var childrenEnumerable = visual.HasNonUniformZIndexChildren
                        ? visual.VisualChildren.OrderBy(x => x, ZIndexComparer.Instance)
                        : (IEnumerable<Visual>)visual.VisualChildren;
                    
                    foreach (var child in childrenEnumerable)
                    {
                        var childBounds = GetTransformedBounds(child);

                        if (!child.ClipToBounds || clipRect.Intersects(childBounds))
                        {
                            var childClipRect = child.RenderTransform == null
                                ? clipRect.Translate(-childBounds.Position)
                                : clipRect;
                            Render(context, child, childClipRect);
                        }
                    }
                }
            }
        }
    }
}
