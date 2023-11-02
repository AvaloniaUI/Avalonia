// This source file is adapted from the Windows Presentation Foundation project. 
// (https://github.com/dotnet/wpf/) 
// 
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    /// <summary>
    /// A panel which lays out its children horizontally or vertically.
    /// </summary>
    public class StackPanel : Panel, INavigableContainer, IScrollSnapPointsInfo
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
        /// Defines the <see cref="AreHorizontalSnapPointsRegular"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> AreHorizontalSnapPointsRegularProperty =
            AvaloniaProperty.Register<StackPanel, bool>(nameof(AreHorizontalSnapPointsRegular));

        /// <summary>
        /// Defines the <see cref="AreVerticalSnapPointsRegular"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> AreVerticalSnapPointsRegularProperty =
            AvaloniaProperty.Register<StackPanel, bool>(nameof(AreVerticalSnapPointsRegular));

        /// <summary>
        /// Defines the <see cref="HorizontalSnapPointsChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> HorizontalSnapPointsChangedEvent =
            RoutedEvent.Register<StackPanel, RoutedEventArgs>(
                nameof(HorizontalSnapPointsChanged),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="VerticalSnapPointsChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> VerticalSnapPointsChangedEvent =
            RoutedEvent.Register<StackPanel, RoutedEventArgs>(
                nameof(VerticalSnapPointsChanged),
                RoutingStrategies.Bubble);

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
            get => GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the orientation in which child controls will be layed out.
        /// </summary>
        public Orientation Orientation
        {
            get => GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        /// <summary>
        /// Occurs when the measurements for horizontal snap points change.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? HorizontalSnapPointsChanged
        {
            add => AddHandler(HorizontalSnapPointsChangedEvent, value);
            remove => RemoveHandler(HorizontalSnapPointsChangedEvent, value);
        }

        /// <summary>
        /// Occurs when the measurements for vertical snap points change.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? VerticalSnapPointsChanged
        {
            add => AddHandler(VerticalSnapPointsChangedEvent, value);
            remove => RemoveHandler(VerticalSnapPointsChangedEvent, value);
        }

        /// <summary>
        /// Gets or sets whether the horizontal snap points for the <see cref="StackPanel"/> are equidistant from each other.
        /// </summary>
        public bool AreHorizontalSnapPointsRegular
        {
            get => GetValue(AreHorizontalSnapPointsRegularProperty);
            set => SetValue(AreHorizontalSnapPointsRegularProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the vertical snap points for the <see cref="StackPanel"/> are equidistant from each other.
        /// </summary>
        public bool AreVerticalSnapPointsRegular
        {
            get => GetValue(AreVerticalSnapPointsRegularProperty);
            set => SetValue(AreVerticalSnapPointsRegularProperty, value);
        }

        /// <summary>
        /// Gets the next control in the specified direction.
        /// </summary>
        /// <param name="direction">The movement direction.</param>
        /// <param name="from">The control from which movement begins.</param>
        /// <param name="wrap">Whether to wrap around when the first or last item is reached.</param>
        /// <returns>The control.</returns>
        IInputElement? INavigableContainer.GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
        {
            var result = GetControlInDirection(direction, from as Control);

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
        protected virtual IInputElement? GetControlInDirection(NavigationDirection direction, Control? from)
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
                    ++index;
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
        /// General StackPanel layout behavior is to grow unbounded in the "stacking" direction (Size To Content).
        /// Children in this dimension are encouraged to be as large as they like.  In the other dimension,
        /// StackPanel will assume the maximum size of its children.
        /// </summary>
        /// <param name="availableSize">Constraint</param>
        /// <returns>Desired size</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            Size stackDesiredSize = new Size();
            var children = Children;
            Size layoutSlotSize = availableSize;
            bool fHorizontal = (Orientation == Orientation.Horizontal);
            double spacing = Spacing;
            bool hasVisibleChild = false;

            //
            // Initialize child sizing and iterator data
            // Allow children as much size as they want along the stack.
            //
            if (fHorizontal)
            {
                layoutSlotSize = layoutSlotSize.WithWidth(Double.PositiveInfinity);
            }
            else
            {
                layoutSlotSize = layoutSlotSize.WithHeight(Double.PositiveInfinity);
            }

            //
            //  Iterate through children.
            //  While we still supported virtualization, this was hidden in a child iterator (see source history).
            //
            for (int i = 0, count = children.Count; i < count; ++i)
            {
                // Get next child.
                var child = children[i];

                bool isVisible = child.IsVisible;

                if (isVisible && !hasVisibleChild)
                {
                    hasVisibleChild = true;
                }

                // Measure the child.
                child.Measure(layoutSlotSize);
                Size childDesiredSize = child.DesiredSize;

                // Accumulate child size.
                if (fHorizontal)
                {
                    stackDesiredSize = stackDesiredSize.WithWidth(stackDesiredSize.Width + (isVisible ? spacing : 0) + childDesiredSize.Width);
                    stackDesiredSize = stackDesiredSize.WithHeight(Math.Max(stackDesiredSize.Height, childDesiredSize.Height));
                }
                else
                {
                    stackDesiredSize = stackDesiredSize.WithWidth(Math.Max(stackDesiredSize.Width, childDesiredSize.Width));
                    stackDesiredSize = stackDesiredSize.WithHeight(stackDesiredSize.Height + (isVisible ? spacing : 0) + childDesiredSize.Height);
                }
            }

            if (fHorizontal)
            {
                stackDesiredSize = stackDesiredSize.WithWidth(stackDesiredSize.Width - (hasVisibleChild ? spacing : 0));
            }
            else
            { 
                stackDesiredSize = stackDesiredSize.WithHeight(stackDesiredSize.Height - (hasVisibleChild ? spacing : 0));
            }

            return stackDesiredSize;
        }

        /// <summary>
        /// Content arrangement.
        /// </summary>
        /// <param name="finalSize">Arrange size</param>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var children = Children;
            bool fHorizontal = (Orientation == Orientation.Horizontal);
            Rect rcChild = new Rect(finalSize);
            double previousChildSize = 0.0;
            var spacing = Spacing;

            //
            // Arrange and Position Children.
            //
            for (int i = 0, count = children.Count; i < count; ++i)
            {
                var child = children[i];

                if (!child.IsVisible)
                {
                    continue;
                }

                if (fHorizontal)
                {
                    rcChild = rcChild.WithX(rcChild.X + previousChildSize);
                    previousChildSize = child.DesiredSize.Width;
                    rcChild = rcChild.WithWidth(previousChildSize);
                    rcChild = rcChild.WithHeight(Math.Max(finalSize.Height, child.DesiredSize.Height));
                    previousChildSize += spacing;
                }
                else
                {
                    rcChild = rcChild.WithY(rcChild.Y + previousChildSize);
                    previousChildSize = child.DesiredSize.Height;
                    rcChild = rcChild.WithHeight(previousChildSize);
                    rcChild = rcChild.WithWidth(Math.Max(finalSize.Width, child.DesiredSize.Width));
                    previousChildSize += spacing;
                }

                ArrangeChild(child, rcChild, finalSize, Orientation);
            }

            RaiseEvent(new RoutedEventArgs(Orientation == Orientation.Horizontal ? HorizontalSnapPointsChangedEvent : VerticalSnapPointsChangedEvent));

            return finalSize;
        }

        internal virtual void ArrangeChild(
            Control child,
            Rect rect,
            Size panelSize,
            Orientation orientation)
        {
            child.Arrange(rect);
        }

        /// <inheritdoc/>
        public IReadOnlyList<double> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment)
        {
            var snapPoints = new List<double>();

            switch (orientation)
            {
                case Orientation.Horizontal:
                    if (AreHorizontalSnapPointsRegular)
                        throw new InvalidOperationException();
                    if (Orientation == Orientation.Horizontal)
                    {
                        foreach(var child in VisualChildren)
                        {
                            double snapPoint = 0;

                            switch (snapPointsAlignment)
                            {
                                case SnapPointsAlignment.Near:
                                    snapPoint = child.Bounds.Left;
                                    break;
                                case SnapPointsAlignment.Center:
                                    snapPoint = child.Bounds.Center.X;
                                    break;
                                case SnapPointsAlignment.Far:
                                    snapPoint = child.Bounds.Right;
                                    break;
                            }

                            snapPoints.Add(snapPoint);
                        }
                    }
                    break;
                case Orientation.Vertical:
                    if (AreVerticalSnapPointsRegular)
                        throw new InvalidOperationException();
                    if (Orientation == Orientation.Vertical)
                    {
                        foreach (var child in VisualChildren)
                        {
                            double snapPoint = 0;

                            switch (snapPointsAlignment)
                            {
                                case SnapPointsAlignment.Near:
                                    snapPoint = child.Bounds.Top;
                                    break;
                                case SnapPointsAlignment.Center:
                                    snapPoint = child.Bounds.Center.Y;
                                    break;
                                case SnapPointsAlignment.Far:
                                    snapPoint = child.Bounds.Bottom;
                                    break;
                            }

                            snapPoints.Add(snapPoint);
                        }
                    }
                    break;
            }

            return snapPoints;
        }

        /// <inheritdoc/>
        public double GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment, out double offset)
        {
            offset = 0f;
            var firstChild = VisualChildren.FirstOrDefault();

            if(firstChild == null)
            {
                return 0;
            }

            double snapPoint = 0;

            switch (Orientation)
            {
                case Orientation.Horizontal:
                    if (!AreHorizontalSnapPointsRegular)
                        throw new InvalidOperationException();

                    snapPoint = firstChild.Bounds.Width;
                    switch (snapPointsAlignment)
                    {
                        case SnapPointsAlignment.Near:
                            offset = firstChild.Bounds.Left;
                            break;
                        case SnapPointsAlignment.Center:
                            offset = firstChild.Bounds.Center.X;
                            break;
                        case SnapPointsAlignment.Far:
                            offset = firstChild.Bounds.Right;
                            break;
                    }
                    break;
                case Orientation.Vertical:
                    if (!AreVerticalSnapPointsRegular)
                        throw new InvalidOperationException();
                    snapPoint = firstChild.Bounds.Height;
                    switch (snapPointsAlignment)
                    {
                        case SnapPointsAlignment.Near:
                            offset = firstChild.Bounds.Top;
                            break;
                        case SnapPointsAlignment.Center:
                            offset = firstChild.Bounds.Center.Y;
                            break;
                        case SnapPointsAlignment.Far:
                            offset = firstChild.Bounds.Bottom;
                            break;
                    }
                    break;
            }

            return snapPoint + Spacing;
        }
    }
}
