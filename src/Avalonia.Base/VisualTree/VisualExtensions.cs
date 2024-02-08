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
            Visual? v = visual ?? throw new ArgumentNullException(nameof(visual));
            var result = 0;

            v = v.VisualParent;

            while (v != null)
            {
                v = v.VisualParent;

                result++;
            }

            return result;
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

            void GoUpwards(ref Visual? node, int count)
            {
                for (int i = 0; i < count; ++i)
                {
                    node = node?.VisualParent;
                }
            }

            Visual? v = visual;
            Visual? t = target;

            // We want to find lowest node first, then make sure that both nodes are at the same height.
            // By doing that we can sometimes find out that other node is our lowest common ancestor.
            var firstHeight = CalculateDistanceFromRoot(v);
            var secondHeight = CalculateDistanceFromRoot(t);

            if (firstHeight > secondHeight)
            {
                GoUpwards(ref v, firstHeight - secondHeight);
            }
            else
            {
                GoUpwards(ref t, secondHeight - firstHeight);
            }

            if (v == t)
            {
                return v;
            }

            while (v != null && t != null)
            {
                Visual? firstParent = v.VisualParent;
                Visual? secondParent = t.VisualParent;

                if (firstParent == secondParent)
                {
                    return firstParent;
                }

                v = v.VisualParent;
                t = t.VisualParent;
            }

            return null;
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

            Visual? parent = includeSelf ? visual : visual.VisualParent;

            while (parent != null)
            {
                if (parent is T result)
                {
                    return result;
                }

                parent = parent.VisualParent;
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
            foreach (Visual child in visual.VisualChildren)
            {
                yield return child;

                foreach (Visual descendant in child.GetVisualDescendants())
                {
                    yield return descendant;
                }
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
            Visual? current = target?.VisualParent;

            while (current != null)
            {
                if (current == visual)
                {
                    return true;
                }

                current = current.VisualParent;
            }

            return false;
        }

        public static IEnumerable<Visual> SortByZIndex(this IEnumerable<Visual> elements)
        {
            return elements
                .Select((element, index) => new ZOrderElement
                {
                    Element = element,
                    Index = index,
                    ZIndex = element.ZIndex,
                })
                .OrderBy(x => x, ZOrderElement.Comparer)
                .Select(x => x.Element!);
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
}
