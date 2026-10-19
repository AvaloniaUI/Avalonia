using System;
using Avalonia;
using Avalonia.Controls;

namespace ControlCatalog.Controls
{
    /// <summary>
    /// A panel that lays out its children in equal-width columns, choosing the column count from the
    /// available width and <see cref="MinItemWidth"/>. Rows are as tall as their tallest child.
    /// Used for every card grid in the catalog so the home page and the sample galleries reflow identically.
    /// </summary>
    public class CardGrid : Panel
    {
        public static readonly StyledProperty<double> MinItemWidthProperty =
            AvaloniaProperty.Register<CardGrid, double>(nameof(MinItemWidth), 248);

        public static readonly StyledProperty<double> ColumnSpacingProperty =
            AvaloniaProperty.Register<CardGrid, double>(nameof(ColumnSpacing), 12);

        public static readonly StyledProperty<double> RowSpacingProperty =
            AvaloniaProperty.Register<CardGrid, double>(nameof(RowSpacing), 12);

        static CardGrid()
        {
            AffectsMeasure<CardGrid>(MinItemWidthProperty, ColumnSpacingProperty, RowSpacingProperty);
        }

        /// <summary>
        /// The narrowest a column may be. The column count is the largest number of columns that keeps every
        /// column at least this wide.
        /// </summary>
        public double MinItemWidth
        {
            get => GetValue(MinItemWidthProperty);
            set => SetValue(MinItemWidthProperty, value);
        }

        public double ColumnSpacing
        {
            get => GetValue(ColumnSpacingProperty);
            set => SetValue(ColumnSpacingProperty, value);
        }

        public double RowSpacing
        {
            get => GetValue(RowSpacingProperty);
            set => SetValue(RowSpacingProperty, value);
        }

        private int _columns = 1;
        private double[] _rowHeights = Array.Empty<double>();

        private int VisibleCount
        {
            get
            {
                var count = 0;
                foreach (var child in Children)
                {
                    if (child.IsVisible)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        private int ComputeColumns(double availableWidth, int count)
        {
            if (count == 0)
            {
                return 1;
            }

            var minWidth = Math.Max(1, MinItemWidth);
            var spacing = Math.Max(0, ColumnSpacing);
            int columns;

            if (double.IsInfinity(availableWidth))
            {
                columns = Math.Max(1, count);
            }
            else
            {
                columns = (int)Math.Floor((availableWidth + spacing) / (minWidth + spacing));
            }

            // Column width depends only on the available width, not on how many children there are, so a
            // group with one card gets the same card width as a group with six.
            columns = Math.Max(1, columns);

            return columns;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var count = VisibleCount;
            var columnSpacing = Math.Max(0, ColumnSpacing);
            var rowSpacing = Math.Max(0, RowSpacing);

            _columns = ComputeColumns(availableSize.Width, count);

            var width = double.IsInfinity(availableSize.Width)
                ? _columns * Math.Max(1, MinItemWidth) + columnSpacing * (_columns - 1)
                : availableSize.Width;

            var columnWidth = Math.Max(0, (width - columnSpacing * (_columns - 1)) / _columns);

            var rows = count == 0 ? 0 : (count + _columns - 1) / _columns;
            _rowHeights = rows == 0 ? Array.Empty<double>() : new double[rows];

            var index = 0;
            foreach (var child in Children)
            {
                if (!child.IsVisible)
                {
                    continue;
                }

                child.Measure(new Size(columnWidth, double.PositiveInfinity));
                var row = index / _columns;
                _rowHeights[row] = Math.Max(_rowHeights[row], child.DesiredSize.Height);
                index++;
            }

            double height = 0;
            for (var i = 0; i < rows; i++)
            {
                height += _rowHeights[i];
            }

            if (rows > 1)
            {
                height += rowSpacing * (rows - 1);
            }

            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var columnSpacing = Math.Max(0, ColumnSpacing);
            var rowSpacing = Math.Max(0, RowSpacing);
            var columnWidth = _columns == 0
                ? finalSize.Width
                : Math.Max(0, (finalSize.Width - columnSpacing * (_columns - 1)) / _columns);

            var index = 0;
            double y = 0;
            foreach (var child in Children)
            {
                if (!child.IsVisible)
                {
                    continue;
                }

                var row = index / _columns;
                var column = index % _columns;

                if (column == 0 && index > 0)
                {
                    y += _rowHeights[row - 1] + rowSpacing;
                }

                var x = column * (columnWidth + columnSpacing);
                var rowHeight = row < _rowHeights.Length ? _rowHeights[row] : child.DesiredSize.Height;
                child.Arrange(new Rect(x, y, columnWidth, rowHeight));
                index++;
            }

            return finalSize;
        }
    }
}
