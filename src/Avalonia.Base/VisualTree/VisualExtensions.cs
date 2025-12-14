using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Rendering;
using Avalonia.Utilities;

namespace Avalonia.VisualTree
{
    /// <summary>
    /// Provides extension methods for working with the visual tree.
    /// </summary>
    public static class VisualExtensions
    {
        /// <summary>
        /// Calculates the distance from a visual's ancestor.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="ancestor">The ancestor visual.</param>
        /// <returns>
        /// The number of steps from the visual to the ancestor or -1 if
        /// <paramref name="visual"/> is not a descendent of <paramref name="ancestor"/>.
        /// </returns>
        public static int CalculateDistanceFromAncestor(this Visual visual, Visual? ancestor)
        {
            Visual? v = visual ?? throw new ArgumentNullException(nameof(visual));
            var result = 0;

            while (v != null && v != ancestor)
            {
                v = v.VisualParent;

                result++;
            }

            return v != null ? result : -1;
        }

        /// <summary>
        /// Calculates the distance from a visual's root.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>
        /// The number of steps from the visual to the root.
        /// </returns>
        public static int CalculateDistanceFromRoot(Visual visual)
        {
            _ = visual ?? throw new ArgumentNullException(nameof(visual));
            // Use the cached visual level for O(1) lookup
            return visual.VisualLevel;
        }

        /// <summary>
        /// Tries to get the first common ancestor of two visuals.
        /// </summary>
        /// <param name="visual">The first visual.</param>
        /// <param name="target">The second visual.</param>
        /// <returns>The common ancestor, or null if not found.</returns>
        public static Visual? FindCommonVisualAncestor(this Visual? visual, Visual? target)
        {
            if (visual is null || target is null)
            {
                return null;
            }

            Visual? v = visual;
            Visual? t = target;

            // Use cached visual levels for O(1) depth lookup instead of O(d) traversal
            var firstLevel = v.VisualLevel;
            var secondLevel = t.VisualLevel;

            // Move the deeper node up to match levels
            while (firstLevel > secondLevel)
            {
                v = v!.VisualParent;
                firstLevel--;
            }

            while (secondLevel > firstLevel)
            {
                t = t!.VisualParent;
                secondLevel--;
            }

            // Now both are at the same level, walk up together until we find common ancestor
            while (v != t)
            {
                v = v?.VisualParent;
                t = t?.VisualParent;
            }

            return v;
        }

        /// <summary>
        /// Enumerates the ancestors of an <see cref="Visual"/> in the visual tree.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The visual's ancestors.</returns>
        public static IEnumerable<Visual> GetVisualAncestors(this Visual visual)
        {
            ThrowHelper.ThrowIfNull(visual, nameof(visual));

            var v = visual.VisualParent;

            while (v != null)
            {
                yield return v;
                v = v.VisualParent;
            }
        }

        /// <summary>
        /// Returns a struct-based enumerable for ancestors that doesn't allocate.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>A struct enumerable for the visual's ancestors.</returns>
        public static VisualAncestorsEnumerable EnumerateAncestors(this Visual visual)
        {
            return new VisualAncestorsEnumerable(visual);
        }

        /// <summary>
        /// Returns a struct-based enumerable for self and ancestors that doesn't allocate.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>A struct enumerable for the visual and its ancestors.</returns>
        public static SelfAndAncestorsEnumerable EnumerateSelfAndAncestors(this Visual visual)
        {
            return new SelfAndAncestorsEnumerable(visual);
        }

