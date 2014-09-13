// -----------------------------------------------------------------------
// <copyright file="ControlExtensions.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Styling;

    public static class ControlExtensions
    {
        public static IEnumerable<Control> GetTemplateControls(this ITemplatedControl control)
        {
            return GetTemplateControls(control, (IVisual)control);
        }

        public static IEnumerable<Control> GetTemplateControls(ITemplatedControl templated, IVisual parent)
        {
            IVisual visual = parent as IVisual;

            foreach (IVisual child in visual.VisualChildren.OfType<Control>().Where(x => x.TemplatedParent == templated))
            {
                yield return (Control)child;

                foreach (IVisual grandchild in GetTemplateControls(templated, child))
                {
                    yield return (Control)grandchild;
                }
            }
        }
    }
}
