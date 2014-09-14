// -----------------------------------------------------------------------
// <copyright file="LogicalExtensions.cs" company="Steven Kirk">
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

    public static class LogicalExtensions
    {
        public static T FindControl<T>(this ILogical control, string id) where T : Control
        {
            return control.GetLogicalDescendents()
                .OfType<T>()
                .FirstOrDefault(x => x.Id == id);
        }

        public static IEnumerable<ILogical> GetLogicalDescendents(this ILogical control)
        {
            foreach (ILogical child in control.LogicalChildren)
            {
                yield return child;

                foreach (ILogical descendent in child.GetLogicalDescendents())
                {
                    yield return descendent;
                }
            }
        }
    }
}
