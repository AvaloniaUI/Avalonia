using System;
using System.Numerics;
using Avalonia.Collections.Pooled;

namespace Avalonia.Rendering.Composition
{
    /// <summary>
    /// Represents the composition output (e. g. a window, embedded control, entire screen)
    /// </summary>
    internal partial class CompositionTarget
    {
        private readonly CompositionHitTestRTree _hitTestIndex = new();
        private bool _hitTestIndexDirty = true;

        partial void OnRootChanged()
        {
            if (Root != null)
                Root.Root = this;
            InvalidateHitTestIndex();
        }

        partial void OnRootChanging()
        {
            if (Root != null)
                Root.Root = null;
            InvalidateHitTestIndex();
        }

        internal void InvalidateHitTestIndex() => _hitTestIndexDirty = true;
        
        /// <summary>
        /// Attempts to perform a hit-tst
        /// </summary>
        /// <returns></returns>
        public PooledList<CompositionVisual>? TryHitTest(Point point, CompositionVisual? root, Func<CompositionVisual, bool>? filter)
        {
            root ??= Root;
            if (root == null)
                return null;

            using var candidates = QueryHitTestCandidates(root, point, out var rootParentPoint);
            if (candidates == null)
                return null;

            var res = new PooledList<CompositionVisual>();

            foreach (var candidate in candidates)
            {
                if (HitTestCandidate(root, candidate.Visual, rootParentPoint, filter))
                    res.Add(candidate.Visual);
            }

            return res;
        }

        PooledList<CompositionHitTestCandidate>? QueryHitTestCandidates(
            CompositionVisual root,
            Point point,
            out Point rootParentPoint)
        {
            rootParentPoint = default;

            Server.Compositor.Readback.NextRead();

            if (Root == null || root.Root != this)
                return null;

            var readRevision = Server.Compositor.Readback.ReadRevision;
            if (_hitTestIndexDirty || !_hitTestIndex.IsCurrent(Root, readRevision))
            {
                _hitTestIndex.Rebuild(Root, readRevision);
                _hitTestIndexDirty = false;
            }

            if (!TryGetOwnTransform(root, out var rootTransform) ||
                !TryGetGlobalTransform(root, out var globalTransform))
                return null;

            rootParentPoint = point.Transform(rootTransform);
            var indexPoint = point.Transform(globalTransform);

            var candidates = new PooledList<CompositionHitTestCandidate>();
            _hitTestIndex.Query(indexPoint, candidates);
            candidates.Sort(static (left, right) => left.Order.CompareTo(right.Order));
            return candidates;
        }

        static bool HitTestCandidate(CompositionVisual root, CompositionVisual visual, Point rootParentPoint,
            Func<CompositionVisual, bool>? filter)
        {
            using var path = new PooledList<CompositionVisual>();

            for (var current = visual; current != null; current = current.Parent)
            {
                path.Add(current);
                if (ReferenceEquals(current, root))
                    break;
            }

            if (path.Count == 0 || !ReferenceEquals(path[path.Count - 1], root))
                return false;

            var parentPoint = rootParentPoint;
            Point point = default;
            for (var c = path.Count - 1; c >= 0; c--)
            {
                if (!HitTestVisual(path[c], parentPoint, filter, out point))
                    return false;

                parentPoint = point;
            }

            return visual.HitTest(point);
        }

        static bool HitTestVisual(CompositionVisual visual, Point parentPoint, Func<CompositionVisual, bool>? filter,
            out Point point)
        {
            point = default;

            if (visual.Visible == false)
                return false;

            if (filter != null && !filter(visual))
                return false;

            var readback = visual.TryGetValidReadback();
            if (readback == null)
                return false;

            if (!visual.DisableSubTreeBoundsHitTestOptimization &&
                (readback.TransformedSubtreeBounds == null ||
                 !readback.TransformedSubtreeBounds.Value.Contains(parentPoint)))
                return false;

            if (!readback.Matrix.TryInvert(out var invMatrix))
                return false;

            point = parentPoint.Transform(invMatrix);

            if (visual.ClipToBounds
                && (point.X < 0 || point.Y < 0 || point.X > visual.Size.X || point.Y > visual.Size.Y))
                return false;

            if (visual.Clip?.FillContains(point) == false)
                return false;

            return true;
        }

        static bool TryGetOwnTransform(CompositionVisual visual, out Matrix transform)
        {
            if (visual.TryGetValidReadback() is { } readback)
            {
                transform = readback.Matrix;
                return true;
            }

            transform = default;
            return false;
        }

        static bool TryGetGlobalTransform(CompositionVisual visual, out Matrix transform)
        {
            transform = Matrix.Identity;

            for (var current = visual; current != null; current = current.Parent)
            {
                if (!TryGetOwnTransform(current, out var ownTransform))
                {
                    transform = default;
                    return false;
                }

                transform *= ownTransform;
            }

            return true;
        }

        public CompositionVisual? TryHitTestFirst(Point point, CompositionVisual? root, Func<CompositionVisual, bool>? filter)
        {
            root ??= Root;
            if (root == null)
                return null;

            using var candidates = QueryHitTestCandidates(root, point, out var rootParentPoint);
            if (candidates == null)
                return null;

            foreach (var candidate in candidates)
            {
                if (HitTestCandidate(root, candidate.Visual, rootParentPoint, filter))
                    return candidate.Visual;
            }

            return null;
        }

        /// <summary>
        /// Registers the composition target for explicit redraw
        /// </summary>
        public void RequestRedraw() => RegisterForSerialization();
    }
}
