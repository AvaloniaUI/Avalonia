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
            using var candidates = QueryHitTestCandidates(point, out var globalPoint);
            root ??= Root;
            if (root == null)
                return null;
            var res = new PooledList<CompositionVisual>();

            if (candidates == null)
                return res;

            foreach (var candidate in candidates)
            {
                if (HitTestCandidate(root, candidate.Visual, globalPoint, filter))
                    res.Add(candidate.Visual);
            }

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

        static bool TryGetInvertedTransform(CompositionVisual visual, out Matrix matrix)
        {
            var m = visual.TryGetServerGlobalTransform();
            if (m == null)
            {
                matrix = default;
                return false;
            }

            var m33 = m.Value;
            return m33.TryInvert(out matrix);
        }

        static bool TryTransformTo(CompositionVisual visual, Point globalPoint, out Point v)
        {
            v = default;
            if (TryGetInvertedTransform(visual, out var m))
            {
                v = globalPoint * m;
                return true;
            }

            return false;
        }
        
        PooledList<CompositionHitTestCandidate>? QueryHitTestCandidates(Point point, out Point globalPoint)
        {
            globalPoint = point * Scaling;
            Server.Readback.NextRead();

            if (Root == null)
                return null;

            if (_hitTestIndexDirty || !_hitTestIndex.IsCurrent(Root, Server.Readback.ReadRevision))
            {
                _hitTestIndex.Rebuild(Root, Server.Readback.ReadRevision);
                _hitTestIndexDirty = false;
            }

            var candidates = new PooledList<CompositionHitTestCandidate>();
            _hitTestIndex.Query(globalPoint, candidates);
            candidates.Sort(static (left, right) => left.Order.CompareTo(right.Order));
            return candidates;
        }

        static bool HitTestCandidate(CompositionVisual root, CompositionVisual visual, Point globalPoint,
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

            Point point = default;
            for (var c = path.Count - 1; c >= 0; c--)
            {
                if (!HitTestVisual(path[c], globalPoint, filter, out point))
                    return false;
            }

            return visual.HitTest(point);
        }

        static bool HitTestVisual(CompositionVisual visual, Point globalPoint, Func<CompositionVisual, bool>? filter,
            out Point point)
        {
            point = default;

            if (visual.Visible == false)
                return false;

            if (filter != null && !filter(visual))
                return false;

            if (!TryTransformTo(visual, globalPoint, out point))
                return false;

            if (visual.ClipToBounds
                && (point.X < 0 || point.Y < 0 || point.X > visual.Size.X || point.Y > visual.Size.Y))
                return false;

            if (visual.Clip?.FillContains(point) == false)
                return false;

            return true;
        }

        public CompositionVisual? TryHitTestFirst(Point point, CompositionVisual? root, Func<CompositionVisual, bool>? filter)
        {
            using var candidates = QueryHitTestCandidates(point, out var globalPoint);
            root ??= Root;
            if (root == null || candidates == null)
                return null;

            foreach (var candidate in candidates)
            {
                if (HitTestCandidate(root, candidate.Visual, globalPoint, filter))
                {
                    return candidate.Visual;
                }
            }

            return null;
        }

        /// <summary>
        /// Registers the composition target for explicit redraw
        /// </summary>
        public void RequestRedraw() => RegisterForSerialization();
    }
}
