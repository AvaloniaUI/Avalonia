// -----------------------------------------------------------------------
// <copyright file="StackPanel.cs" company="Tricycle">
// Copyright 2014 Tricycle. All rights reserved.
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
        Horizontal,
        Vertical,
    }

    public class StackPanel : Panel
    {
        public static readonly PerspexProperty<Orientation> OrientationProperty =
            PerspexProperty.Register<StackPanel, Orientation>("Orientation");

        public Orientation Orientation
        {
            get { return this.GetValue(OrientationProperty); }
            set { this.SetValue(OrientationProperty, value); }
        }

        protected override Size MeasureContent(Size availableSize)
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

            foreach (Control child in this.Children)
            {
                child.Measure(new Size(childAvailableWidth, childAvailableHeight));
                Size size = child.DesiredSize.Value;

                if (Orientation == Orientation.Vertical)
                {
                    measuredHeight += size.Height;
                    measuredWidth = Math.Max(measuredWidth, size.Width);
                }
                else
                {
                    measuredWidth += size.Width;
                    measuredHeight = Math.Max(measuredHeight, size.Height);
                }
            }

            return new Size(measuredWidth, measuredHeight);
        }

        protected override Size ArrangeContent(Size finalSize)
        {
            double arrangedWidth = finalSize.Width;
            double arrangedHeight = finalSize.Height;

            if (Orientation == Orientation.Vertical)
            {
                arrangedHeight = 0;
            }
            else
            {
                arrangedWidth = 0;
            }

            foreach (Control child in this.Children)
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
                    arrangedHeight += childHeight;
                }
                else
                {
                    childHeight = finalSize.Height;

                    Rect childFinal = new Rect(arrangedWidth, 0, childWidth, childHeight);

                    if (childFinal.IsEmpty)
                    {
                        child.Arrange(new Rect());
                    }
                    else
                    {
                        child.Arrange(childFinal);
                    }

                    arrangedWidth += childWidth;
                    arrangedHeight = Math.Max(arrangedHeight, childHeight);
                }
            }

            if (Orientation == Orientation.Vertical)
            {
                arrangedHeight = Math.Max(arrangedHeight, finalSize.Height);
            }
            else
            {
                arrangedWidth = Math.Max(arrangedWidth, finalSize.Width);
            }

            return new Size(arrangedWidth, arrangedHeight);
        }
    }
}
