// -----------------------------------------------------------------------
// <copyright file="StackPanel.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public enum Orientation
    {
        Vertical,
        Horizontal,
    }

    public class StackPanel : Panel
    {
        public static readonly PerspexProperty<double> GapProperty =
            PerspexProperty.Register<StackPanel, double>("Gap");

        public static readonly PerspexProperty<Orientation> OrientationProperty =
            PerspexProperty.Register<StackPanel, Orientation>("Orientation");

        public double Gap
        {
            get { return this.GetValue(GapProperty); }
            set { this.SetValue(GapProperty, value); }
        }

        public Orientation Orientation
        {
            get { return this.GetValue(OrientationProperty); }
            set { this.SetValue(OrientationProperty, value); }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            double childAvailableWidth = double.PositiveInfinity;
            double childAvailableHeight = double.PositiveInfinity;

            if (this.Orientation == Orientation.Vertical)
            {
                childAvailableWidth = availableSize.Width;

                if (!double.IsNaN(this.Width))
                {
                    childAvailableWidth = this.Width;
                }

                childAvailableWidth = Math.Min(childAvailableWidth, this.MaxWidth);
                childAvailableWidth = Math.Max(childAvailableWidth, this.MinWidth);
            }
            else
            {
                childAvailableHeight = availableSize.Height;

                if (!double.IsNaN(this.Height))
                {
                    childAvailableHeight = this.Height;
                }

                childAvailableHeight = Math.Min(childAvailableHeight, this.MaxHeight);
                childAvailableHeight = Math.Max(childAvailableHeight, this.MinHeight);
            }

            double measuredWidth = 0;
            double measuredHeight = 0;
            double gap = this.Gap;

            foreach (Control child in this.Children)
            {
                child.Measure(new Size(childAvailableWidth, childAvailableHeight));
                Size size = child.DesiredSize.Value;

                if (Orientation == Orientation.Vertical)
                {
                    measuredHeight += size.Height + gap;
                    measuredWidth = Math.Max(measuredWidth, size.Width);
                }
                else
                {
                    measuredWidth += size.Width + gap;
                    measuredHeight = Math.Max(measuredHeight, size.Height);
                }
            }

            return new Size(measuredWidth, measuredHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double arrangedWidth = finalSize.Width;
            double arrangedHeight = finalSize.Height;
            double gap = this.Gap;

            if (Orientation == Orientation.Vertical)
            {
                arrangedHeight = 0;
            }
            else
            {
                arrangedWidth = 0;
            }

            foreach (Control child in this.Children.Where(x => x.DesiredSize.HasValue))
            {
                double childWidth = child.DesiredSize.Value.Width;
                double childHeight = child.DesiredSize.Value.Height;

                if (Orientation == Orientation.Vertical)
                {
                    childWidth = finalSize.Width;

                    Rect childFinal = new Rect(0, arrangedHeight, childWidth, childHeight);

                    if (childFinal.IsEmpty)
                    {
                        child.Arrange(new Rect());
                    }
                    else
                    {
                        child.Arrange(childFinal);
                    }

                    arrangedWidth = Math.Max(arrangedWidth, childWidth);
                    arrangedHeight += childHeight + gap;
                }
                else
                {
                    childHeight = finalSize.Height;
                    Rect childFinal = new Rect(arrangedWidth, 0, childWidth, childHeight);
                    child.Arrange(childFinal);
                    arrangedWidth += childWidth + gap;
                    arrangedHeight = Math.Max(arrangedHeight, childHeight);
                }
            }

            if (Orientation == Orientation.Vertical)
            {
                arrangedHeight = Math.Max(arrangedHeight - gap, finalSize.Height);
            }
            else
            {
                arrangedWidth = Math.Max(arrangedWidth - gap, finalSize.Width);
            }

            return new Size(arrangedWidth, arrangedHeight);
        }
    }
}
