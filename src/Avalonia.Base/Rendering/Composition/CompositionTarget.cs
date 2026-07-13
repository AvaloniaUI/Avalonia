using System;
using Avalonia.Collections.Pooled;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition
{
    /// <summary>
    /// Represents the composition output (e. g. a window, embedded control, entire screen)
    /// </summary>
    internal partial class CompositionTarget
    {
        private readonly PooledList<CompositionVisual> _hitTestChildCandidates = [];
        private bool _hitTestChildCandidatesInUse;

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

            // Need to convert transform the point using visual's readback since HitTestCore will use its inverse matrix
            // NOTE: it can technically break hit-testing of the root visual itself if it has a non-identity transform,
            // need to investigate that possibility later. We might want a separate mode for root hit-testing.
            var readback = root.TryGetValidReadback();
            if (readback == null)
                return null;
            point = point.Transform(readback.Matrix);

            var res = new PooledList<CompositionVisual>();
            HitTestCore(root, point, res, filter);
            return res;
        }

        internal PooledList<CompositionVisual>? TryHitTest(Geometry geometry, CompositionVisual? root, Func<CompositionVisual, bool>? filter)
        {
            Server.Compositor.Readback.NextRead();

            root ??= Root;
            if (root == null)
                return null;

            // Need to convert transform the point using visual's readback since HitTestCore will use its inverse matrix
            // NOTE: it can technically break hit-testing of the root visual itself if it has a non-identity transform,
            // need to investigate that possibility later. We might want a separate mode for root hit-testing.
            var readback = root.TryGetValidReadback();
            if (readback == null)
                return null;

            geometry = geometry.Clone();
            geometry.Transform = new MatrixTransform((geometry.Transform?.Value ?? Matrix.Identity) * readback.Matrix);

            var res = new PooledList<CompositionVisual>();
            HitTestCore(root, geometry, res, filter);
            return res;
        }

        private PooledList<CompositionVisual> RentHitTestChildCandidates(out bool releaseToField)
        {
            if (!_hitTestChildCandidatesInUse)
            {
                _hitTestChildCandidatesInUse = true;
                releaseToField = true;
                _hitTestChildCandidates.Clear();
                return _hitTestChildCandidates;
            }

            releaseToField = false;
            return [];
        }

        private void ReleaseHitTestChildCandidates(PooledList<CompositionVisual> candidates, bool releaseToField)
        {
            if (releaseToField)
            {
                candidates.Clear();
                _hitTestChildCandidatesInUse = false;
            }
            else
            {
                candidates.Dispose();
            }
        }

        private void HitTestCore(CompositionVisual visual, Point parentPoint, PooledList<CompositionVisual> result, Func<CompositionVisual, bool>? filter)
        {
            if (!HitTestVisual(visual, parentPoint, filter, out var point))
                return;

            // Inspect children
            if (visual is CompositionContainerVisual cv)
                HitTestChildren(cv, point, result, filter);
            
            // Hit-test the current node
            if (visual.HitTest(point))
                result.Add(visual);
        }

        private void HitTestCore(CompositionVisual visual, Geometry parentGeometry, PooledList<CompositionVisual> result, Func<CompositionVisual, bool>? filter)
        {
            if (!HitTestVisual(visual, parentGeometry, filter, out var geometry) || geometry == null)
                return;

            // Inspect children
            if (visual is CompositionContainerVisual cv)
                HitTestChildren(cv, geometry, result, filter);

            // Hit-test the current node
            if (visual.HitTest(geometry) > IntersectionDetail.Empty)
                result.Add(visual);
        }

        private void HitTestChildren(CompositionContainerVisual visual, Point point, PooledList<CompositionVisual> result, Func<CompositionVisual, bool>? filter)
        {
            if (visual.Children.Count >= CompositionContainerVisual.HitTestAabbTreeThreshold)
            {
                var candidates = RentHitTestChildCandidates(out var releaseToField);
                try
                {
                    if (visual.TryQueryHitTestChildren(point, candidates))
                    {
                        foreach (var child in candidates)
                            HitTestCore(child, point, result, filter);
                        return;
                    }
                }
                finally
                {
                    ReleaseHitTestChildCandidates(candidates, releaseToField);
                }
            }

            for (var c = visual.Children.Count - 1; c >= 0; c--)
                HitTestCore(visual.Children[c], point, result, filter);
        }

        private void HitTestChildren(CompositionContainerVisual visual, Geometry? geometry, PooledList<CompositionVisual> result, Func<CompositionVisual, bool>? filter)
        {
            if (geometry == null)
                return;

            if (visual.Children.Count >= CompositionContainerVisual.HitTestAabbTreeThreshold)
            {
                var candidates = RentHitTestChildCandidates(out var releaseToField);
                try
                {
                    if (visual.TryQueryHitTestChildren(geometry, candidates))
                    {
                        foreach (var child in candidates)
                            HitTestCore(child, geometry, result, filter);
                        return;
                    }
                }
                finally
                {
                    ReleaseHitTestChildCandidates(candidates, releaseToField);
                }
            }

            for (var c = visual.Children.Count - 1; c >= 0; c--)
                HitTestCore(visual.Children[c], geometry, result, filter);
        }

        private static bool HitTestVisual(CompositionVisual visual, Point parentPoint, Func<CompositionVisual, bool>? filter, out Point point)
        {
            point = default;

            if (visual.Visible == false)
                return false;

            if (filter != null && !filter(visual))
                return false;

            var readback = visual.TryGetValidReadback();
            if(readback == null)
                return false;

            if (!visual.DisableSubTreeBoundsHitTestOptimization &&
                (readback.TransformedSubtreeBounds == null ||
                 !readback.TransformedSubtreeBounds.Value.Contains(parentPoint)))
                return false;

            if(!readback.Matrix.TryInvert(out var invMatrix))
                return false;

            point = parentPoint.Transform(invMatrix);

            if (visual.ClipToBounds
                && (point.X < 0 || point.Y < 0 || point.X > visual.Size.X || point.Y > visual.Size.Y))
                return false;

            if (visual.Clip?.FillContains(point) == false)
                return false;

            return true;
        }

        private static bool HitTestVisual(CompositionVisual visual, Geometry parentGeometry, Func<CompositionVisual, bool>? filter, out Geometry? geometry)
        {
            geometry = null;

            if (visual.Visible == false)
                return false;

            if (filter != null && !filter(visual))
                return false;

            var readback = visual.TryGetValidReadback();
            if (readback == null)
                return false;

            var pen = new ImmutablePen(Colors.Black.ToUInt32(), 0);
            var transformedBounds = parentGeometry.GetRenderBounds(pen);
            var parentGeometyBounds = new LtrbRect(transformedBounds);

            if (!visual.DisableSubTreeBoundsHitTestOptimization &&
                (readback.TransformedSubtreeBounds == null ||
                 !readback.TransformedSubtreeBounds.Value.Overlaps(parentGeometyBounds)))
                return false;

            if (!readback.Matrix.TryInvert(out var invMatrix))
                return false;

            geometry = parentGeometry.Clone();
            geometry.Transform = new MatrixTransform((parentGeometry.Transform?.Value ?? Matrix.Identity) * invMatrix);

            var bounds = geometry.Bounds;
            if (visual.ClipToBounds
                && (bounds.IsEmpty() || 
                    bounds.Width <= 0 ||
                    bounds.Height <= 0))
                return false;

            if (geometry.PlatformImpl == null || visual.Clip?.FillContains(geometry.PlatformImpl) <= IntersectionDetail.Empty)
                return false;

            return true;
        }

        public CompositionVisual? TryHitTestFirst(Point point, CompositionVisual? root, Func<CompositionVisual, bool>? filter, Func<CompositionVisual, bool>? resultFilter)
        {
            Server.Compositor.Readback.NextRead();

            root ??= Root;
            if (root == null)
                return null;

            // Need to convert transform the point using visual's readback since HitTestCore will use its inverse matrix
            // NOTE: it can technically break hit-testing of the root visual itself if it has a non-identity transform,
            // need to investigate that possibility later. We might want a separate mode for root hit-testing.
            var readback = root.TryGetValidReadback();
            if (readback == null)
                return null;

            return HitTestFirstCore(root, point.Transform(readback.Matrix), filter, resultFilter);
        }

        public CompositionVisual? TryHitTestFirst(Geometry geometry, CompositionVisual? root, Func<CompositionVisual, bool>? filter, Func<CompositionVisual, bool>? resultFilter)
        {
            Server.Compositor.Readback.NextRead();

            root ??= Root;
            if (root == null)
                return null;

            // Need to convert transform the point using visual's readback since HitTestCore will use its inverse matrix
            // NOTE: it can technically break hit-testing of the root visual itself if it has a non-identity transform,
            // need to investigate that possibility later. We might want a separate mode for root hit-testing.
            var readback = root.TryGetValidReadback();
            if (readback == null)
                return null;
            geometry = geometry.Clone();
            geometry.Transform = new MatrixTransform(geometry.Transform?.Value ?? Matrix.Identity * readback.Matrix);
            return HitTestFirstCore(root, geometry, filter, resultFilter);
        }

        internal CompositionVisual? HitTestFirstCore(CompositionVisual visual, Point parentPoint, Func<CompositionVisual, bool>? filter, Func<CompositionVisual, bool>? resultFilter)
        {
            if (!HitTestVisual(visual, parentPoint, filter, out var point))
                return null;

            if (visual is CompositionContainerVisual cv)
            {
                var queriedIndexedChildren = false;
                if (cv.Children.Count >= CompositionContainerVisual.HitTestAabbTreeThreshold)
                {
                    if (cv.TryQueryFirstHitTestChild(this, point, filter, resultFilter, out var hit))
                    {
                        queriedIndexedChildren = true;
                        if (hit != null)
                            return hit;
                    }
                }

                if (!queriedIndexedChildren)
                {
                    for (var c = cv.Children.Count - 1; c >= 0; c--)
                    {
                        var hit = HitTestFirstCore(cv.Children[c], point, filter, resultFilter);
                        if (hit != null)
                            return hit;
                    }
                }
            }

            return visual.HitTest(point) && (resultFilter == null || resultFilter(visual)) ? visual : null;
        }

        internal CompositionVisual? HitTestFirstCore(CompositionVisual visual, Geometry parentGeometry, Func<CompositionVisual, bool>? filter, Func<CompositionVisual, bool>? resultFilter)
        {
            if (!HitTestVisual(visual, parentGeometry, filter, out var geometry) || geometry == null)
                return null;

            if (visual is CompositionContainerVisual cv)
            {
                var queriedIndexedChildren = false;
                if (cv.Children.Count >= CompositionContainerVisual.HitTestAabbTreeThreshold)
                {
                    if (cv.TryQueryFirstHitTestChild(this, parentGeometry, filter, resultFilter, out var hit))
                    {
                        queriedIndexedChildren = true;
                        if (hit != null)
                            return hit;
                    }
                }

                if (!queriedIndexedChildren)
                {
                    for (var c = cv.Children.Count - 1; c >= 0; c--)
                    {
                        var hit = HitTestFirstCore(cv.Children[c], geometry, filter, resultFilter);
                        if (hit != null)
                            return hit;
                    }
                }
            }

            return visual.HitTest(geometry) > IntersectionDetail.Empty && (resultFilter == null || resultFilter(visual)) ? visual : null;
        }

        /// <summary>
        /// Registers the composition target for explicit redraw
        /// </summary>
        public void RequestRedraw() => RegisterForSerialization();
    }
}
