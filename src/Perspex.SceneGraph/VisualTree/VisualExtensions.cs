﻿// -----------------------------------------------------------------------
// <copyright file="VisualExtensions.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.VisualTree
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Provides extension methods for working with visual tree.
    /// </summary>
    public static class VisualExtensions
    {
        /// <summary>
        /// Enumerates the ancestors of an <see cref="IVisual"/> in the visual tree.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The visual's ancestors.</returns>
        public static IEnumerable<IVisual> GetVisualAncestors(this IVisual visual)
        {
            Contract.Requires<NullReferenceException>(visual != null);

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
            Contract.Requires<NullReferenceException>(visual != null);

            return visual.GetVisualsAt(p).FirstOrDefault();
        }

        /// <summary>
        /// Enumerates the visuals in the visual tree whose bounds contain a point.
        /// </summary>
        /// <param name="visual">The root visual to test.</param>
        /// <param name="p">The point.</param>
        /// <returns>The visuals at the requested point.</returns>
        public static IEnumerable<IVisual> GetVisualsAt(this IVisual visual, Point p)
        {
            Contract.Requires<NullReferenceException>(visual != null);

            if (visual.Bounds.Contains(p))
            {
                p -= visual.Bounds.Position;

                if (visual.VisualChildren.Any())
                {
                    foreach (IVisual child in visual.VisualChildren)
                    {
                        foreach (IVisual v in child.GetVisualsAt(p))
                        {
                            yield return v;
                        }
                    }
                }

                yield return visual;
            }
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
        /// Enumerates the descendents of an <see cref="IVisual"/> in the visual tree.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The visual's ancestors.</returns>
        public static IEnumerable<IVisual> GetVisualDescendents(this IVisual visual)
        {
            foreach (IVisual child in visual.VisualChildren)
            {
                yield return child;

                foreach (IVisual descendent in child.GetVisualDescendents())
                {
                    yield return descendent;
                }
            }
        }

        /// <summary>
        /// Enumerates an <see cref="IVisual"/> and its descendents in the visual tree.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The visual and its ancestors.</returns>
        public static IEnumerable<IVisual> GetSelfAndVisualDescendents(this IVisual visual)
        {
            yield return visual;

            foreach (var ancestor in visual.GetVisualDescendents())
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
        public static IVisual GetVisualRoot(this IVisual visual)
        {
            Contract.Requires<NullReferenceException>(visual != null);

            var parent = visual.VisualParent;

            while (parent != null)
            {
                visual = parent;
                parent = visual.VisualParent;
            }

            return visual;
        }

        /// <summary>
        /// Tests whether an <see cref="IVisual"/> is an ancestor of another visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="target">The potential descendent.</param>
        /// <returns>
        /// True if <paramref name="visual"/> is an ancestor of <paramref name="target"/>;
        /// otherwise false.
        /// </returns>
        public static bool IsVisualAncestorOf(this IVisual visual, IVisual target)
        {
            return target.GetVisualAncestors().Any(x => x == visual);
        }
    }
}
