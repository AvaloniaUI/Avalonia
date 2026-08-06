// This source file is adapted from the Windows Presentation Foundation project. 
// (https://github.com/dotnet/wpf/) 
// 
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
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
        /// <see cref="WrapPanel.ItemSpacing"/> becomes the minimum spacing between items.
        /// </remarks>
        Justify,

        /// <summary>
        /// Items are stretched evenly to fill the entire height/width of each column/row (last column/row excluded).
        /// </summary>
        /// <remarks>
        /// <see cref="WrapPanel.ItemWidth"/> or <see cref="WrapPanel.ItemHeight"/> becomes the minimum size of items.
        /// </remarks>
        Stretch,

        /// <summary>
        /// Items are stretched evenly to fill the entire height/width of each column/row.
        /// </summary>
        /// <remarks>
        /// <see cref="WrapPanel.ItemWidth"/> or <see cref="WrapPanel.ItemHeight"/> becomes the minimum size of items.
        /// </remarks>
        StretchAll
    }

    /// <summary>
    /// Positions child elements in sequential position from left to right, 
    /// breaking content to the next line at the edge of the containing box. 
    /// Subsequent ordering happens sequentially from top to bottom or from right to left, 
    /// depending on the value of the <see cref="Orientation"/> property.
    /// </summary>
    public class WrapPanel : Panel, INavigableContainer
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
            var orientation = Orientation;
            var children = Children;
            var curLineSize = new UVSize(orientation);
            var panelSize = new UVSize(orientation);
            var uvConstraint = new UVSize(orientation, constraint.Width, constraint.Height);
            bool itemWidthSet = !double.IsNaN(itemWidth);
            bool itemHeightSet = !double.IsNaN(itemHeight);
            bool itemExists = false;
            bool lineExists = false;
            var itemsAlignment = ItemsAlignment;
            // If we have infinite space on the U axis, we always use Start alignment to avoid strange behavior
            if (uvConstraint.U is double.PositiveInfinity)
                itemsAlignment = WrapPanelItemsAlignment.Start;
            // Justify/StretchAll need to measure with the full constraint on the U axis
            if (itemsAlignment is WrapPanelItemsAlignment.Justify or WrapPanelItemsAlignment.StretchAll)
                panelSize.U = uvConstraint.U;

            var childConstraint = new Size(
                itemWidthSet ? itemWidth : constraint.Width,
                itemHeightSet ? itemHeight : constraint.Height);

            for (int i = 0, count = children.Count; i < count; ++i)
            {
                var child = children[i];
                // Flow passes its own constraint to children
                child.Measure(childConstraint);

                var childSize = new UVSize(orientation,
                    itemWidthSet ? itemWidth : child.DesiredSize.Width,
                    itemHeightSet ? itemHeight : child.DesiredSize.Height);

                var nextSpacing = itemExists && child.IsVisible ? itemSpacing : 0;
                if (MathUtilities.GreaterThan(curLineSize.U + childSize.U + nextSpacing, uvConstraint.U)) // Need to switch to another line
                {
                    panelSize.U = Max(curLineSize.U, panelSize.U);
                    panelSize.V += curLineSize.V + (lineExists ? lineSpacing : 0);
                    curLineSize = childSize;

                    itemExists = child.IsVisible;
                    lineExists = true;
                }
                else // Continue to accumulate a line
                {
                    curLineSize.U += childSize.U + nextSpacing;
                    curLineSize.V = Max(childSize.V, curLineSize.V);

                    itemExists |= child.IsVisible; // keep true
                }
            }

            // Stretch needs to measure with the full constraint on the U axis if there are multiple lines
            if (lineExists && itemsAlignment is WrapPanelItemsAlignment.Stretch)
                panelSize.U = uvConstraint.U;

            // The last line size, if any should be added
            panelSize.U = Max(curLineSize.U, panelSize.U);
            panelSize.V += curLineSize.V + (lineExists ? lineSpacing : 0);

            return new Size(panelSize.Width, panelSize.Height);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            double itemWidth = ItemWidth;
            double itemHeight = ItemHeight;
            double itemSpacing = ItemSpacing;
            double lineSpacing = LineSpacing;
            var orientation = Orientation;
            bool isHorizontal = orientation == Orientation.Horizontal;
            var children = Children;
            int firstInLine = 0;
            double accumulatedV = 0;
            double itemU = isHorizontal ? itemWidth : itemHeight;
            var curLineSize = new UVSize(orientation);
            var uvFinalSize = new UVSize(orientation, finalSize.Width, finalSize.Height);
            bool itemWidthSet = !double.IsNaN(itemWidth);
            bool itemHeightSet = !double.IsNaN(itemHeight);
            bool itemExists = false;
            bool lineExists = false;
            var itemsAlignment = ItemsAlignment;
            // If we have infinite space on the U axis, we always use Start alignment to avoid strange behavior
            if (uvFinalSize.U is double.PositiveInfinity)
                itemsAlignment = WrapPanelItemsAlignment.Start;

            for (int i = 0; i < children.Count; ++i)
            {
                var child = children[i];
                var childSize = new UVSize(orientation,
                    itemWidthSet ? itemWidth : child.DesiredSize.Width,
                    itemHeightSet ? itemHeight : child.DesiredSize.Height);

                var nextSpacing = itemExists && child.IsVisible ? itemSpacing : 0;
                if (MathUtilities.GreaterThan(curLineSize.U + childSize.U + nextSpacing, uvFinalSize.U)) // Need to switch to another line
                {
                    accumulatedV += lineExists ? lineSpacing : 0; // add spacing to arrange line first
                    ArrangeLine(curLineSize.V, firstInLine, i);
                    accumulatedV += curLineSize.V; // add the height of the line just arranged
                    curLineSize = childSize;

                    firstInLine = i;

                    itemExists = child.IsVisible;
                    lineExists = true;
                }
                else // Continue to accumulate a line
                {
                    curLineSize.U += childSize.U + nextSpacing;
                    curLineSize.V = Max(childSize.V, curLineSize.V);

                    itemExists |= child.IsVisible; // keep true
                }
            }

            // Arrange the last line, if any
            if (firstInLine < children.Count)
            {
                accumulatedV += lineExists ? lineSpacing : 0; // add spacing to arrange line first
                ArrangeLine(curLineSize.V, firstInLine, children.Count);
            }

            return finalSize;

            void ArrangeLine(double lineV, int start, int endExcluded)
            {
                bool useItemU = isHorizontal ? itemWidthSet : itemHeightSet;
                double u = 0d;
                var spacing = itemSpacing;
                // Count of spacings between items
                var spacingCount = -1;
                double totalU = 0d;
                double stretchRatio = 1d;
                var lineItemsAlignment = itemsAlignment;
                if (itemsAlignment is WrapPanelItemsAlignment.Stretch && endExcluded == children.Count)
                    // Don't stretch the last line
                    lineItemsAlignment = WrapPanelItemsAlignment.Start;

                if (lineItemsAlignment is not WrapPanelItemsAlignment.Start)
                {
                    for (int i = start; i < endExcluded; ++i)
                    {
                        totalU += GetChildU(i);
                        if (children[i].IsVisible)
                            ++spacingCount;
                    }
                }

                switch (lineItemsAlignment)
                {
                    case WrapPanelItemsAlignment.Start:
                        break;
                    case WrapPanelItemsAlignment.Center:
                        totalU += spacing * spacingCount;
                        u = (uvFinalSize.U - totalU) / 2;
                        break;
                    case WrapPanelItemsAlignment.End:
                        totalU += spacing * spacingCount;
                        u = uvFinalSize.U - totalU;
                        break;
                    case WrapPanelItemsAlignment.Justify:
                        var totalSpacing = Max(uvFinalSize.U - totalU, 0);
                        if (spacingCount > 0)
                            spacing = totalSpacing / spacingCount;
                        break;
                    case WrapPanelItemsAlignment.Stretch or WrapPanelItemsAlignment.StretchAll:
                        if (!MathUtilities.IsZero(totalU))
                        {
                            var finalUWithoutSpacing = Max(uvFinalSize.U - spacing * spacingCount, 0);
                            stretchRatio = finalUWithoutSpacing / totalU;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(ItemsAlignment), itemsAlignment, null);
                }

                for (int i = start; i < endExcluded; ++i)
                {
                    double layoutSlotU = GetChildU(i) * stretchRatio;
                    children[i].Arrange(isHorizontal ?
                        new Rect(u, accumulatedV, layoutSlotU, lineV) :
                        new Rect(accumulatedV, u, lineV, layoutSlotU));
                    u += layoutSlotU + (children[i].IsVisible ? spacing : 0);
                }

                return;
                double GetChildU(int i) => useItemU ? itemU :
                    isHorizontal ? children[i].DesiredSize.Width : children[i].DesiredSize.Height;
            }
        }

        private struct UVSize
        {
            internal UVSize(Orientation orientation, double width, double height)
            {
                U = V = 0d;
                _orientation = orientation;
                Width = width;
                Height = height;
            }

            internal UVSize(Orientation orientation)
            {
                U = V = 0d;
                _orientation = orientation;
            }

            internal double U;
            internal double V;
            private Orientation _orientation;

            internal double Width
            {
                get => _orientation == Orientation.Horizontal ? U : V;
                set { if (_orientation == Orientation.Horizontal) U = value; else V = value; }
            }

            internal double Height
            {
                get => _orientation == Orientation.Horizontal ? V : U;
                set { if (_orientation == Orientation.Horizontal) V = value; else U = value; }
            }
        }

    }
}
