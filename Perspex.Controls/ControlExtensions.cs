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
    using Perspex.LogicalTree;
    using Perspex.Styling;
    using Perspex.VisualTree;

    public static class ControlExtensions
    {
        public static T FindControl<T>(this Control control, string id) where T : Control
        {
            return control.GetLogicalDescendents()
                .OfType<T>()
                .FirstOrDefault(x => x.Id == id);
        }
    }
}
