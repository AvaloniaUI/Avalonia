// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Templates
{
    public static class TemplateExtensions
    {
        /// <summary>
        /// Gets a named control from a templated control's template children.
        /// </summary>
        /// <param name="template">The control template.</param>
        /// <param name="name">The name of the control.</param>
        /// <param name="templatedParent">The templated parent control.</param>
        /// <returns>An <see cref="IControl"/> or null if the control was not found.</returns>
        public static IControl FindName(this IControlTemplate template, string name, IControl templatedParent)
        {
            Contract.Requires<ArgumentNullException>(template != null);
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<ArgumentNullException>(templatedParent != null);

            return ((IControl)templatedParent.GetVisualChildren().FirstOrDefault())?.FindControl<Canvas>(name);
        }

        /// <summary>
        /// Gets a named control from a templated control's template children.
        /// </summary>
        /// <typeparam name="T">The type of the template child.</typeparam>
        /// <param name="template">The control template.</param>
        /// <param name="name">The name of the control.</param>
        /// <param name="templatedParent">The templated parent control.</param>
        /// <returns>An <see cref="IControl"/> or null if the control was not found.</returns>
        public static T FindName<T>(this IControlTemplate template, string name, IControl templatedParent)
            where T : class, IControl
        {
            Contract.Requires<ArgumentNullException>(template != null);
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<ArgumentNullException>(templatedParent != null);

            return template.FindName(name, templatedParent) as T;
        }

        public static IEnumerable<IControl> GetTemplateChildren(this ITemplatedControl control)
        {
            foreach (IControl child in GetTemplateChildren((IControl)control, control))
            {
                yield return child;
            }
        }

        private static IEnumerable<IControl> GetTemplateChildren(IControl control, ITemplatedControl templatedParent)
        {
            foreach (IControl child in control.GetVisualChildren())
            {
                var childTemplatedParent = child.TemplatedParent;

                if (childTemplatedParent == templatedParent)
                {
                    yield return child;
                }

                if (childTemplatedParent != null)
                {
                    foreach (var descendant in GetTemplateChildren(child, templatedParent))
                    {
                        yield return descendant;
                    }
                }
            }
        }
    }
}
