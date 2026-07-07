using System;
using System.Collections.Generic;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Contains extension methods for <see cref="TemplatedControl"/>.
    /// </summary>
    public static class TemplateExtensions
    {
        /// <summary>
        /// Gets the list of all control descendants that are part of the template of a <see cref="TemplatedControl"/>,
        /// i.e. their <see cref="StyledElement.TemplatedParent"/> is <paramref name="control"/>.
        /// </summary>
        /// <param name="control">The control whose descendants will be returned.</param>
        /// <returns>An enumeration of <see cref="Control"/> objects.</returns>
        [Obsolete($"Use {nameof(GetTemplateDescendants)}")]
        public static IEnumerable<Control> GetTemplateChildren(this TemplatedControl control)
        {
            foreach (var child in control.GetTemplateDescendants())
            {
                if (child is Control childControl)
                {
                    yield return childControl;
                }
            }
        }

        /// <summary>
        /// Gets the list of all visual descendants that are part of the template of a <see cref="TemplatedControl"/>,
        /// i.e. their <see cref="StyledElement.TemplatedParent"/> is <paramref name="control"/>.
        /// </summary>
        /// <param name="control">The control whose descendants will be returned.</param>
        /// <returns>An enumeration of <see cref="Visual"/> objects.</returns>
        public static IEnumerable<Visual> GetTemplateDescendants(this TemplatedControl control)
        {
            return GetTemplateDescendants(control, control);
        }

        private static IEnumerable<Visual> GetTemplateDescendants(Visual control, TemplatedControl templatedParent)
        {
            foreach (var child in control.GetVisualChildren())
            {
                var childTemplatedParent = child.TemplatedParent;

                if (childTemplatedParent == templatedParent)
                {
                    yield return child;
                }

                if (childTemplatedParent != null)
                {
                    foreach (var descendant in GetTemplateDescendants(child, templatedParent))
                    {
                        yield return descendant;
                    }
                }
            }
        }
    }
}
