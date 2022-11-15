using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia.Collections.Pooled;
using Avalonia.VisualTree;

// Special license applies <see href="https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Base/Rendering/Composition/License.md">License.md</see>

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
        public PooledList<CompositionVisual>? TryHitTest(Point point, Func<CompositionVisual, bool>? filter)
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
            var m = visual.TryGetServerGlobalTransform();
            if (m == null)
            {
                matrix = default;
                return false;
            }

            var m33 = MatrixUtils.ToMatrix(m.Value);
            return m33.TryInvert(out matrix);
        }

        bool TryTransformTo(CompositionVisual visual, Point globalPoint, out Point v)
        {
            v = default;
            if (TryGetInvertedTransform(visual, out var m))
            {
                v = globalPoint * m;
                return true;
            }

            return false;
        }
        
        void HitTestCore(CompositionVisual visual, Point globalPoint, PooledList<CompositionVisual> result,
            Func<CompositionVisual, bool>? filter)
        {
            if (visual.Visible == false)
                return;
            
            if (filter != null && !filter(visual))
                return;
            
            if (!TryTransformTo(visual, globalPoint, out var point))
                return;

            if (visual.ClipToBounds
                && (point.X < 0 || point.Y < 0 || point.X > visual.Size.X || point.Y > visual.Size.Y))
                return;

            if (visual.Clip?.FillContains(point) == false)
                return;
            
            // Inspect children
            if (visual is CompositionContainerVisual cv)
                for (var c = cv.Children.Count - 1; c >= 0; c--)
                {
                    var ch = cv.Children[c];
                    HitTestCore(ch, globalPoint, result, filter);
                }
            
            // Hit-test the current node
            if (visual.HitTest(point)) 
                result.Add(visual);
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
            Compositor.Commit();
            Compositor.Server.Render();
        }
    }
}
