// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;
using Avalonia.VisualTree;
using System.Collections.Generic;
using Avalonia.Threading;
using Avalonia.Media;
using System.Linq;

namespace Avalonia.Rendering
{
    public class ImmediateRenderer : IDisposable, IRenderer
    {
        private readonly IRenderLoop _renderLoop;
        private readonly IVisual _root;
        private IRenderTarget _renderTarget;
        private bool _dirty;
        private bool _renderQueued;

        public ImmediateRenderer(IRenderRoot root, IRenderLoop renderLoop)
        {
            Contract.Requires<ArgumentNullException>(root != null);
            Contract.Requires<ArgumentNullException>(renderLoop != null);

            _root = root;
            _renderLoop = renderLoop;
            _renderLoop.Tick += OnRenderLoopTick;
        }

        private ImmediateRenderer(IVisual root)
        {
            Contract.Requires<ArgumentNullException>(root != null);

            _root = root;
        }

        public bool DrawFps { get; set; }
        public bool DrawDirtyRects { get; set; }

        public static void Render(IVisual visual, IRenderTarget target)
        {
            using (var renderer = new ImmediateRenderer(visual))
            using (var context = new DrawingContext(target.CreateDrawingContext()))
            {
                renderer.Render(context, visual, visual.Bounds);
            }
        }

        public static void Render(IVisual visual, DrawingContext context)
        {
            using (var renderer = new ImmediateRenderer(visual))
            {
                renderer.Render(context, visual, visual.Bounds);
            }
        }

        public void AddDirty(IVisual visual)
        {
            _dirty = true;
        }

        public void Dispose()
        {
            if (_renderLoop != null)
            {
                _renderLoop.Tick -= OnRenderLoopTick;
            }
        }

        public IEnumerable<IVisual> HitTest(Point p, Func<IVisual, bool> filter)
        {
            return HitTest(_root, p, filter);
        }

        public void Render(Rect rect)
        {
            if (_renderTarget == null)
            {
                _renderTarget = ((IRenderRoot)_root).CreateRenderTarget();
            }

            try
            {
                Render(_root);
            }
            catch (RenderTargetCorruptedException ex)
            {
                Logging.Logger.Information("Renderer", this, "Render target was corrupted. Exception: {0}", ex);
                _renderTarget.Dispose();
                _renderTarget = null;
            }
            finally
            {
                _dirty = false;
                _renderQueued = false;
            }
        }

        private static void ClearTransformedBounds(IVisual visual)
        {
            foreach (var e in visual.GetSelfAndVisualDescendents())
            {
                BoundsTracker.SetTransformedBounds((Visual)visual, null);
            }
        }

        private static Rect GetTransformedBounds(IVisual visual)
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

        static IEnumerable<IVisual> HitTest(
           IVisual visual,
           Point p,
           Func<IVisual, bool> filter)
        {
            Contract.Requires<ArgumentNullException>(visual != null);

            if (filter?.Invoke(visual) != false)
            {
                bool containsPoint = BoundsTracker.GetTransformedBounds((Visual)visual)?.Contains(p) == true;

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

        private void Render(IVisual visual)
        {
            using (var context = new DrawingContext(_renderTarget.CreateDrawingContext()))
            {
                Render(context, visual, visual.Bounds);
            }
        }

        private void Render(DrawingContext context, IVisual visual, Rect clipRect)
        {
            var opacity = visual.Opacity;
            var clipToBounds = visual.ClipToBounds;
            var bounds = new Rect(visual.Bounds.Size);

            if (visual.IsVisible && opacity > 0)
            {
                var m = Matrix.CreateTranslation(visual.Bounds.Position);

                var renderTransform = Matrix.Identity;

                if (visual.RenderTransform != null)
                {
                    var origin = visual.RenderTransformOrigin.ToPixels(new Size(visual.Bounds.Width, visual.Bounds.Height));
                    var offset = Matrix.CreateTranslation(origin);
                    renderTransform = (-offset) * visual.RenderTransform.Value * (offset);
                }

                m = renderTransform * m;

                if (clipToBounds)
                {
                    clipRect = clipRect.Intersect(new Rect(visual.Bounds.Size));
                }

                using (context.PushPostTransform(m))
                using (context.PushOpacity(opacity))
                using (clipToBounds ? context.PushClip(bounds) : default(DrawingContext.PushedState))
                using (visual.Clip != null ? context.PushGeometryClip(visual.Clip) : default(DrawingContext.PushedState))
                using (visual.OpacityMask != null ? context.PushOpacityMask(visual.OpacityMask, bounds) : default(DrawingContext.PushedState))
                using (context.PushTransformContainer())
                {
                    visual.Render(context);

#pragma warning disable 0618
                    var transformed =
                        new TransformedBounds(bounds, new Rect(), context.CurrentContainerTransform);
#pragma warning restore 0618

                    if (visual is Visual)
                    {
                        BoundsTracker.SetTransformedBounds((Visual)visual, transformed);
                    }

                    foreach (var child in visual.VisualChildren.OrderBy(x => x, ZIndexComparer.Instance))
                    {
                        var childBounds = GetTransformedBounds(child);

                        if (!child.ClipToBounds || clipRect.Intersects(childBounds))
                        {
                            var childClipRect = clipRect.Translate(-childBounds.Position);
                            Render(context, child, childClipRect);
                        }
                        else
                        {
                            ClearTransformedBounds(child);
                        }
                    }
                }
            }

            if (!visual.IsVisible)
            {
                ClearTransformedBounds(visual);
            }
        }

        private void OnRenderLoopTick(object sender, EventArgs e)
        {
            if (_dirty && !_renderQueued)
            {
                _renderQueued = true;
                Dispatcher.UIThread.InvokeAsync(() => Render(new Rect(((IRenderRoot)_root).ClientSize)));
            }
        }
    }
}
