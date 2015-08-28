﻿// -----------------------------------------------------------------------
// <copyright file="StackPanel.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using Perspex.Input;

    /// <summary>
    /// Defines vertical or horizontal orientation.
    /// </summary>
    public enum Orientation
    {
        /// <summary>
        /// Vertical orientation.
        /// </summary>
        Vertical,

        /// <summary>
        /// Horizontal orientation.
        /// </summary>
        Horizontal,
    }

    /// <summary>
    /// A panel which lays out its children horizontally or vertically.
    /// </summary>
    public class StackPanel : Panel, INavigableContainer
    {
        /// <summary>
        /// Defines the <see cref="Gap"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> GapProperty =
            PerspexProperty.Register<StackPanel, double>(nameof(Gap));

        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly PerspexProperty<Orientation> OrientationProperty =
            PerspexProperty.Register<StackPanel, Orientation>(nameof(Orientation));

        /// <summary>
        /// Initializes static members of the <see cref="StackPanel"/> class.
        /// </summary>
        static StackPanel()
        {
            AffectsMeasure(GapProperty);
            AffectsMeasure(OrientationProperty);
        }

        /// <summary>
        /// Gets or sets the size of the gap to place between child controls.
        /// </summary>
        public double Gap
        {
            get { return this.GetValue(GapProperty); }
            set { this.SetValue(GapProperty, value); }
        }

        /// <summary>
        /// Gets or sets the orientation in which child controls will be layed out.
        /// </summary>
        public Orientation Orientation
        {
            get { return this.GetValue(OrientationProperty); }
            set { this.SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// Gets the next control in the specified direction.
        /// </summary>
        /// <param name="direction">The movement direction.</param>
        /// <param name="from">The control from which movement begins.</param>
        /// <returns>The control.</returns>
        IInputElement INavigableContainer.GetControl(FocusNavigationDirection direction, IInputElement from)
        {
            var horiz = this.Orientation == Orientation.Horizontal;
            int index = this.Children.IndexOf((IControl)from);

            switch (direction)
            {
                case FocusNavigationDirection.First:
                    index = 0;
                    break;
                case FocusNavigationDirection.Last:
                    index = this.Children.Count - 1;
                    break;
                case FocusNavigationDirection.Next:
                    ++index;
                    break;
                case FocusNavigationDirection.Previous:
                    --index;
                    break;
                case FocusNavigationDirection.Left:
                    index = horiz ? index - 1 : -1;
                    break;
                case FocusNavigationDirection.Right:
                    index = horiz ? index + 1 : -1;
                    break;
                case FocusNavigationDirection.Up:
                    index = horiz ? -1 : index - 1;
                    break;
                case FocusNavigationDirection.Down:
                    index = horiz ? -1 : index + 1;
                    break;
            }

            if (index >= 0 && index < this.Children.Count)
            {
                return this.Children[index];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size.</param>
        /// <returns>The desired size of the control.</returns>
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
                Size size = child.DesiredSize;

                if (this.Orientation == Orientation.Vertical)
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

        /// <summary>
        /// Arranges the control's children.
        /// </summary>
        /// <param name="finalSize">The size allocated to the control.</param>
        /// <returns>The space taken.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            double arrangedWidth = finalSize.Width;
            double arrangedHeight = finalSize.Height;
            double gap = this.Gap;

            if (this.Orientation == Orientation.Vertical)
            {
                arrangedHeight = 0;
            }
            else
            {
                arrangedWidth = 0;
            }

            foreach (Control child in this.Children)
            {
                double childWidth = child.DesiredSize.Width;
                double childHeight = child.DesiredSize.Height;

                if (this.Orientation == Orientation.Vertical)
                {
                    double width = Math.Max(childWidth, arrangedWidth);
                    Rect childFinal = new Rect(0, arrangedHeight, width, childHeight);
                    child.Arrange(childFinal);
                    arrangedWidth = Math.Max(arrangedWidth, childWidth);
                    arrangedHeight += childHeight + gap;
                }
                else
                {
                    double height = Math.Max(childHeight, arrangedHeight);
                    Rect childFinal = new Rect(arrangedWidth, 0, childWidth, height);
                    child.Arrange(childFinal);
                    arrangedWidth += childWidth + gap;
                    arrangedHeight = Math.Max(arrangedHeight, childHeight);
                }
            }

            if (this.Orientation == Orientation.Vertical)
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