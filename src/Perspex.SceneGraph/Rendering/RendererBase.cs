// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Perspex.Media;
using Perspex.Platform;

namespace Perspex.Rendering
{
    /// <summary>
    /// Base class for standard renderers.
    /// </summary>
    /// <remarks>
    /// This class provides implements the platform-independent parts of <see cref="IRenderTarget"/>.
    /// </remarks>
    [SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery")]
    [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach")]
    public static class RendererMixin
    {
        static int s_frameNum;
        static int s_fps;
        static int s_currentFrames;
        static TimeSpan s_lastMeasure;
        static readonly Stopwatch s_stopwatch = Stopwatch.StartNew();
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
                            FontWeight.Normal))
                    {
                        ctx.FillRectangle(Brushes.White, new Rect(pt, txt.Measure()));
                        ctx.DrawText(Brushes.Black, pt, txt);
                    }
                }
            }
        }

        public static bool DrawFpsCounter { get; set; }

        /// <summary>
        /// Renders the specified visual.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        /// 
        /// <param name="context">The drawing context.</param>
        public static void Render(this DrawingContext context, IVisual visual)
        {
            var opacity = visual.Opacity;
            if (visual.IsVisible && opacity > 0)
            {
                var m = Matrix.CreateTranslation(visual.Bounds.Position);

                var renderTransform = Matrix.Identity;

                if (visual.RenderTransform != null)
                {
                    var origin = visual.TransformOrigin.ToPixels(new Size(visual.Bounds.Width, visual.Bounds.Height));
                    var offset = Matrix.CreateTranslation(origin);
                    renderTransform = (-offset)*visual.RenderTransform.Value*(offset);
                }
                m = renderTransform*m;

                using (context.PushPostTransform(m))
                using (context.PushOpacity(opacity))
                using (visual.ClipToBounds ? context.PushClip(new Rect(visual.Bounds.Size)) : default(DrawingContext.PushedState))
                using (context.PushTransformContainer())
                {
                    visual.Render(context);
                    var lst = GetSortedVisualList(visual.VisualChildren);
                    foreach (var child in lst)
                    {
                        context.Render(child);
                    }
                    ReturnListToPool(lst);
                }
            }
        }

        static readonly Stack<List<IVisual>> ListPool = new Stack<List<IVisual>>();
        static readonly ZIndexComparer VisualComparer = new ZIndexComparer();
        class ZIndexComparer : IComparer<IVisual>
        {
            public int Compare(IVisual x, IVisual y) => x.ZIndex.CompareTo(y.ZIndex);
        }

        static void ReturnListToPool(List<IVisual> lst)
        {
            lst.Clear();
            ListPool.Push(lst);
        }

        static List<IVisual> GetSortedVisualList(IReadOnlyList<IVisual> source)
        {
            var lst = ListPool.Count == 0 ? new List<IVisual>() : ListPool.Pop();
            for (var c = 0; c < source.Count; c++)
                lst.Add(source[c]);
            lst.Sort(VisualComparer);
            return lst;
        }



    }
}
