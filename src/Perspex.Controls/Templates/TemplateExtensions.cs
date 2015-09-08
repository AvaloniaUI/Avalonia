// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Perspex.Controls;
using Perspex.Styling;
using Perspex.VisualTree;

namespace Perspex.Controls.Templates
{
    public static class TemplateExtensions
    {
        public static IReparentingHost FindReparentingHost(this IControl control)
        {
            var tp = control.TemplatedParent;
            var chain = new List<IReparentingHost>();

            while (tp != null)
            {
                var reparentingHost = tp as IReparentingHost;
                var styleable = tp as IStyleable;

                if (reparentingHost != null)
                {
                    chain.Add(reparentingHost);
                }

                tp = styleable?.TemplatedParent ?? null;
            }

            foreach (var reparenting in chain.AsEnumerable().Reverse())
            {
                if (reparenting.WillReparentChildrenOf(control))
                {
                    return reparenting;
                }
            }

            return null;
        }

        public static T FindTemplateChild<T>(this ITemplatedControl control, string name) where T : INamed
        {
            return control.GetTemplateChildren().OfType<T>().SingleOrDefault(x => x.Name == name);
        }

        public static T GetTemplateChild<T>(this ITemplatedControl control, string name) where T : INamed
        {
            var result = control.FindTemplateChild<T>(name);

            if (result == null)
            {
                throw new InvalidOperationException(string.Format(
                    "Could not find template child '{0}' of type '{1}' in template for '{2}'.",
                    name,
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
