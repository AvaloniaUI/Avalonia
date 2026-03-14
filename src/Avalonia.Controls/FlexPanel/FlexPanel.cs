using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Layout;

namespace Avalonia.Controls
{

    /// <summary>
    /// A panel that arranges child controls using CSS FlexBox principles.
    /// It organizes child items in one or more lines along a main-axis (either row or column)
    /// and provides advanced control over their sizing and layout.
    /// </summary>
    /// <remarks>
    /// See CSS FlexBox specification: https://www.w3.org/TR/css-flexbox-1
    /// </remarks>
    public sealed class FlexPanel : Panel
    {
        private static readonly Func<Layoutable, int> s_getOrder = x => x is { } y ? Flex.GetOrder(y) : 0;
        private static readonly Func<Layoutable, bool> s_isVisible = x => x.IsVisible;

        /// <summary>
        /// Defines the <see cref="Direction"/> property.
        /// </summary>
        public static readonly StyledProperty<FlexDirection> DirectionProperty =
            AvaloniaProperty.Register<FlexPanel, FlexDirection>(nameof(Direction));

        /// <summary>
        /// Defines the <see cref="JustifyContent"/> property.
        /// </summary>
        public static readonly StyledProperty<JustifyContent> JustifyContentProperty =
            AvaloniaProperty.Register<FlexPanel, JustifyContent>(nameof(JustifyContent));

        /// <summary>
        /// Defines the <see cref="AlignItems"/> property.
        /// </summary>
        public static readonly StyledProperty<AlignItems> AlignItemsProperty =
            AvaloniaProperty.Register<FlexPanel, AlignItems>(nameof(AlignItems), AlignItems.Stretch);

        /// <summary>
        /// Defines the <see cref="AlignContent"/> property.
        /// </summary>
        public static readonly StyledProperty<AlignContent> AlignContentProperty =
            AvaloniaProperty.Register<FlexPanel, AlignContent>(nameof(AlignContent), AlignContent.Stretch);

        /// <summary>
        /// Defines the <see cref="Wrap"/> property.
        /// </summary>
        public static readonly StyledProperty<FlexWrap> WrapProperty =
            AvaloniaProperty.Register<FlexPanel, FlexWrap>(nameof(Wrap));

        /// <summary>
        /// Defines the <see cref="ColumnSpacing"/> property.
        /// </summary>
        public static readonly StyledProperty<double> ColumnSpacingProperty =
            AvaloniaProperty.Register<FlexPanel, double>(nameof(ColumnSpacing));

        /// <summary>
        /// Defines the <see cref="RowSpacing"/> property.
        /// </summary>
        public static readonly StyledProperty<double> RowSpacingProperty =
            AvaloniaProperty.Register<FlexPanel, double>(nameof(RowSpacing));

        private FlexLayoutState? _state;

        static FlexPanel()
        {
            AffectsMeasure<FlexPanel>(
                DirectionProperty,
                JustifyContentProperty,
                WrapProperty,
                ColumnSpacingProperty,
                RowSpacingProperty);

            AffectsArrange<FlexPanel>(
                AlignItemsProperty,
                AlignContentProperty);

            AffectsParentMeasure<FlexPanel>(
                HorizontalAlignmentProperty,
                VerticalAlignmentProperty,
                Flex.OrderProperty,
                Flex.BasisProperty,
                Flex.ShrinkProperty,
                Flex.GrowProperty);

            AffectsParentArrange<FlexPanel>(
                Flex.AlignSelfProperty);
        }

        /// <summary>
        /// Gets or sets the direction of the <see cref="FlexPanel"/>'s main-axis,
        /// determining the orientation in which child controls are laid out.
        /// </summary>
        /// <remarks>
        /// When omitted, it is set to <see cref="FlexDirection.Row"/>.
        /// Equivalent to CSS flex-direction property
        /// </remarks>
        public FlexDirection Direction
        {
            get => GetValue(DirectionProperty);
            set => SetValue(DirectionProperty, value);
        }

        /// <summary>
        /// Gets or sets the main-axis alignment of child items inside a line of the <see cref="FlexPanel"/>.
        /// Typically used to distribute extra free space leftover after flexible lengths and margins have been resolved.
        /// </summary>
        /// <remarks>
        /// When omitted, it is set to <see cref="JustifyContent.FlexStart"/>.
        /// Equivalent to CSS justify-content property.
        /// </remarks>
        public JustifyContent JustifyContent
        {
            get => GetValue(JustifyContentProperty);
            set => SetValue(JustifyContentProperty, value);
        }

