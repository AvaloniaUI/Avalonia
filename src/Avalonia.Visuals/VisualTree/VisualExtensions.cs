// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Rendering;

namespace Avalonia.VisualTree
{
    /// <summary>
    /// Provides extension methods for working with the visual tree.
    /// </summary>
    public static class VisualExtensions
    {
        /// <summary>
        /// Calculates the distance from a visual's <see cref="IRenderRoot"/>.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="ancestor">The ancestor visual.</param>
        /// <returns>
        /// The number of steps from the visual to the ancestor or -1 if
        /// <paramref name="visual"/> is not a descendent of <paramref name="ancestor"/>.
        /// </returns>
        public static int CalculateDistanceFromAncestor(this IVisual visual, IVisual ancestor)
        {
            Contract.Requires<ArgumentNullException>(visual != null);

            var result = 0;

            while (visual != null && visual != ancestor)
            {
                ++result;
                visual = visual.VisualParent;
            }

            return visual != null ? result : -1;
        }

        /// <summary>
        /// Tries to get the first common ancestor of two visuals.
        /// </summary>
        /// <param name="visual">The first visual.</param>
        /// <param name="target">The second visual.</param>
        /// <returns>The common ancestor, or null if not found.</returns>
        public static IVisual FindCommonVisualAncestor(this IVisual visual, IVisual target)
        {
            Contract.Requires<ArgumentNullException>(visual != null);

            return visual.GetSelfAndVisualAncestors().Intersect(target.GetSelfAndVisualAncestors())
                .FirstOrDefault();
        }

        /// <summary>
        /// Enumerates the ancestors of an <see cref="IVisual"/> in the visual tree.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The visual's ancestors.</returns>
        public static IEnumerable<IVisual> GetVisualAncestors(this IVisual visual)
        {
            Contract.Requires<ArgumentNullException>(visual != null);

            visual = visual.VisualParent;

            while (visual != null)
            {
                yield return visual;
                visual = visual.VisualParent;
            }
        }

        /// <summary>
        /// Enumerates an <see cref="IVisual"/> and its ancestors in the visual tree.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The visual and its ancestors.</returns>
        public static IEnumerable<IVisual> GetSelfAndVisualAncestors(this IVisual visual)
        {
            Contract.Requires<ArgumentNullException>(visual != null);

            yield return visual;

            foreach (var ancestor in visual.GetVisualAncestors())
            {
                yield return ancestor;
            }
        }

        /// <summary>
        /// Gets the first visual in the visual tree whose bounds contain a point.
        /// </summary>
        /// <param name="visual">The root visual to test.</param>
        /// <param name="p">The point.</param>
        /// <returns>The visuals at the requested point.</returns>
        public static IVisual GetVisualAt(this IVisual visual, Point p)
        {
            Contract.Requires<ArgumentNullException>(visual != null);

            return visual.GetVisualsAt(p).FirstOrDefault();
        }

        /// <summary>
        /// Enumerates the visible visuals in the visual tree whose bounds contain a point.
        /// </summary>
        /// <param name="visual">The root visual to test.</param>
        /// <param name="p">The point.</param>
        /// <returns>The visuals at the requested point.</returns>
        public static IEnumerable<IVisual> GetVisualsAt(
            this IVisual visual,
            Point p)
        {
            Contract.Requires<ArgumentNullException>(visual != null);

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
        public static IEnumerable<IVisual> GetVisualsAt(
            this IVisual visual,
            Point p,
            Func<IVisual, bool> filter)
        {
            Contract.Requires<ArgumentNullException>(visual != null);

            var root = visual.GetVisualRoot();
            p = visual.TranslatePoint(p, root);
            return root.Renderer.HitTest(p, visual, filter);
        }

        /// <summary>
        /// Enumerates the children of an <see cref="IVisual"/> in the visual tree.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The visual children.</returns>
        public static IEnumerable<IVisual> GetVisualChildren(this IVisual visual)
        {
            return visual.VisualChildren;
        }

        /// <summary>
        /// Enumerates the descendants of an <see cref="IVisual"/> in the visual tree.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The visual's ancestors.</returns>
        public static IEnumerable<IVisual> GetVisualDescendants(this IVisual visual)
        {
            foreach (IVisual child in visual.VisualChildren)
            {
                yield return child;

                foreach (IVisual descendant in child.GetVisualDescendants())
                {
                    yield return descendant;
                }
            }
        }

        /// <summary>
        /// Enumerates an <see cref="IVisual"/> and its descendants in the visual tree.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The visual and its ancestors.</returns>
        public static IEnumerable<IVisual> GetSelfAndVisualDescendants(this IVisual visual)
        {
            yield return visual;

            foreach (var ancestor in visual.GetVisualDescendants())
            {
                yield return ancestor;
            }
        }

        /// <summary>
        /// Gets the visual parent of an <see cref="IVisual"/>.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The parent, or null if the visual is unparented.</returns>
        public static IVisual GetVisualParent(this IVisual visual)
        {
            return visual.VisualParent;
        }

        /// <summary>
        /// Gets the visual parent of an <see cref="IVisual"/>.
        /// </summary>
        /// <typeparam name="T">The type of the visual parent.</typeparam>
        /// <param name="visual">The visual.</param>
        /// <returns>
        /// The parent, or null if the visual is unparented or its parent is not of type <typeparamref name="T"/>.
        /// </returns>
        public static T GetVisualParent<T>(this IVisual visual) where T : class
        {
            return visual.VisualParent as T;
        }

        /// <summary>
        /// Gets the root visual for an <see cref="IVisual"/>.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>
        /// The root visual or null if the visual is not rooted.
        /// </returns>
        public static IRenderRoot GetVisualRoot(this IVisual visual)
        {
            Contract.Requires<ArgumentNullException>(visual != null);

            return visual as IRenderRoot ?? visual.VisualRoot;
        }
        
        /// <summary>
        /// Tests whether an <see cref="IVisual"/> is an ancestor of another visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="target">The potential descendant.</param>
        /// <returns>
        /// True if <paramref name="visual"/> is an ancestor of <paramref name="target"/>;
        /// otherwise false.
        /// </returns>
        public static bool IsVisualAncestorOf(this IVisual visual, IVisual target)
        {
            return target.GetVisualAncestors().Any(x => x == visual);
        }

        public static IEnumerable<IVisual> SortByZIndex(this IEnumerable<IVisual> elements)
        {
            return elements
                .Select((element, index) => new ZOrderElement
                {
                    Element = element,
                    Index = index,
                    ZIndex = element.ZIndex,
                })
                .OrderBy(x => x, null)
                .Select(x => x.Element);
        }

        private class ZOrderElement : IComparable<ZOrderElement>
        {
            public IVisual Element { get; set; }
            public int Index { get; set; }
            public int ZIndex { get; set; }

            public int CompareTo(ZOrderElement other)
            {
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