        /// <summary>
        /// Finds first ancestor of given type.
        /// </summary>
        /// <typeparam name="T">Ancestor type.</typeparam>
        /// <param name="visual">The visual.</param>
        /// <param name="includeSelf">If given visual should be included in search.</param>
        /// <returns>First ancestor of given type.</returns>
        public static T? FindAncestorOfType<T>(this Visual? visual, bool includeSelf = false) where T : class
        {
            if (visual is null)
            {
                return null;
            }

            if (includeSelf)
            {
                foreach (var v in visual.EnumerateSelfAndAncestors())
                {
                    if (v is T result)
                        return result;
                }
            }
            else
            {
                foreach (var v in visual.EnumerateAncestors())
                {
                    if (v is T result)
                        return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds first descendant of given type.
        /// </summary>
        /// <typeparam name="T">Descendant type.</typeparam>
        /// <param name="visual">The visual.</param>
        /// <param name="includeSelf">If given visual should be included in search.</param>
        /// <returns>First descendant of given type.</returns>
        public static T? FindDescendantOfType<T>(this Visual? visual, bool includeSelf = false) where T : class
        {
            if (visual is null)
            {
                return null;
            }

            if (includeSelf && visual is T result)
            {
                return result;
            }

            return FindDescendantOfTypeCore<T>(visual);
        }

        /// <summary>
        /// Enumerates an <see cref="Visual"/> and its ancestors in the visual tree.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The visual and its ancestors.</returns>
        public static IEnumerable<Visual> GetSelfAndVisualAncestors(this Visual visual)
        {
            ThrowHelper.ThrowIfNull(visual, nameof(visual));

            yield return visual;

            foreach (var ancestor in visual.GetVisualAncestors())
            {
                yield return ancestor;
            }
        }

        public static TransformedBounds? GetTransformedBounds(this Visual visual)
        {
            if (visual is null)
            {
                throw new ArgumentNullException(nameof(visual));
            }

            Rect clip = default;
            var transform = Matrix.Identity;

            bool Visit(Visual visual)
            {
                if (!visual.IsVisible)
                    return false;

                // The visual's bounds in local coordinates.
                var bounds = new Rect(visual.Bounds.Size);

                // If the visual has no parent, we've reached the root. We start the clip
                // rectangle with these bounds.
                if (visual.GetVisualParent() is not { } parent)
                {
                    clip = bounds;
                    return true;
                }

                // Otherwise recurse until the root visual is found, exiting early if one of the
                // ancestors is invisible.
                if (!Visit(parent))
                    return false;

                // Calculate the transform for this control from its offset and render transform.
                var renderTransform = Matrix.Identity;

                if (visual.HasMirrorTransform)
                {
                    var mirrorMatrix = new Matrix(-1.0, 0.0, 0.0, 1.0, visual.Bounds.Width, 0);
                    renderTransform *= mirrorMatrix;
                }

                if (visual.RenderTransform != null)
                {
                    var origin = visual.RenderTransformOrigin.ToPixels(bounds.Size);
                    var offset = Matrix.CreateTranslation(origin);
                    var finalTransform = (-offset) * visual.RenderTransform.Value * offset;
                    renderTransform *= finalTransform;
                }

                transform = renderTransform *
                    Matrix.CreateTranslation(visual.Bounds.Position) *
                    transform;

                // If the visual is clipped, update the clip bounds.
                if (visual.ClipToBounds)
                {
                    var globalBounds = bounds.TransformToAABB(transform);
                    var clipBounds = visual.ClipToBounds ?
                        globalBounds.Intersect(clip) :
                        clip;
                    clip = clip.Intersect(clipBounds);
                }

                return true;
            }

            return Visit(visual) ? new(new(visual.Bounds.Size), clip, transform) : null;
        }

        /// <summary>
        /// Gets the first visual in the visual tree whose bounds contain a point.
        /// </summary>
        /// <param name="visual">The root visual to test.</param>
        /// <param name="p">The point.</param>
        /// <returns>The visual at the requested point.</returns>
        public static Visual? GetVisualAt(this Visual visual, Point p)
        {
            ThrowHelper.ThrowIfNull(visual, nameof(visual));

            return visual.GetVisualAt(p, x => x.IsVisible);
        }

        /// <summary>
        /// Gets the first visual in the visual tree whose bounds contain a point.
        /// </summary>
        /// <param name="visual">The root visual to test.</param>
        /// <param name="p">The point.</param>
        /// <param name="filter">
        /// A filter predicate. If the predicate returns false then the visual and all its
        /// children will be excluded from the results.
        /// </param>
        /// <returns>The visual at the requested point.</returns>
        public static Visual? GetVisualAt(this Visual visual, Point p, Func<Visual, bool> filter)
        {
            ThrowHelper.ThrowIfNull(visual, nameof(visual));

            var root = visual.GetVisualRoot();

            if (root is null)
            {
                return null;
            }

            var rootPoint = visual.TranslatePoint(p, (Visual)root);

            if (rootPoint.HasValue)
            {
                return root.HitTester.HitTestFirst(rootPoint.Value, visual, filter);
            }

            return null;
        }

        /// <summary>
        /// Enumerates the visible visuals in the visual tree whose bounds contain a point.
        /// </summary>
        /// <param name="visual">The root visual to test.</param>
        /// <param name="p">The point.</param>
        /// <returns>The visuals at the requested point.</returns>
        public static IEnumerable<Visual> GetVisualsAt(
            this Visual visual,
            Point p)
        {
            ThrowHelper.ThrowIfNull(visual, nameof(visual));

            return visual.GetVisualsAt(p, x => x.IsVisible);
        }

        /// <summary>
        /// Enumerates the visuals in the visual tree whose bounds contain a point.
        /// </summary>
        /// <param name="visual">The root visual to test.</param>
        /// <param name="p">The point.</param>
        /// <param name="filter">
        /// A filter predicate. If the predicate returns false then the visual and all its
        /// children will be excluded from the results.
        /// </param>
        /// <returns>The visuals at the requested point.</returns>
        public static IEnumerable<Visual> GetVisualsAt(
            this Visual visual,
            Point p,
            Func<Visual, bool> filter)
        {
            ThrowHelper.ThrowIfNull(visual, nameof(visual));

            var root = visual.GetVisualRoot();

            if (root is null)
            {
                return Array.Empty<Visual>();
            }

            var rootPoint = visual.TranslatePoint(p, (Visual)root);

            if (rootPoint.HasValue)
            {
                return root.HitTester.HitTest(rootPoint.Value, visual, filter);
            }

            return Enumerable.Empty<Visual>();
        }

        /// <summary>
        /// Enumerates the children of an <see cref="Visual"/> in the visual tree.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The visual children.</returns>
        public static IEnumerable<Visual> GetVisualChildren(this Visual visual)
        {
            return visual.VisualChildren;
        }

        /// <summary>
        /// Enumerates the descendants of an <see cref="Visual"/> in the visual tree.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The visual's ancestors.</returns>
        public static IEnumerable<Visual> GetVisualDescendants(this Visual visual)
        {
            foreach (var descendant in visual.EnumerateDescendants())
            {
                yield return descendant;
            }
        }

        /// <summary>
        /// Enumerates an <see cref="Visual"/> and its descendants in the visual tree.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The visual and its ancestors.</returns>
        public static IEnumerable<Visual> GetSelfAndVisualDescendants(this Visual visual)
        {
            yield return visual;

            foreach (var ancestor in visual.GetVisualDescendants())
            {
                yield return ancestor;
            }
        }

        /// <summary>
        /// Gets the visual parent of an <see cref="Visual"/>.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The parent, or null if the visual is unparented.</returns>
        public static Visual? GetVisualParent(this Visual visual)
        {
            return visual.VisualParent;
        }

        /// <summary>
        /// Gets the visual parent of an <see cref="Visual"/>.
        /// </summary>
        /// <typeparam name="T">The type of the visual parent.</typeparam>
        /// <param name="visual">The visual.</param>
        /// <returns>
        /// The parent, or null if the visual is unparented or its parent is not of type <typeparamref name="T"/>.
        /// </returns>
        public static T? GetVisualParent<T>(this Visual visual) where T : class
        {
            return visual.VisualParent as T;
        }

        /// <summary>
        /// Gets the root visual for an <see cref="Visual"/>.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>
        /// The root visual or null if the visual is not rooted.
        /// </returns>
        public static IRenderRoot? GetVisualRoot(this Visual visual)
        {
            ThrowHelper.ThrowIfNull(visual, nameof(visual));

            return visual as IRenderRoot ?? visual.VisualRoot;
        }

        /// <summary>
        /// Returns a value indicating whether this control is attached to a visual root.
        /// </summary>
        public static bool IsAttachedToVisualTree(this Visual visual) => visual.IsAttachedToVisualTree;

        /// <summary>
        /// Tests whether an <see cref="Visual"/> is an ancestor of another visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="target">The potential descendant.</param>
        /// <returns>
        /// True if <paramref name="visual"/> is an ancestor of <paramref name="target"/>;
        /// otherwise false.
        /// </returns>
        public static bool IsVisualAncestorOf(this Visual? visual, Visual? target)
        {
            if (visual is null || target is null)
            {
                return false;
            }

            // Quick check: ancestor must be at a lower level (closer to root)
            if (visual.VisualLevel >= target.VisualLevel)
            {
                return false;
            }

            // Walk up from target until we reach visual's level
            Visual? current = target.VisualParent;
            var targetLevel = target.VisualLevel - 1;
            var visualLevel = visual.VisualLevel;

            while (current != null && targetLevel > visualLevel)
            {
                current = current.VisualParent;
                targetLevel--;
            }

            return current == visual;
        }

        public static IEnumerable<Visual> SortByZIndex(this IEnumerable<Visual> elements)
        {
            // Fast path for IReadOnlyList
            if (elements is IReadOnlyList<Visual> list)
            {
                var output = new List<Visual>(list.Count);
                list.SortByZIndexInto(output);
                return output;
            }

            // Fallback for other enumerables
            var materializedList = elements.ToList();
            var result = new List<Visual>(materializedList.Count);
            ((IReadOnlyList<Visual>)materializedList).SortByZIndexInto(result);
            return result;
        }

        /// <summary>
        /// Sorts the given visuals by ZIndex using a pooled list to reduce allocations.
        /// </summary>
        /// <param name="elements">The elements to sort.</param>
        /// <param name="output">The output list that will be populated with sorted elements.</param>
        public static void SortByZIndexInto(this IReadOnlyList<Visual> elements, List<Visual> output)
        {
            output.Clear();
            var count = elements.Count;

            if (count == 0)
                return;

            // For small lists, use simple insertion sort to avoid allocations
            if (count <= 8)
            {
                for (int i = 0; i < count; i++)
                {
                    var element = elements[i];
                    var zIndex = element.ZIndex;
                    var insertIndex = output.Count;

                    // Find insertion point (stable sort - maintain order for same ZIndex)
                    for (int j = 0; j < output.Count; j++)
                    {
                        if (output[j].ZIndex > zIndex)
                        {
                            insertIndex = j;
                            break;
                        }
                    }

                    output.Insert(insertIndex, element);
                }
                return;
            }

            // For larger lists, copy and sort
            for (int i = 0; i < count; i++)
                output.Add(elements[i]);

            // Stable sort by ZIndex using custom comparer
            output.Sort((a, b) => a.ZIndex.CompareTo(b.ZIndex));
        }

        /// <summary>
        /// Returns a struct-based enumerable for descendants using depth-first traversal that minimizes allocations.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>A struct enumerable for the visual's descendants.</returns>
        public static VisualDescendantsEnumerable EnumerateDescendants(this Visual visual)
        {
            return new VisualDescendantsEnumerable(visual);
        }

        private static T? FindDescendantOfTypeCore<T>(Visual visual) where T : class
        {
            var visualChildren = visual.VisualChildren;
            var visualChildrenCount = visualChildren.Count;

            for (var i = 0; i < visualChildrenCount; i++)
            {
                Visual child = visualChildren[i];

                if (child is T result)
                {
                    return result;
                }

                var childResult = FindDescendantOfTypeCore<T>(child);

                if (!(childResult is null))
                {
                    return childResult;
                }
            }

            return null;
        }

        private class ZOrderElement : IComparable<ZOrderElement>
        {
            public Visual? Element { get; set; }
            public int Index { get; set; }
            public int ZIndex { get; set; }

            class ZOrderComparer : IComparer<ZOrderElement>
            {
                public int Compare(ZOrderElement? x, ZOrderElement? y)
                {
                    if (ReferenceEquals(x, y)) return 0;
                    if (ReferenceEquals(null, y)) return 1;
                    if (ReferenceEquals(null, x)) return -1;
                    return x.CompareTo(y);
                }
            }

            public static IComparer<ZOrderElement> Comparer { get; } = new ZOrderComparer();
            
            public int CompareTo(ZOrderElement? other)
            {
                if (other is null)
                    return 1;

                var z = other.ZIndex - ZIndex;

                if (z != 0)
                {
                    return z;
                }
                else
                {
                    return other.Index - Index;
                }
            }
        }
    }

    /// <summary>
    /// A struct-based enumerable for visual ancestors that doesn't allocate.
    /// </summary>
    public readonly struct VisualAncestorsEnumerable
    {
        private readonly Visual? _visual;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal VisualAncestorsEnumerable(Visual? visual)
        {
            _visual = visual;
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VisualAncestorsEnumerator GetEnumerator() => new(_visual);

        /// <summary>
        /// A struct-based enumerator for visual ancestors.
        /// </summary>
        public struct VisualAncestorsEnumerator
        {
            private Visual? _current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal VisualAncestorsEnumerator(Visual? visual)
            {
                _current = visual;
            }

            /// <summary>
            /// Gets the current visual.
            /// </summary>
            public Visual Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current!;
            }

            /// <summary>
            /// Moves to the next ancestor.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                _current = _current?.VisualParent;
                return _current != null;
            }
        }
    }

    /// <summary>
    /// A struct-based enumerable for self and visual ancestors that doesn't allocate.
    /// </summary>
    public readonly struct SelfAndAncestorsEnumerable
    {
        private readonly Visual? _visual;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal SelfAndAncestorsEnumerable(Visual? visual)
        {
            _visual = visual;
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SelfAndAncestorsEnumerator GetEnumerator() => new(_visual);

        /// <summary>
        /// A struct-based enumerator for self and visual ancestors.
        /// </summary>
        public struct SelfAndAncestorsEnumerator
        {
            private Visual? _current;
            private bool _started;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal SelfAndAncestorsEnumerator(Visual? visual)
            {
                _current = visual;
                _started = false;
            }

            /// <summary>
            /// Gets the current visual.
            /// </summary>
            public Visual Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current!;
            }

            /// <summary>
            /// Moves to the next visual (self first, then ancestors).
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (!_started)
                {
                    _started = true;
                    return _current != null;
                }

                _current = _current?.VisualParent;
                return _current != null;
            }
        }
    }