        /// <summary>
        /// Gets or sets the cross-axis alignment of all child items inside a line of the <see cref="FlexPanel"/>.
        /// Similar to <see cref="JustifyContent"/>, but in the perpendicular direction.
        /// </summary>
        /// <remarks>
        /// When omitted, it is set to <see cref="AlignItems.Stretch"/>.
        /// Equivalent to CSS align-items property.
        /// </remarks>
        public AlignItems AlignItems
        {
            get => GetValue(AlignItemsProperty);
            set => SetValue(AlignItemsProperty, value);
        }

        /// <summary>
        /// Gets or sets the cross-axis alignment of lines in the <see cref="FlexPanel"/> when there is extra space. 
        /// Similar to <see cref="AlignItems"/>, but for entire lines.
        /// <see cref="FlexPanel.Wrap"/> property set to <see cref="FlexWrap.Wrap"/> mode
        /// allows controls to be arranged on multiple lines.
        /// </summary>
        /// <remarks>
        /// When omitted, it is set to <see cref="AlignContent.Stretch"/>.
        /// Equivalent to CSS align-content property.
        /// </remarks>
        public AlignContent AlignContent
        {
            get => GetValue(AlignContentProperty);
            set => SetValue(AlignContentProperty, value);
        }

        /// <summary>
        /// Gets or sets the wrap mode, controlling whether the <see cref="FlexPanel"/> is single-line or multi-line.
        /// Additionally, it determines the cross-axis stacking direction for new lines.
        /// </summary>
        /// <remarks>
        /// When omitted, it is set to <see cref="FlexWrap.NoWrap"/>.
        /// Equivalent to CSS flex-wrap property.
        /// </remarks>
        public FlexWrap Wrap
        {
            get => GetValue(WrapProperty);
            set => SetValue(WrapProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum horizontal spacing between child items or lines,
        /// depending on main-axis direction of the <see cref="FlexPanel"/>.
        /// </summary>
        /// <remarks>
        /// When omitted, it is set to 0.
        /// Similar to CSS column-gap property.
        /// </remarks>
        public double ColumnSpacing
        {
            get => GetValue(ColumnSpacingProperty);
            set => SetValue(ColumnSpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum vertical spacing between child items or lines,
        /// depending on main-axis direction of the <see cref="FlexPanel"/>.
        /// </summary>
        /// <remarks>
        /// When omitted, it is set to 0.
        /// Similar to CSS row-gap property.
        /// </remarks>
        public double RowSpacing
        {
            get => GetValue(RowSpacingProperty);
            set => SetValue(RowSpacingProperty, value);
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            var children = (IReadOnlyList<Layoutable>)Children;
            children = children.Where(s_isVisible).OrderBy(s_getOrder).ToArray();

            var isColumn = Direction is FlexDirection.Column or FlexDirection.ColumnReverse;

            var max = Uv.FromSize(availableSize, isColumn);
            var spacing = Uv.FromSize(ColumnSpacing, RowSpacing, isColumn);

            LineData lineData = default;
            var (childIndex, firstChildIndex, itemIndex) = (0, 0, 0);

            var flexLines = new List<FlexLine>();

            foreach (var element in children)
            {
                var size = MeasureChild(element, max, isColumn);

                if (Wrap != FlexWrap.NoWrap && lineData.U + size.U + itemIndex * spacing.U > max.U)
                {
                    flexLines.Add(new FlexLine(firstChildIndex, childIndex - 1, lineData));
                    lineData = default;
                    firstChildIndex = childIndex;
                    itemIndex = 0;
                }

                lineData.U += size.U;
                lineData.V = Math.Max(lineData.V, size.V);
                lineData.Shrink += Flex.GetShrink(element);
                lineData.Grow += Flex.GetGrow(element);
                lineData.AutoMargins += GetItemAutoMargins(element, isColumn);
                itemIndex++;
                childIndex++;
            }

            if (itemIndex != 0)
            {
                flexLines.Add(new FlexLine(firstChildIndex, firstChildIndex + itemIndex - 1, lineData));
            }

            var state = new FlexLayoutState(children, flexLines, Wrap);

            var totalSpacingV = (flexLines.Count - 1) * spacing.V;
            var panelSizeU = flexLines.Count > 0 ? flexLines.Max(flexLine => flexLine.U + (flexLine.Count - 1) * spacing.U) : 0.0;

            // Resizing along main axis using grow and shrink factors can affect cross axis, so remeasure affected items and lines.
            foreach (var flexLine in flexLines)
            {
                var (itemsCount, totalSpacingU, totalU, freeU) = GetLineMeasureU(flexLine, max.U, spacing.U);
                var (lineMult, autoMargins, remainingFreeU) = GetLineMultInfo(flexLine, freeU);
                
                if (lineMult != 0.0 && remainingFreeU != 0.0)
                {
                    foreach (var element in state.GetLineItems(flexLine))
                    {
                        var baseLength = Flex.GetBaseLength(element);
                        var mult = GetItemMult(element, freeU);
                        if (mult != 0.0)
                        {
                            var length = Math.Max(0.0, baseLength + remainingFreeU * mult / lineMult);
                            element.Measure(Uv.ToSize(max.WithU(length), isColumn));
                        }
                    }

                    flexLine.V = state.GetLineItems(flexLine).Max(i => Uv.FromSize(i.DesiredSize, isColumn).V);
                }
            }

            _state = state;
            var totalLineV = flexLines.Sum(l => l.V);
            var panelSize = flexLines.Count == 0 ? default : new Uv(panelSizeU, totalLineV + totalSpacingV);
            return Uv.ToSize(panelSize, isColumn);
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            var state = _state ?? throw new InvalidOperationException();

            var isColumn = Direction is FlexDirection.Column or FlexDirection.ColumnReverse;
            var isReverse = Direction is FlexDirection.RowReverse or FlexDirection.ColumnReverse;

            var panelSize = Uv.FromSize(finalSize, isColumn);
            var spacing = Uv.FromSize(ColumnSpacing, RowSpacing, isColumn);

            var linesCount = state.Lines.Count;
            var totalLineV = state.Lines.Sum(s => s.V);
            var totalSpacingV = (linesCount - 1) * spacing.V;
            var totalV = totalLineV + totalSpacingV;
            var freeV = panelSize.V - totalV;

            var alignContent = DetermineAlignContent(AlignContent, freeV, linesCount);

            var (v, spacingV) = GetCrossAxisPosAndSpacing(alignContent, spacing, freeV, linesCount);

            var scaleV = alignContent == AlignContent.Stretch && totalLineV != 0 ? (panelSize.V - totalSpacingV) / totalLineV : 1.0;

            foreach (var line in state.Lines)
            {
                var lineV = scaleV * line.V;
                var (itemsCount, totalSpacingU, totalU, freeU) = GetLineMeasureU(line, panelSize.U, spacing.U);
                var (lineMult, lineAutoMargins, remainingFreeU) = GetLineMultInfo(line, freeU);

                var currentFreeU = remainingFreeU;
                if (lineMult != 0.0 && remainingFreeU != 0.0)
                {
                    foreach (var element in state.GetLineItems(line))
                    {
                        var baseLength = Flex.GetBaseLength(element);
                        var mult = GetItemMult(element, freeU);
                        if (mult != 0.0)
                        {
                            var length = Math.Max(0.0, baseLength + remainingFreeU * mult / lineMult);
                            Flex.SetCurrentLength(element, length);
                            currentFreeU -= length - baseLength;
                        }
                    }
                }
                remainingFreeU = currentFreeU;

                if (lineAutoMargins != 0 && remainingFreeU != 0.0)
                {
                    foreach (var element in state.GetLineItems(line))
                    {
                        var baseLength = Flex.GetCurrentLength(element);
                        var autoMargins = GetItemAutoMargins(element, isColumn);
                        if (autoMargins != 0)
                        {
                            var length = Math.Max(0.0, baseLength + remainingFreeU * autoMargins / lineAutoMargins);
                            Flex.SetCurrentLength(element, length);
                            currentFreeU -= length - baseLength;
                        }
                    }
                }
                remainingFreeU = currentFreeU;

                var (u, spacingU) = GetMainAxisPosAndSpacing(JustifyContent, line, spacing, remainingFreeU, itemsCount);

                foreach (var element in state.GetLineItems(line))
                {
                    var size = Uv.FromSize(element.DesiredSize, isColumn).WithU(Flex.GetCurrentLength(element));
                    var align = Flex.GetAlignSelf(element) ?? AlignItems;

                    var positionV = align switch
                    {
                        AlignItems.FlexStart => v,
                        AlignItems.FlexEnd => v + lineV - size.V,
                        AlignItems.Center => v + (lineV - size.V) / 2,
                        AlignItems.Stretch => v,
                        _ => throw new InvalidOperationException()
                    };

                    size = size.WithV(align == AlignItems.Stretch ? lineV : size.V);
                    var position = new Uv(isReverse ? panelSize.U - size.U - u : u, positionV);
                    element.Arrange(new Rect(Uv.ToPoint(position, isColumn), Uv.ToSize(size, isColumn)));

                    u += size.U + spacingU;
                }

                v += lineV + spacingV;
            }

            return finalSize;
        }

        private static Uv MeasureChild(Layoutable element, Uv max, bool isColumn)
        {
            var basis = Flex.GetBasis(element);
            var flexConstraint = basis.Kind switch
            {
                FlexBasisKind.Auto => max.U,
                FlexBasisKind.Absolute => basis.Value,
                FlexBasisKind.Relative => max.U * basis.Value / 100,
                _ => throw new InvalidOperationException($"Unsupported FlexBasisKind value: {basis.Kind}")
            };
            element.Measure(Uv.ToSize(max.WithU(flexConstraint), isColumn));

            var size = Uv.FromSize(element.DesiredSize, isColumn);
            
            var flexLength = basis.Kind switch
            {
                FlexBasisKind.Auto => size.U,
                FlexBasisKind.Absolute or FlexBasisKind.Relative => Math.Max(size.U, flexConstraint),
                _ => throw new InvalidOperationException()
            };
            size = size.WithU(flexLength);
            
            Flex.SetBaseLength(element, flexLength);
            Flex.SetCurrentLength(element, flexLength);
            return size;
        }

        private static AlignContent DetermineAlignContent(AlignContent currentAlignContent, double freeV, int linesCount)
        {
            // Determine AlignContent based on available space and line count
            return currentAlignContent switch
            {
                // If there's free vertical space, handle distribution based on the content alignment
                AlignContent.Stretch when freeV > 0.0 => AlignContent.Stretch,
                AlignContent.SpaceBetween when freeV > 0.0 && linesCount > 1 => AlignContent.SpaceBetween,
                AlignContent.SpaceAround when freeV > 0.0 && linesCount > 0 => AlignContent.SpaceAround,
                AlignContent.SpaceEvenly when freeV > 0.0 && linesCount > 0 => AlignContent.SpaceEvenly,
                
                // Default alignments when there's no free space or not enough lines
                AlignContent.Stretch => AlignContent.FlexStart,
                AlignContent.SpaceBetween => AlignContent.FlexStart,
                AlignContent.SpaceAround => AlignContent.Center,
                AlignContent.SpaceEvenly => AlignContent.Center,
                AlignContent.FlexStart or AlignContent.Center or AlignContent.FlexEnd => currentAlignContent,
                
                _ => throw new InvalidOperationException($"Unsupported AlignContent value: {currentAlignContent}")
            };
        }
        
        private static (double v, double spacingV) GetCrossAxisPosAndSpacing(AlignContent alignContent, Uv spacing, 
            double freeV, int linesCount)
        {
            return alignContent switch
            {
                AlignContent.FlexStart => (0.0, spacing.V),
                AlignContent.FlexEnd => (freeV, spacing.V),
                AlignContent.Center => (freeV / 2, spacing.V),
                AlignContent.Stretch => (0.0, spacing.V),
                
                AlignContent.SpaceBetween when linesCount > 1 => (0.0, spacing.V + freeV / (linesCount - 1)),
                AlignContent.SpaceBetween => (0.0, spacing.V),
                
                AlignContent.SpaceAround when linesCount > 0 =>  (freeV / linesCount / 2, spacing.V + freeV / linesCount),
                AlignContent.SpaceAround => (freeV / 2, spacing.V),
                
                AlignContent.SpaceEvenly => (freeV / (linesCount + 1), spacing.V + freeV / (linesCount + 1)),
                
                _ => throw new InvalidOperationException($"Unsupported AlignContent value: {alignContent}")
            };
        }
        
        private static (double u, double spacingU) GetMainAxisPosAndSpacing(JustifyContent justifyContent, FlexLine line, 
            Uv spacing, double remainingFreeU, int itemsCount)
        {
            return line.Grow > 0 ? (0.0, spacing.U) : justifyContent switch
            {
                JustifyContent.FlexStart => (0.0, spacing.U),
                JustifyContent.FlexEnd => (remainingFreeU, spacing.U),
                JustifyContent.Center => (remainingFreeU / 2, spacing.U),
                
                JustifyContent.SpaceBetween when itemsCount > 1 => (0.0, spacing.U + remainingFreeU / (itemsCount - 1)),
                JustifyContent.SpaceBetween => (0.0, spacing.U),
                
                JustifyContent.SpaceAround when itemsCount > 0 => (remainingFreeU / itemsCount / 2, spacing.U + remainingFreeU / itemsCount),
                JustifyContent.SpaceAround => (remainingFreeU / 2, spacing.U),
                
                JustifyContent.SpaceEvenly when itemsCount > 0 =>  (remainingFreeU / (itemsCount + 1), spacing.U + remainingFreeU / (itemsCount + 1)), 
                JustifyContent.SpaceEvenly => (remainingFreeU / 2, spacing.U),
                
                _ => throw new InvalidOperationException($"Unsupported JustifyContent value: {justifyContent}")
            };
        }

        private static (int ItemsCount, double TotalSpacingU, double TotalU, double FreeU) GetLineMeasureU(
            FlexLine line, double panelSizeU, double spacingU)
        {
            var itemsCount = line.Count;
            var totalSpacingU = (itemsCount - 1) * spacingU;
            var totalU = line.U + totalSpacingU;
            var freeU = panelSizeU - totalU;
            return (itemsCount, totalSpacingU, totalU, freeU);
        }

        private static (double LineMult, double LineAutoMargins, double RemainingFreeU) GetLineMultInfo(FlexLine line, double freeU)
        {
            var lineMult = freeU switch
            {
                < 0 => line.Shrink,
                > 0 => line.Grow,
                _ => 0.0,
            };
            // https://www.w3.org/TR/css-flexbox-1/#remaining-free-space
            // Sum of flex factors less than 1 reduces remaining free space to be distributed.
            return lineMult is > 0 and < 1
                ? (lineMult, line.AutoMargins, freeU * lineMult)
                : (lineMult, line.AutoMargins, freeU);
        }

        private static double GetItemMult(Layoutable element, double freeU)
        {
            var mult = freeU switch
            {
                < 0 => Flex.GetShrink(element),
                > 0 => Flex.GetGrow(element),
                _ => 0.0,
            };
            return mult;
        }

        private static int GetItemAutoMargins(Layoutable element, bool isColumn)
        {
            return isColumn
                ? element.VerticalAlignment switch
                {
                    VerticalAlignment.Stretch => 0,
                    VerticalAlignment.Top or VerticalAlignment.Bottom => 1,
                    VerticalAlignment.Center => 2,
                    _ => throw new InvalidOperationException()
                }
                : element.HorizontalAlignment switch
                {
                    HorizontalAlignment.Stretch => 0,
                    HorizontalAlignment.Left or HorizontalAlignment.Right => 1,
                    HorizontalAlignment.Center => 2,
                    _ => throw new InvalidOperationException()
                };
        }

        private readonly struct FlexLayoutState
        {
            private readonly IReadOnlyList<Layoutable> _children;

            public IReadOnlyList<FlexLine> Lines { get; }

            public FlexLayoutState(IReadOnlyList<Layoutable> children, List<FlexLine> lines, FlexWrap wrap)
            {
                if (wrap == FlexWrap.WrapReverse)
                {
                    lines.Reverse();
                }
                _children = children;
                Lines = lines;
            }

            public IEnumerable<Layoutable> GetLineItems(FlexLine line)
            {
                for (var i = line.First; i <= line.Last; i++)
                    yield return _children[i];
            }
        }

        private struct LineData
        {
            public double U { get; set; }

            public double V { get; set; }

            public double Shrink { get; set; }

            public double Grow { get; set; }

            public int AutoMargins { get; set; }
        }

        private class FlexLine
        {
            public FlexLine(int first, int last, LineData l)
            {
                First = first;
                Last = last;
                U = l.U;
                V = l.V;
                Shrink = l.Shrink;
                Grow = l.Grow;
                AutoMargins = l.AutoMargins;
            }

            /// <summary>First item index.</summary>
            public int First { get; }

            /// <summary>Last item index.</summary>
            public int Last { get; }

            /// <summary>Sum of main sizes of items.</summary>
            public double U { get; }

            /// <summary>Max of cross sizes of items.</summary>
            public double V { get; set; }

            /// <summary>Sum of shrink factors of flexible items.</summary>
            public double Shrink { get; }

            /// <summary>Sum of grow factors of flexible items.</summary>
            public double Grow { get; }

            /// <summary>Number of "auto margins" along main axis.</summary>
            public int AutoMargins { get; }

            /// <summary>Number of items.</summary>
            public int Count => Last - First + 1;
        }
    }
}
