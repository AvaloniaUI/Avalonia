// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Input;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    /// <summary>
    /// A panel which lays out its children horizontally or vertically.
    /// </summary>
    public class StackPanel : Panel, INavigableContainer
    {
        /// <summary>
        /// Defines the <see cref="Spacing"/> property.
        /// </summary>
        public static readonly StyledProperty<double> SpacingProperty =
            AvaloniaProperty.Register<StackPanel, double>(nameof(Spacing));

        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly StyledProperty<Orientation> OrientationProperty =
            AvaloniaProperty.Register<StackPanel, Orientation>(nameof(Orientation), Orientation.Vertical);

        /// <summary>
        /// Initializes static members of the <see cref="StackPanel"/> class.
        /// </summary>
        static StackPanel()
        {
            AffectsMeasure<StackPanel>(SpacingProperty);
            AffectsMeasure<StackPanel>(OrientationProperty);
        }

        /// <summary>
        /// Gets or sets the size of the spacing to place between child controls.
        /// </summary>
        public double Spacing
        {
            get { return GetValue(SpacingProperty); }
            set { SetValue(SpacingProperty, value); }
        }

        /// <summary>
        /// Gets or sets the orientation in which child controls will be layed out.
        /// </summary>
        public Orientation Orientation
        {
            get { return GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// Gets the next control in the specified direction.
        /// </summary>
        /// <param name="direction">The movement direction.</param>
        /// <param name="from">The control from which movement begins.</param>
        /// <param name="wrap">Whether to wrap around when the first or last item is reached.</param>
        /// <returns>The control.</returns>
        IInputElement INavigableContainer.GetControl(NavigationDirection direction, IInputElement from, bool wrap)
        {
            var result = GetControlInDirection(direction, from as IControl);

            if (result == null && wrap)
            {
                if (Orientation == Orientation.Vertical)
                {
                    switch (direction)
                    {
                        case NavigationDirection.Up:
                        case NavigationDirection.Previous:
                        case NavigationDirection.PageUp:
                            result = GetControlInDirection(NavigationDirection.Last, null);
                            break;
                        case NavigationDirection.Down:
                        case NavigationDirection.Next:
                        case NavigationDirection.PageDown:
                            result = GetControlInDirection(NavigationDirection.First, null);
                            break;
                    }
                }
                else
                {
                    switch (direction)
                    {
                        case NavigationDirection.Left:
                        case NavigationDirection.Previous:
                        case NavigationDirection.PageUp:
                            result = GetControlInDirection(NavigationDirection.Last, null);
                            break;
                        case NavigationDirection.Right:
                        case NavigationDirection.Next:
                        case NavigationDirection.PageDown:
                            result = GetControlInDirection(NavigationDirection.First, null);
                            break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the next control in the specified direction.
        /// </summary>
        /// <param name="direction">The movement direction.</param>
        /// <param name="from">The control from which movement begins.</param>
        /// <returns>The control.</returns>
        protected virtual IInputElement GetControlInDirection(NavigationDirection direction, IControl from)
        {
            var horiz = Orientation == Orientation.Horizontal;
            int index = from != null ? Children.IndexOf(from) : -1;

            switch (direction)
            {
                case NavigationDirection.First:
                    index = 0;
                    break;
                case NavigationDirection.Last:
                    index = Children.Count - 1;
                    break;
                case NavigationDirection.Next:
                    if (index != -1) ++index;
                    break;
                case NavigationDirection.Previous:
                    if (index != -1) --index;
                    break;
                case NavigationDirection.Left:
                    if (index != -1) index = horiz ? index - 1 : -1;
                    break;
                case NavigationDirection.Right:
                    if (index != -1) index = horiz ? index + 1 : -1;
                    break;
                case NavigationDirection.Up:
                    if (index != -1) index = horiz ? -1 : index - 1;
                    break;
                case NavigationDirection.Down:
                    if (index != -1) index = horiz ? -1 : index + 1;
                    break;
                default:
                    index = -1;
                    break;
            }

            if (index >= 0 && index < Children.Count)
            {
                return Children[index];
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
            double childAvailableWidth = availableSize.Width;
            double childAvailableHeight = availableSize.Height;

            if (Orientation == Orientation.Vertical)
            {
                childAvailableWidth = availableSize.Width;

                if (!double.IsNaN(Width))
                {
                    childAvailableWidth = Width;
                }

                childAvailableWidth = Math.Min(childAvailableWidth, MaxWidth);
                childAvailableWidth = Math.Max(childAvailableWidth, MinWidth);
            }
            else
            {
                childAvailableHeight = availableSize.Height;

                if (!double.IsNaN(Height))
                {
                    childAvailableHeight = Height;
                }

                childAvailableHeight = Math.Min(childAvailableHeight, MaxHeight);
                childAvailableHeight = Math.Max(childAvailableHeight, MinHeight);
            }

            double measuredWidth = 0;
            double measuredHeight = 0;
            double spacing = Spacing;
            bool hasVisibleChild = Children.Any(c => c.IsVisible);

            foreach (Control child in Children)
            {
                child.Measure(new Size(childAvailableWidth, childAvailableHeight));
                Size size = child.DesiredSize;

                if (Orientation == Orientation.Vertical)
                {
                    measuredHeight += size.Height + (child.IsVisible ? spacing : 0);
                    measuredWidth = Math.Max(measuredWidth, size.Width);
                }
                else
                {
                    measuredWidth += size.Width + (child.IsVisible ? spacing : 0);   
                    measuredHeight = Math.Max(measuredHeight, size.Height);
                }
            }

            if (Orientation == Orientation.Vertical)
            {
                measuredHeight -= (hasVisibleChild ? spacing : 0);
            }
            else
            {
                measuredWidth -= (hasVisibleChild ? spacing : 0);
            }

            return new Size(measuredWidth, measuredHeight).Constrain(availableSize);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var orientation = Orientation;
            var spacing = Spacing;
            var finalRect = new Rect(finalSize);
            var pos = 0.0;

            foreach (Control child in Children)
            {
                if (!child.IsVisible)
                {
                    continue;
                }

                double childWidth = child.DesiredSize.Width;
                double childHeight = child.DesiredSize.Height;

                if (orientation == Orientation.Vertical)
                {
                    var rect = new Rect(0, pos, childWidth, childHeight)
                        .Align(finalRect, child.HorizontalAlignment, VerticalAlignment.Top);
                    ArrangeChild(child, rect, finalSize, orientation);
                    pos += childHeight + spacing;
                }
                else
                {
                    var rect = new Rect(pos, 0, childWidth, childHeight)
                        .Align(finalRect, HorizontalAlignment.Left, child.VerticalAlignment);
                    ArrangeChild(child, rect, finalSize, orientation);
                    pos += childWidth + spacing;
                }
            }

            return finalSize;
        }

        internal virtual void ArrangeChild(
            IControl child,
            Rect rect,
            Size panelSize,
            Orientation orientation)
        {
            child.Arrange(rect);
        }
    }
}
