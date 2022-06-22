using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia.Collections.Pooled;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.Composition
{
    /// <summary>
    /// Represents the composition output (e. g. a window, embedded control, entire screen)
    /// </summary>
    public partial class CompositionTarget
    {
        partial void OnRootChanged()
        {
            if (Root != null)
                Root.Root = this;
        }

        partial void OnRootChanging()
        {
            if (Root != null)
                Root.Root = null;
        }
        
        /// <summary>
        /// Attempts to perform a hit-tst
        /// </summary>
        /// <param name="point"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public PooledList<CompositionVisual>? TryHitTest(Point point, Func<IVisual, bool>? filter)
        {
            Server.Readback.NextRead();
            if (Root == null)
                return null;
            var res = new PooledList<CompositionVisual>();
            HitTestCore(Root, point, res, filter);
            return res;
        }

        /// <summary>
        /// Attempts to transform a point to a particular CompositionVisual coordinate space
        /// </summary>
        /// <returns></returns>
        public Point? TryTransformToVisual(CompositionVisual visual, Point point)
        {
            if (visual.Root != this)
                return null;
            var v = visual;
            var m = Matrix.Identity;
            while (v != null)
            {
                if (!TryGetInvertedTransform(v, out var cm))
                    return null;
                m = m * cm;
                v = v.Parent;
            }

            return point * m;
        }

        bool TryGetInvertedTransform(CompositionVisual visual, out Matrix matrix)
        {
            var m = visual.TryGetServerTransform();
            if (m == null)
            {
                matrix = default;
                return false;
            }

            var m33 = MatrixUtils.ToMatrix(m.Value);
            return m33.TryInvert(out matrix);
        }

        bool TryTransformTo(CompositionVisual visual, ref Point v)
        {
            if (TryGetInvertedTransform(visual, out var m))
            {
                v = v * m;
                return true;
            }

            return false;
        }
        
        bool HitTestCore(CompositionVisual visual, Point point, PooledList<CompositionVisual> result,
            Func<IVisual, bool>? filter)
        {
            //TODO: Check readback too
            if (visual.Visible == false)
                return false;
            if (!TryTransformTo(visual, ref point))
                return false;

            if (visual.ClipToBounds
                && (point.X < 0 || point.Y < 0 || point.X > visual.Size.X || point.Y > visual.Size.Y))
                return false;
            if (visual.Clip?.FillContains(point) == false)
                return false;

            bool success = false;
            // Hit-test the current node
            if (visual.HitTest(point, filter))
            {
                result.Add(visual);
                success = true;
            }

            // Inspect children too
            if (visual is CompositionContainerVisual cv)
                for (var c = cv.Children.Count - 1; c >= 0; c--)
                {
                    var ch = cv.Children[c];
                    var hit = HitTestCore(ch, point, result, filter);
                    if (hit)
                        return true;
                }

            return success;

        }

        /// <summary>
        /// Registers the composition target for explicit redraw
        /// </summary>
        public void RequestRedraw() => RegisterForSerialization();

        /// <summary>
        /// Performs composition directly on the UI thread 
        /// </summary>
        internal void ImmediateUIThreadRender()
        {
            Compositor.RequestCommitAsync();
            Compositor.Server.Render();
        }
    }
}