// -----------------------------------------------------------------------
// <copyright file="TemplateExtensions.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Templates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Styling;
    using Perspex.VisualTree;

    public static class TemplateExtensions
    {
        public static T FindTemplateChild<T>(this ITemplatedControl control, string id) where T : INamed
        {
            return control.GetTemplateChildren().OfType<T>().SingleOrDefault(x => x.Name == id);
        }

        public static T GetTemplateChild<T>(this ITemplatedControl control, string id) where T : INamed
        {
            var result = control.FindTemplateChild<T>(id);

            if (result == null)
            {
                throw new InvalidOperationException(string.Format(
                    "Could not find template child '{0}' of type '{1}' in template for '{2}'.",
                    id,
                    typeof(T).FullName,
                    control.GetType().FullName));
            }

            return result;
        }

        public static IEnumerable<Control> GetTemplateChildren(this ITemplatedControl control)
        {
            var visual = control as IVisual;

            if (visual != null)
            {
                // TODO: This searches the whole descendent tree - it can stop when it exits the 
                // template.
                return visual.GetVisualDescendents()
                    .OfType<Control>()
                    .Where(x => x.TemplatedParent == control);
            }
            else
            {
                return Enumerable.Empty<Control>();
            }
        }
    }
}
