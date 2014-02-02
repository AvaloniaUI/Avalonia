// -----------------------------------------------------------------------
// <copyright file="VisualExtensions.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Linq;

    public static class VisualExtensions
    {
        public static T GetVisualAncestor<T>(this Visual visual) where T : Visual
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

        public static Visual GetVisualAt(this Visual visual, Point p)
        {
            Contract.Requires<NullReferenceException>(visual != null);

            if (visual.Bounds.Contains(p))
            {
                p -= visual.Bounds.Position;

                if (visual.VisualChildren.Any())
                {
                    foreach (Visual child in visual.VisualChildren)
                    {
                        Visual hit = child.GetVisualAt(p);

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
    }
}
