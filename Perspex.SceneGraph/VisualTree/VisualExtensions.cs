// -----------------------------------------------------------------------
// <copyright file="VisualExtensions.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.VisualTree
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class VisualExtensions
    {
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

        public static IEnumerable<IVisual> GetSelfAndVisualAncestors(this IVisual visual)
        {
            yield return visual;

            foreach (var ancestor in visual.GetVisualAncestors())
            {
                yield return ancestor;
            }
        }

        public static IVisual GetVisualAt(this IVisual visual, Point p)
        {
            Contract.Requires<NullReferenceException>(visual != null);

            return visual.GetVisualsAt(p).FirstOrDefault();
        }

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

        public static IEnumerable<IVisual> GetVisualChildren(this IVisual visual)
        {
            return visual.VisualChildren;
        }

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

        public static IEnumerable<IVisual> GetSelfAndVisualDescendents(this IVisual visual)
        {
            yield return visual;

            foreach (var ancestor in visual.GetVisualDescendents())
            {
                yield return ancestor;
            }
        }

        public static IVisual GetVisualParent(this IVisual visual)
        {
            return visual.VisualParent;
        }

        public static T GetVisualParent<T>(this IVisual visual) where T : class
        {
            return visual.VisualParent as T;
        }
    }
}
