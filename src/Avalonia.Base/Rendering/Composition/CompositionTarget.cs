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
    internal partial class CompositionTarget
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
        /// <returns></returns>
        public PooledList<CompositionVisual>? TryHitTest(Point point, CompositionVisual? root, Func<CompositionVisual, bool>? filter)
        {
            Server.Compositor.Readback.NextRead();
            root ??= Root;
            if (root == null)
                return null;
            
            var res = new PooledList<CompositionVisual>();
            
            // Need to convert transform the point using visual's readback since HitTestCore will use its inverse matrix
            // NOTE: it can technically break hit-testing of the root visual itself if it has a non-identity transform,
            // need to investigate that possibility later. We might want a separate mode for root hit-testing.
            var readback = root.TryGetValidReadback();
            if (readback == null)
                return null;
            point = point.Transform(readback.Matrix);
            
            HitTestCore(root, point, res, filter);
            return res;
        }
        
        void HitTestCore(CompositionVisual visual, Point parentPoint, PooledList<CompositionVisual> result,
            Func<CompositionVisual, bool>? filter)
        {
            if (visual.Visible == false)
                return;
            
            if (filter != null && !filter(visual))
                return;

            var readback = visual.TryGetValidReadback();
            if(readback == null)
                return;


            if (!visual.DisableSubTreeBoundsHitTestOptimization &&
                (readback.TransformedSubtreeBounds == null ||
                 !readback.TransformedSubtreeBounds.Value.Contains(parentPoint)))
                return;
            
            if(!readback.Matrix.TryInvert(out var invMatrix))
                return;

            var point = parentPoint.Transform(invMatrix);
            
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
                    HitTestCore(ch, point, result, filter);
                }
            
            // Hit-test the current node
            if (visual.HitTest(point)) 
                result.Add(visual);
        }

        /// <summary>
        /// Registers the composition target for explicit redraw
        /// </summary>
        public void RequestRedraw() => RegisterForSerialization();
    }
}
