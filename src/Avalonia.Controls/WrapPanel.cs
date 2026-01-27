// This source file is adapted from the Windows Presentation Foundation project. 
// (https://github.com/dotnet/wpf/) 
// 
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Diagnostics;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Utilities;

using static System.Math;

namespace Avalonia.Controls
{
    public enum WrapPanelItemsAlignment
    {
        /// <summary>
        /// Items are laid out so the first one in each column/row touches the top/left of the panel.
        /// </summary>
        Start,

        /// <summary>
        /// Items are laid out so that each column/row is centred vertically/horizontally within the panel.
        /// </summary>
        Center,

        /// <summary>
        /// Items are laid out so the last one in each column/row touches the bottom/right of the panel.
        /// </summary>
        End,

        /// <summary>
        /// Items are laid out with equal spacing between them within each column/row.
        /// </summary>
        /// <remarks>
        /// <see cref="WrapPanel.ItemSpacing"/> is become the minimum spacing between items,
        /// </remarks>
        Justify,

        /// <summary>
        /// Items are laid out with equal spacing between them within each column/row.
        /// </summary>
        /// <remarks>
        /// <see cref="WrapPanel.ItemWidth"/> or <see cref="WrapPanel.ItemHeight"/> is become the minimum spacing between items,
        /// </remarks>
        Stretch
    }

    /// <summary>
    /// Positions child elements in sequential position from left to right, 
    /// breaking content to the next line at the edge of the containing box. 
    /// Subsequent ordering happens sequentially from top to bottom or from right to left, 
    /// depending on the value of the <see cref="Orientation"/> property.
    /// </summary>
    public class WrapPanel : Panel, INavigableContainer, IOrientationBasedMeasures
    {
        /// <summary>
        /// Defines the <see cref="ItemSpacing"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> ItemSpacingProperty =
            AvaloniaProperty.Register<WrapPanel, double>(nameof(ItemSpacing));

        /// <summary>
        /// Defines the <see cref="LineSpacing"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> LineSpacingProperty =
            AvaloniaProperty.Register<WrapPanel, double>(nameof(LineSpacing));

        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly StyledProperty<Orientation> OrientationProperty =
            AvaloniaProperty.Register<WrapPanel, Orientation>(nameof(Orientation), defaultValue: Orientation.Horizontal);

        /// <summary>
        /// Defines the <see cref="ItemsAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<WrapPanelItemsAlignment> ItemsAlignmentProperty =
            AvaloniaProperty.Register<WrapPanel, WrapPanelItemsAlignment>(nameof(ItemsAlignment), defaultValue: WrapPanelItemsAlignment.Start);

        /// <summary>
        /// Defines the <see cref="ItemWidth"/> property.
        /// </summary>
        public static readonly StyledProperty<double> ItemWidthProperty =
            AvaloniaProperty.Register<WrapPanel, double>(nameof(ItemWidth), double.NaN);

        /// <summary>
        /// Defines the <see cref="ItemHeight"/> property.
        /// </summary>
        public static readonly StyledProperty<double> ItemHeightProperty =
            AvaloniaProperty.Register<WrapPanel, double>(nameof(ItemHeight), double.NaN);

        /// <summary>
        /// Initializes static members of the <see cref="WrapPanel"/> class.
        /// </summary>
        static WrapPanel()
        {
            AffectsMeasure<WrapPanel>(ItemSpacingProperty, LineSpacingProperty, OrientationProperty, ItemWidthProperty, ItemHeightProperty);
            AffectsArrange<WrapPanel>(ItemsAlignmentProperty);
        }

