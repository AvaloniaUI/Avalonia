// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Extension methods for rendering.
    /// </summary>
    /// <remarks>
    /// This class provides implements the platform-independent parts of <see cref="IRenderTarget"/>.
    /// </remarks>
    [SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery")]
    [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach")]
    public static class RendererMixin
    {
        private static int s_frameNum;
        private static int s_fps;
        private static int s_currentFrames;
        private static TimeSpan s_lastMeasure;
        private static readonly Stopwatch s_stopwatch = Stopwatch.StartNew();
        private static readonly Stack<List<IVisual>> s_listPool = new Stack<List<IVisual>>();
        private static readonly ZIndexComparer s_visualComparer = new ZIndexComparer();

        /// <summary>
        /// Gets or sets a value which determines whether an FPS counted will be drawn on each
        /// rendered frame.
        /// </summary>
        public static bool DrawFpsCounter { get; set; }

        /// <summary>
        /// Renders the specified visual.
        /// </summary>
        /// <param name="renderTarget">IRenderer instance</param>
        /// <param name="visual">The visual to render.</param>
        public static void Render(this IRenderTarget renderTarget, IVisual visual)
        {
            using (var ctx = renderTarget.CreateDrawingContext())
            {
                ctx.Render(visual);
                s_frameNum++;
                if (DrawFpsCounter)
                {
                    s_currentFrames++;
                    var now = s_stopwatch.Elapsed;
                    var elapsed = now - s_lastMeasure;
                    if (elapsed.TotalSeconds > 0)
                    {
                        s_fps = (int) (s_currentFrames/elapsed.TotalSeconds);
                        s_currentFrames = 0;
                        s_lastMeasure = now;
                    }
                    var pt = new Point(40, 40);
                    using (
                        var txt = new FormattedText("Frame #" + s_frameNum + " FPS: " + s_fps, "Arial", 18,
                            FontStyle.Normal,
                            TextAlignment.Left,
                            FontWeight.Normal,
                            TextWrapping.NoWrap))
                    {
                        ctx.FillRectangle(Brushes.White, new Rect(pt, txt.Measure()));
                        ctx.DrawText(Brushes.Black, pt, txt);
                    }
                }
            }
        }

        /// <summary>
        /// Renders the specified visual.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        /// <param name="context">The drawing context.</param>
        public static void Render(this DrawingContext context, IVisual visual)
        {
            context.Render(visual, visual.Bounds);
        }

        /// <summary>
        /// Renders the specified visual.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        /// <param name="context">The drawing context.</param>
        /// <param name="clipRect">
        /// The current clip rect, in coordinates relative to <paramref name="visual"/>.
        /// </param>
        private static void Render(this DrawingContext context, IVisual visual, Rect clipRect)
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
                    var transformed =
                        new TransformedBounds(bounds, new Rect(), context.CurrentContainerTransform);
                    if (visual is Visual)
                    {
                        BoundsTracker.SetTransformedBounds((Visual)visual, transformed);
                    }

                    var lst = GetSortedVisualList(visual.VisualChildren);

                    foreach (var child in lst)
                    {
                        var childBounds = GetTransformedBounds(child);

                        if (!child.ClipToBounds || clipRect.Intersects(childBounds))
                        {
                            var childClipRect = clipRect.Translate(-childBounds.Position);
                            context.Render(child, childClipRect);
                        }
                        else
                        {
                            ClearTransformedBounds(child);
                        }
                    }

                    ReturnListToPool(lst);
                }
            }
            
            if (!visual.IsVisible)
            {
                ClearTransformedBounds(visual);
            }
        }

        private static void ClearTransformedBounds(IVisual visual)
        {
            foreach (var e in visual.GetSelfAndVisualDescendents())
            {
                BoundsTracker.SetTransformedBounds((Visual)visual, null);
            }
        }

        private static void ReturnListToPool(List<IVisual> lst)
        {
            lst.Clear();
            s_listPool.Push(lst);
        }

        private static List<IVisual> GetSortedVisualList(IReadOnlyList<IVisual> source)
        {
            var lst = s_listPool.Count == 0 ? new List<IVisual>() : s_listPool.Pop();
            for (var c = 0; c < source.Count; c++)
                lst.Add(source[c]);
            lst.Sort(s_visualComparer);
            return lst;
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

        class ZIndexComparer : IComparer<IVisual>
        {
            public int Compare(IVisual x, IVisual y) => x.ZIndex.CompareTo(y.ZIndex);
        }
    }
}