    /// <summary>
    /// A struct-based enumerable for visual descendants using depth-first traversal.
    /// Note: This allocates a stack internally but avoids iterator state machine overhead.
    /// </summary>
    public struct VisualDescendantsEnumerable
    {
        private readonly Visual? _root;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal VisualDescendantsEnumerable(Visual? visual)
        {
            _root = visual;
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VisualDescendantsEnumerator GetEnumerator() => new(_root);

        /// <summary>
        /// A struct-based enumerator for visual descendants using depth-first traversal.
        /// </summary>
        public struct VisualDescendantsEnumerator
        {
            private readonly Stack<(Visual parent, int index)>? _stack;
            private Visual? _current;
            private Visual? _currentParent;
            private int _currentIndex;

            internal VisualDescendantsEnumerator(Visual? root)
            {
                _current = null;
                _currentParent = root;
                _currentIndex = 0;

                if (root != null && root.VisualChildren.Count > 0)
                {
                    _stack = new Stack<(Visual, int)>(16);
                }
                else
                {
                    _stack = null;
                }
            }

            /// <summary>
            /// Gets the current visual.
            /// </summary>
            public Visual Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current!;
            }

            /// <summary>
            /// Moves to the next descendant using depth-first traversal.
            /// </summary>
            public bool MoveNext()
            {
                if (_currentParent == null)
                    return false;

                var children = _currentParent.VisualChildren;

                // Try to get next child at current level
                while (_currentIndex < children.Count)
                {
                    var child = children[_currentIndex];
                    _currentIndex++;
                    _current = child;

                    // If this child has children, push current state and descend
                    if (child.VisualChildren.Count > 0)
                    {
                        _stack?.Push((_currentParent, _currentIndex));
                        _currentParent = child;
                        _currentIndex = 0;
                    }

                    return true;
                }

                // Pop back up the stack
                while (_stack != null && _stack.Count > 0)
                {
                    (_currentParent, _currentIndex) = _stack.Pop();
                    children = _currentParent.VisualChildren;

                    while (_currentIndex < children.Count)
                    {
                        var child = children[_currentIndex];
                        _currentIndex++;
                        _current = child;

                        if (child.VisualChildren.Count > 0)
                        {
                            _stack.Push((_currentParent, _currentIndex));
                            _currentParent = child;
                            _currentIndex = 0;
                        }

                        return true;
                    }
                }

                _currentParent = null;
                return false;
            }
        }
    }
}