        /// <summary>
        /// Gets or sets the spacing between items.
        /// </summary>
        public double ItemSpacing
        {
            get => GetValue(ItemSpacingProperty);
            set => SetValue(ItemSpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the spacing between lines.
        /// </summary>
        public double LineSpacing
        {
            get => GetValue(LineSpacingProperty);
            set => SetValue(LineSpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the orientation in which child controls will be laid out.
        /// </summary>
        public Orientation Orientation
        {
            get => GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        /// <summary>
        /// Gets or sets the alignment of items in the WrapPanel.
        /// </summary>
        public WrapPanelItemsAlignment ItemsAlignment
        {
            get => GetValue(ItemsAlignmentProperty);
            set => SetValue(ItemsAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the width of all items in the WrapPanel.
        /// </summary>
        public double ItemWidth
        {
            get => GetValue(ItemWidthProperty);
            set => SetValue(ItemWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the height of all items in the WrapPanel.
        /// </summary>
        public double ItemHeight
        {
            get => GetValue(ItemHeightProperty);
            set => SetValue(ItemHeightProperty, value);
        }

        private ScrollOrientation ScrollOrientation { get; set; } = ScrollOrientation.Vertical;

        ScrollOrientation IOrientationBasedMeasures.ScrollOrientation => ScrollOrientation;

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == OrientationProperty)
                ScrollOrientation = Orientation is Orientation.Horizontal ?
                    ScrollOrientation.Vertical :
                    ScrollOrientation.Horizontal;
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
            var orientation = Orientation;
            var children = Children;
            bool horiz = orientation == Orientation.Horizontal;
            int index = from is not null ? Children.IndexOf((Control)from) : -1;

            switch (direction)
            {
                case NavigationDirection.First:
                    index = 0;
                    break;
                case NavigationDirection.Last:
                    index = children.Count - 1;
                    break;
                case NavigationDirection.Next:
                    ++index;
                    break;
                case NavigationDirection.Previous:
                    --index;
                    break;
                case NavigationDirection.Left:
                    index = horiz ? index - 1 : -1;
                    break;
                case NavigationDirection.Right:
                    index = horiz ? index + 1 : -1;
                    break;
                case NavigationDirection.Up:
                    index = horiz ? -1 : index - 1;
                    break;
                case NavigationDirection.Down:
                    index = horiz ? -1 : index + 1;
                    break;
            }

            if (index >= 0 && index < children.Count)
            {
                return children[index];
            }

            return null;
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size constraint)
        {
            double itemWidth = ItemWidth;
            double itemHeight = ItemHeight;
            double itemSpacing = ItemSpacing;
            double lineSpacing = LineSpacing;
            var curLineSize = new Size();
            var panelSize = new Size();
            bool itemWidthSet = !double.IsNaN(itemWidth);
            bool itemHeightSet = !double.IsNaN(itemHeight);
            bool itemExists = false;
            bool lineExists = false;
            var itemsAlignment = ItemsAlignment;
            // If we have infinite space on minor axis, we always use Start alignment to avoid strange behavior
            if (this.Minor(constraint) is double.PositiveInfinity)
                itemsAlignment = WrapPanelItemsAlignment.Start;
            // Stretch/Justify need to measure with full constraint on minor axis
            if (itemsAlignment is WrapPanelItemsAlignment.Stretch or WrapPanelItemsAlignment.Justify)
                this.SetMinor(ref panelSize, this.Minor(constraint));

            var childConstraint = new Size(
                itemWidthSet ? itemWidth : constraint.Width,
                itemHeightSet ? itemHeight : constraint.Height);

            foreach (var child in Children)
            {
                // Flow passes its own constraint to children
                child.Measure(childConstraint);

                var childSize = new Size(
                    itemWidthSet ? itemWidth : child.DesiredSize.Width,
                    itemHeightSet ? itemHeight : child.DesiredSize.Height);

                var nextSpacing = itemExists && child.IsVisible ? itemSpacing : 0;
                if (MathUtilities.GreaterThan(this.Minor(curLineSize) + this.Minor(childSize) + nextSpacing, this.Minor(constraint))) // Need to switch to another line
                {
                    panelSize = this.MinorMajorSize(
                        Max(this.Minor(curLineSize), this.Minor(panelSize)),
                        this.Major(panelSize) + this.Major(curLineSize) + (lineExists ? lineSpacing : 0));
                    curLineSize = childSize;

                    itemExists = child.IsVisible;
                    lineExists = true;
                }
                else // Continue to accumulate a line
                {
                    curLineSize = this.MinorMajorSize(
                        this.Minor(curLineSize) + this.Minor(childSize) + nextSpacing,
                        Max(this.Major(childSize), this.Major(curLineSize)));
                    
                    itemExists |= child.IsVisible; // keep true
                }
            }

            // The last line size, if any should be added
            panelSize = this.MinorMajorSize(
                Max(this.Minor(curLineSize), this.Minor(panelSize)),
                this.Major(panelSize) + this.Major(curLineSize) + (lineExists ? lineSpacing : 0));

            return panelSize;
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            double itemWidth = ItemWidth;
            double itemHeight = ItemHeight;
            double itemSpacing = ItemSpacing;
            double lineSpacing = LineSpacing;
            var children = Children;
            int firstInLine = 0;
            double accumulatedMajor = 0;
            double itemMinor = this.Minor(itemWidth, itemHeight);
            var curLineSize = new Size();
            bool itemWidthSet = !double.IsNaN(itemWidth);
            bool itemHeightSet = !double.IsNaN(itemHeight);
            bool itemExists = false;
            bool lineExists = false;
            var itemsAlignment = ItemsAlignment;
            // If we have infinite space on minor axis, we always use Start alignment to avoid strange behavior
            if (this.Minor(finalSize) is double.PositiveInfinity)
                itemsAlignment = WrapPanelItemsAlignment.Start;

            for (int i = 0; i < children.Count; ++i)
            {
                var child = children[i];
                var childSize = new Size(
                    itemWidthSet ? itemWidth : child.DesiredSize.Width,
                    itemHeightSet ? itemHeight : child.DesiredSize.Height);

                var nextSpacing = itemExists && child.IsVisible ? itemSpacing : 0;
                if (MathUtilities.GreaterThan(this.Minor(curLineSize) + this.Minor(childSize) + nextSpacing, this.Minor(finalSize))) // Need to switch to another line
                {
                    accumulatedMajor += lineExists ? lineSpacing : 0; // add spacing to arrange line first
                    ArrangeLine(this.Major(curLineSize), firstInLine, i);
                    accumulatedMajor += this.Major(curLineSize); // add the height of the line just arranged
                    curLineSize = childSize;

                    firstInLine = i;

                    itemExists = child.IsVisible;
                    lineExists = true;
                }
                else // Continue to accumulate a line
                {
                    curLineSize = this.MinorMajorSize(
                        this.Minor(curLineSize) + this.Minor(childSize) + nextSpacing,
                        Max(this.Major(childSize), this.Major(curLineSize)));

                    itemExists |= child.IsVisible; // keep true
                }
            }

            // Arrange the last line, if any
            if (firstInLine < children.Count)
            {
                accumulatedMajor += lineExists ? lineSpacing : 0; // add spacing to arrange line first
                ArrangeLine(this.Major(curLineSize), firstInLine, children.Count);
            }

            return finalSize;

            void ArrangeLine(double lineMajor, int start, int endExcluded)
            {
                bool useItemMinor = this.Minor(itemWidthSet, itemHeightSet);
                double minorStart = 0d;
                var minorSpacing = itemSpacing;
                // Count of spacings between items
                var minorSpacingCount = -1;
                double totalMinor = 0d;
                double minorStretchRatio = 1d;

                if (itemsAlignment is not WrapPanelItemsAlignment.Start)
                {
                    for (int i = start; i < endExcluded; ++i)
                    {
                        totalMinor += GetChildMinor(i);
                        if (children[i].IsVisible)
                            ++minorSpacingCount;
                    }
                }

                Debug.Assert(this.Minor(finalSize) >= totalMinor + minorSpacing * minorSpacingCount);

                switch (itemsAlignment)
                {
                    case WrapPanelItemsAlignment.Start:
                        break;
                    case WrapPanelItemsAlignment.Center:
                        totalMinor += minorSpacing * minorSpacingCount;
                        minorStart = (this.Minor(finalSize) - totalMinor) / 2;
                        break;
                    case WrapPanelItemsAlignment.End:
                        totalMinor += minorSpacing * minorSpacingCount;
                        minorStart = this.Minor(finalSize) - totalMinor;
                        break;
                    case WrapPanelItemsAlignment.Justify:
                        var totalMinorSpacing = this.Minor(finalSize) - totalMinor - 0.01; // small epsilon to avoid rounding issues
                        if (minorSpacingCount > 0)
                            minorSpacing = totalMinorSpacing / minorSpacingCount;
                        break;
                    case WrapPanelItemsAlignment.Stretch:
                        var finalMinorWithoutSpacing = this.Minor(finalSize) - minorSpacing * minorSpacingCount - 0.01; // small epsilon to avoid rounding issues
                        minorStretchRatio = finalMinorWithoutSpacing / totalMinor;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(itemsAlignment), itemsAlignment, null);
                }

                for (int i = start; i < endExcluded; ++i)
                {
                    double layoutSlotMinor = GetChildMinor(i) * minorStretchRatio;
                    children[i].Arrange(this.MinorMajorRect(minorStart, accumulatedMajor, layoutSlotMinor, lineMajor));
                    minorStart += layoutSlotMinor + (children[i].IsVisible ? minorSpacing : 0);
                }

                return;
                double GetChildMinor(int i) => useItemMinor ? itemMinor : this.Minor(children[i].DesiredSize);
            }
        }
    }
}
