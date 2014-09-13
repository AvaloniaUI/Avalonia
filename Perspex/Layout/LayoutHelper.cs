// -----------------------------------------------------------------------
// <copyright file="LayoutHelper.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Layout
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Perspex.Controls;

    public static class LayoutHelper
    {
        public static Size ApplyLayoutConstraints(Layoutable control, Size constraints)
        {
            double width = (control.Width > 0) ? control.Width : constraints.Width;
            double height = (control.Height > 0) ? control.Height : constraints.Height;
            width = Math.Min(width, control.MaxWidth);
            width = Math.Max(width, control.MinWidth);
            height = Math.Min(height, control.MaxHeight);
            height = Math.Max(height, control.MinHeight);
            return new Size(width, height);
        }
        
        public static Size MeasureDecorator(
            Control decorator,
            Control content,
            Size availableSize, 
            Thickness padding)
        {
            double width = 0;
            double height = 0;

            if (content != null)
            {
                content.Measure(availableSize.Deflate(padding));
                Size s = content.DesiredSize.Value.Inflate(padding);
                width = s.Width;
                height = s.Height;
            }

            if (decorator.Width > 0)
            {
                width = decorator.Width;
            }

            if (decorator.Height > 0)
            {
                height = decorator.Height;
            }

            return new Size(width, height);
        }
    }
}
