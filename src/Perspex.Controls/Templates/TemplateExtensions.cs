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
                if (child.TemplatedParent == templatedParent)
                {
                    yield return child;
                }

                if (child.TemplatedParent != null)
                {
                    foreach (var descendent in GetTemplateChildren(child, templatedParent))
                    {
                        yield return descendent;
                    }
                }
            }
        }
    }
}
