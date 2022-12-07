using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    /// <summary>
    /// A renderer which renders the state of the visual tree without an intermediate scene graph
    /// representation.
    /// </summary>
    /// <remarks>
    /// The immediate renderer supports only clip-bound-based hit testing; a control's geometry is
    /// not taken into account.
    /// </remarks>
    public class ImmediateRenderer : RendererBase, IRenderer, IVisualBrushRenderer
    {
        private readonly Visual _root;
        private readonly IRenderRoot? _renderRoot;
        private bool _updateTransformedBounds = true;
        private IRenderTarget? _renderTarget;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmediateRenderer"/> class.
        /// </summary>
        /// <param name="root">The control to render.</param>
        public ImmediateRenderer(Visual root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _renderRoot = root as IRenderRoot;
        }

        private ImmediateRenderer(Visual root, bool updateTransformedBounds)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _renderRoot = root as IRenderRoot;
            _updateTransformedBounds = updateTransformedBounds;
        }

        /// <inheritdoc/>
        public bool DrawFps { get; set; }

        /// <inheritdoc/>
        public bool DrawDirtyRects { get; set; }

        /// <inheritdoc/>
        public event EventHandler<SceneInvalidatedEventArgs>? SceneInvalidated;

        /// <inheritdoc/>
        public void Paint(Rect rect)
        {
            if (_renderTarget == null)
            {
                _renderTarget = ((IRenderRoot)_root).CreateRenderTarget();
            }

            try
            {
                using (var context = new DrawingContext(_renderTarget.CreateDrawingContext(this)))
                {
                    context.PlatformImpl.Clear(Colors.Transparent);

                    using (context.PushTransformContainer())
                    {
                        Render(context, _root, _root.Bounds);
                    }

                    if (DrawDirtyRects)
                    {
                        var color = (uint)new Random().Next(0xffffff) | 0x44000000;
                        context.FillRectangle(
                            new SolidColorBrush(color),
                            rect);
                    }

                    if (DrawFps)
                    {
                        RenderFps(context, _root.Bounds, null);
                    }
                }
            }
            catch (RenderTargetCorruptedException ex)
            {
                Logger.TryGet(LogEventLevel.Information, LogArea.Animations)?.Log(this, "Render target was corrupted. Exception: {0}", ex);
                _renderTarget.Dispose();
                _renderTarget = null;
            }

            SceneInvalidated?.Invoke(this, new SceneInvalidatedEventArgs((IRenderRoot)_root, rect));
        }

        /// <inheritdoc/>
        public void Resized(Size size)
        {
        }

        /// <summary>
        /// Renders a visual to a render target.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="target">The render target.</param>
        public static void Render(Visual visual, IRenderTarget target)
        {
            using (var renderer = new ImmediateRenderer(visual, updateTransformedBounds: false))
            using (var context = new DrawingContext(target.CreateDrawingContext(renderer)))
            {
                renderer.Render(context, visual, visual.Bounds);
            }
        }

        /// <summary>
        /// Renders a visual to a drawing context.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="context">The drawing context.</param>
        public static void Render(Visual visual, DrawingContext context)
        {
            using (var renderer = new ImmediateRenderer(visual, updateTransformedBounds: false))
            {
                renderer.Render(context, visual, visual.Bounds);
            }
        }

        /// <inheritdoc/>
        public void AddDirty(Visual visual)
        {
            if (visual.Bounds != Rect.Empty)
            {
                var m = visual.TransformToVisual(_root);

                if (m.HasValue)
                {
                    var bounds = new Rect(visual.Bounds.Size).TransformToAABB(m.Value);

                    //use transformedbounds as previous render state of the visual bounds
                    //so we can invalidate old and new bounds of a control in case it moved/shrinked
                    if (visual.TransformedBounds.HasValue)
                    {
                        var trb = visual.TransformedBounds.Value;
                        var trBounds = trb.Bounds.TransformToAABB(trb.Transform);

                        if (trBounds != bounds)
                        {
                            _renderRoot?.Invalidate(trBounds);
                        }
                    }

                    _renderRoot?.Invalidate(bounds);
                }
            }
        }

        /// <summary>
        /// Ends the operation of the renderer.
        /// </summary>
        public void Dispose()
        {
            _renderTarget?.Dispose();
        }

        /// <inheritdoc/>
        public IEnumerable<Visual> HitTest(Point p, Visual root, Func<Visual, bool> filter)
        {
            return HitTest(root, p, filter);
        }

        public Visual? HitTestFirst(Point p, Visual root, Func<Visual, bool> filter)
        {
            return HitTest(root, p, filter).FirstOrDefault();
        }

        /// <inheritdoc/>
        public void RecalculateChildren(Visual visual) => AddDirty(visual);

        /// <inheritdoc/>
        public void Start()
        {
        }

        /// <inheritdoc/>
        public void Stop()
        {
        }

        /// <inheritdoc/>
        Size IVisualBrushRenderer.GetRenderTargetSize(IVisualBrush brush)
        {
            (brush.Visual as IVisualBrushInitialize)?.EnsureInitialized();
            return brush.Visual?.Bounds.Size ?? Size.Empty;
        }

        /// <inheritdoc/>
        void IVisualBrushRenderer.RenderVisualBrush(IDrawingContextImpl context, IVisualBrush brush)
        {
            var visual = brush.Visual;
            Render(new DrawingContext(context), visual, visual.Bounds);
        }

        internal static void Render(Visual visual, DrawingContext context, bool updateTransformedBounds)
        {
            using var renderer = new ImmediateRenderer(visual, updateTransformedBounds);
            renderer.Render(context, visual, visual.Bounds);
        }

        private static void ClearTransformedBounds(Visual visual)
        {
            foreach (var e in visual.GetSelfAndVisualDescendants())
            {
                visual.SetTransformedBounds(null);
            }
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

        private static IEnumerable<Visual> HitTest(
           Visual visual,
           Point p,
           Func<Visual, bool>? filter)
        {
            _ = visual ?? throw new ArgumentNullException(nameof(visual));

            if (filter?.Invoke(visual) != false)
            {
                bool containsPoint;

                if (visual is ICustomSimpleHitTest custom)
                {
                    containsPoint = custom.HitTest(p);
                }
                else
                {
                    containsPoint = visual.TransformedBounds?.Contains(p) == true;
                }

                if ((containsPoint || !visual.ClipToBounds) && visual.VisualChildren.Count > 0)
                {
                    foreach (var child in visual.VisualChildren.SortByZIndex())
                    {
                        foreach (var result in HitTest(child, p, filter))
                        {
                            yield return result;
                        }
                    }
                }

                if (containsPoint)
                {
                    yield return visual;
                }
            }
        }

        private void Render(DrawingContext context, Visual visual, Rect clipRect)
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
                    : default(DrawingContext.PushedState))
#pragma warning restore CS0618 // Type or member is obsolete

                using (visual.Clip != null ? context.PushGeometryClip(visual.Clip) : default(DrawingContext.PushedState))
                using (visual.OpacityMask != null ? context.PushOpacityMask(visual.OpacityMask, bounds) : default(DrawingContext.PushedState))
                using (context.PushTransformContainer())
                {
                    visual.Render(context);

#pragma warning disable 0618
                    var transformed =
                        new TransformedBounds(bounds, new Rect(), context.CurrentContainerTransform);
#pragma warning restore 0618

                    if (_updateTransformedBounds)
                        visual.SetTransformedBounds(transformed);

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
                        else if (_updateTransformedBounds)
                        {
                            ClearTransformedBounds(child);
                        }
                    }
                }
            }

            if (!visual.IsVisible && _updateTransformedBounds)
            {
                ClearTransformedBounds(visual);
            }
        }
    }
}
