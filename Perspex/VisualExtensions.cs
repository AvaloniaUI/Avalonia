// -----------------------------------------------------------------------
// <copyright file="VisualExtensions.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.Styling;

    public static class VisualExtensions
    {
        public static IEnumerable<IVisual> GetVisual(this IVisual visual, Func<Selector, Selector> selector)
        {
            Selector sel = selector(new Selector());
            IEnumerable<IVisual> visuals = Enumerable.Repeat(visual, 1).Concat(visual.GetVisualDescendents());

            foreach (IStyleable v in visuals.OfType<IStyleable>())
            {
                using (StyleActivator activator = sel.GetActivator(v))
                {
                    if (activator.CurrentValue)
                    {
                        yield return (IVisual)v;
                    }
                }
            }
        }

        public static T GetVisualAncestor<T>(this IVisual visual) where T : class
        {
            Contract.Requires<NullReferenceException>(visual != null);

            visual = visual.VisualParent;

            while (visual != null)
            {
                if (visual is T)
                {
                    return (T)visual;
                }
                else
                {
                    visual = visual.VisualParent;
                }
            }

            return null;
        }

        public static T GetVisualAncestorOrSelf<T>(this IVisual visual) where T : class
        {
            Contract.Requires<NullReferenceException>(visual != null);

            return (visual as T) ?? visual.GetVisualAncestor<T>();
        }

        public static IVisual GetVisualAt(this IVisual visual, Point p)
        {
            Contract.Requires<NullReferenceException>(visual != null);

            if (visual.Bounds.Contains(p))
            {
                p -= visual.Bounds.Position;

                if (visual.VisualChildren.Any())
                {
                    foreach (IVisual child in visual.VisualChildren)
                    {
                        IVisual hit = child.GetVisualAt(p);

                        if (hit != null)
                        {
                            return hit;
                        }
                    }
                }
                else
                {
                    return visual;
                }
            }

            return null;
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
    }
}
