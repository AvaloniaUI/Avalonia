// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Layout
{
    /// <summary>
    /// Provides helper methods needed for layout.
    /// </summary>
    public static class LayoutHelper
    {
        /// <summary>
        /// Calculates a control's size based on its <see cref="ILayoutable.Width"/>,
        /// <see cref="ILayoutable.Height"/>, <see cref="ILayoutable.MinWidth"/>,
        /// <see cref="ILayoutable.MaxWidth"/>, <see cref="ILayoutable.MinHeight"/> and
        /// <see cref="ILayoutable.MaxHeight"/>.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="constraints">The space available for the control.</param>
        /// <returns>The control's size.</returns>
        public static Size ApplyLayoutConstraints(ILayoutable control, Size constraints)
        {
            var controlWidth = control.Width;
            var controlHeight = control.Height;

            double width = (controlWidth > 0) ? controlWidth : constraints.Width;
            double height = (controlHeight > 0) ? controlHeight : constraints.Height;
            width = Math.Min(width, control.MaxWidth);
            width = Math.Max(width, control.MinWidth);
            height = Math.Min(height, control.MaxHeight);
            height = Math.Max(height, control.MinHeight);
            return new Size(width, height);
        }

        public static Size MeasureChild(ILayoutable control, Size availableSize, Thickness padding,
            Thickness borderThickness)
        {
            return MeasureChild(control, availableSize, padding + borderThickness);
        }

        public static Size MeasureChild(ILayoutable control, Size availableSize, Thickness padding)
        {
            if (control != null)
            {
                control.Measure(availableSize.Deflate(padding));
                return control.DesiredSize.Inflate(padding);
            }

            return new Size(padding.Left + padding.Right, padding.Bottom + padding.Top);
        }

        public static Size ArrangeChild(ILayoutable child, Size availableSize, Thickness padding, Thickness borderThickness)
        {
            return ArrangeChild(child, availableSize, padding + borderThickness);
        }

        public static Size ArrangeChild(ILayoutable child, Size availableSize, Thickness padding)
        {
            child?.Arrange(new Rect(availableSize).Deflate(padding));

            return availableSize;
        }
    }
}
