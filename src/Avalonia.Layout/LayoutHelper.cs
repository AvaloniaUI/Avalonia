using System;
using Avalonia.Utilities;
using Avalonia.VisualTree;

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
            var minmax = new MinMax(control);

            return new Size(
                MathUtilities.Clamp(constraints.Width, minmax.MinWidth, minmax.MaxWidth),
                MathUtilities.Clamp(constraints.Height, minmax.MinHeight, minmax.MaxHeight));
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

        /// <summary>
        /// Invalidates measure for given control and all visual children recursively.
        /// </summary>
        public static void InvalidateSelfAndChildrenMeasure(ILayoutable control)
        {
            void InnerInvalidateMeasure(IVisual target)
            {
                if (target is ILayoutable targetLayoutable)
                {
                    targetLayoutable.InvalidateMeasure();
                }

                var visualChildren = target.VisualChildren;
                var visualChildrenCount = visualChildren.Count;

                for (int i = 0; i < visualChildrenCount; i++)
                {
                    IVisual child = visualChildren[i];

                    InnerInvalidateMeasure(child);
                }
            }

            InnerInvalidateMeasure(control);
        }

        /// <summary>
        /// Calculates the min and max height for a control. Ported from WPF.
        /// </summary>
        private readonly struct MinMax
        {
            public MinMax(ILayoutable e)
            {
                MaxHeight = e.MaxHeight;
                MinHeight = e.MinHeight;
                double l = e.Height;

                double height = (double.IsNaN(l) ? double.PositiveInfinity : l);
                MaxHeight = Math.Max(Math.Min(height, MaxHeight), MinHeight);

                height = (double.IsNaN(l) ? 0 : l);
                MinHeight = Math.Max(Math.Min(MaxHeight, height), MinHeight);

                MaxWidth = e.MaxWidth;
                MinWidth = e.MinWidth;
                l = e.Width;

                double width = (double.IsNaN(l) ? double.PositiveInfinity : l);
                MaxWidth = Math.Max(Math.Min(width, MaxWidth), MinWidth);

                width = (double.IsNaN(l) ? 0 : l);
                MinWidth = Math.Max(Math.Min(MaxWidth, width), MinWidth);
            }

            public double MinWidth { get; }
            public double MaxWidth { get; }
            public double MinHeight { get; }
            public double MaxHeight { get; }
        }
    }
}
