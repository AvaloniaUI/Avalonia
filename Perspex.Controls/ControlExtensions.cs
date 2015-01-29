// -----------------------------------------------------------------------
// <copyright file="ControlExtensions.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Styling;
    using Perspex.VisualTree;

    public static class ControlExtensions
    {
        // TODO: This needs to traverse the logical tree, not the visual.
        public static T FindControl<T>(this Control control, string id) where T : Control
        {
            return control.GetVisualDescendents()
                .OfType<T>()
                .FirstOrDefault(x => x.Id == id);
        }

        public static IEnumerable<Control> GetTemplateControls(this ITemplatedControl control)
        {
            var visual = control as IVisual;

            if (visual != null)
            {
                return visual.GetVisualDescendents()
                    .OfType<Control>()
                    .TakeWhile(x => x.TemplatedParent != null)
                    .Where(x => x.TemplatedParent == control);
            }
            else
            {
                return Enumerable.Empty<Control>();
            }
        }
    }
}
