using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Templates
{
    public static class TemplateExtensions
    {
        public static IEnumerable<Control> GetTemplateChildren(this ITemplatedControl control)
        {
            foreach (Control child in GetTemplateChildren((Control)control, control))
            {
                yield return child;
            }
        }

        private static IEnumerable<Control> GetTemplateChildren(Control control, ITemplatedControl templatedParent)
        {
            foreach (Control child in control.GetVisualChildren())
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
