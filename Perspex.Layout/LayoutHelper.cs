// -----------------------------------------------------------------------
// <copyright file="LayoutHelper.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Layout
{
    using System;

    public static class LayoutHelper
    {
        public static Size ApplyLayoutConstraints(ILayoutable control, Size constraints)
        {
            double width = (control.Width > 0) ? control.Width : constraints.Width;
            double height = (control.Height > 0) ? control.Height : constraints.Height;
            width = Math.Min(width, control.MaxWidth);
            width = Math.Max(width, control.MinWidth);
            height = Math.Min(height, control.MaxHeight);
            height = Math.Max(height, control.MinHeight);
            return new Size(width, height);
        }
    }
}
