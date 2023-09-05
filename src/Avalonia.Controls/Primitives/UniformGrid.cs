using System;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// A <see cref="Panel"/> with uniform column and row sizes.
    /// </summary>
    public class UniformGrid : Panel
    {
        /// <summary>
        /// Defines the <see cref="Rows"/> property.
        /// </summary>
        public static readonly StyledProperty<int> RowsProperty =
            AvaloniaProperty.Register<UniformGrid, int>(nameof(Rows));

        /// <summary>
        /// Defines the <see cref="Columns"/> property.
        /// </summary>
        public static readonly StyledProperty<int> ColumnsProperty =
            AvaloniaProperty.Register<UniformGrid, int>(nameof(Columns));

        /// <summary>
        /// Defines the <see cref="FirstColumn"/> property.
        /// </summary>
        public static readonly StyledProperty<int> FirstColumnProperty =
            AvaloniaProperty.Register<UniformGrid, int>(nameof(FirstColumn));

        /// <summary>
        /// Defines the <see cref="RowSpacing"/> property.
        /// </summary>
        public static readonly StyledProperty<double> RowSpacingProperty =
            AvaloniaProperty.Register<UniformGrid, double>(nameof(RowSpacing));

        /// <summary>
        /// Defines the <see cref="ColumnSpacing"/> property.
        /// </summary>
        public static readonly StyledProperty<double> ColumnSpacingProperty =
            AvaloniaProperty.Register<UniformGrid, double>(nameof(ColumnSpacing));

        private int _rows;
        private int _columns;

        static UniformGrid()
        {
            AffectsMeasure<UniformGrid>(RowsProperty,
                                        ColumnsProperty,
                                        FirstColumnProperty,
                                        RowSpacingProperty,
                                        ColumnSpacingProperty);
        }

        /// <summary>
        /// Specifies the row count. If set to 0, row count will be calculated automatically.
        /// </summary>
        public int Rows
        {
            get => GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        /// <summary>
        /// Specifies the column count. If set to 0, column count will be calculated automatically.
        /// </summary>
        public int Columns
        {
            get => GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        /// <summary>
        /// Specifies, for the first row, the column where the items should start.
        /// </summary>
        public int FirstColumn
        {
            get => GetValue(FirstColumnProperty);
            set => SetValue(FirstColumnProperty, value);
        }

        /// <summary>
        /// Gets or sets the size of the row spacing to place between child controls.
        /// </summary>
        public double RowSpacing
        {
            get => GetValue(RowSpacingProperty);
            set => SetValue(RowSpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the size of the column spacing to place between child controls.
        /// </summary>
        public double ColumnSpacing
        {
            get => GetValue(ColumnSpacingProperty);
            set => SetValue(ColumnSpacingProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            UpdateRowsAndColumns();

            var maxWidth = 0d;
            var maxHeight = 0d;

            var childAvailableSize = new Size(availableSize.Width / _columns, availableSize.Height / _rows);

            foreach (var child in Children)
            {
                child.Measure(childAvailableSize);

                if (child.DesiredSize.Width > maxWidth)
                {
                    maxWidth = child.DesiredSize.Width;
                }

                if (child.DesiredSize.Height > maxHeight)
                {
                    maxHeight = child.DesiredSize.Height;
                }
            }

            var desiredWidth = maxWidth * _columns;
            var desiredHeight = maxHeight * _rows;

            if (desiredWidth > 0d)
                desiredWidth += (_columns - 1) * ColumnSpacing;

            if (desiredHeight > 0d)
                desiredHeight += (_rows - 1) * RowSpacing;

            return new Size(desiredWidth, desiredHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var x = FirstColumn;
            var y = 0;

            var columnSpacing = ColumnSpacing;
            var rowSpacing = RowSpacing;

            var width = Math.Max(0d, (finalSize.Width - (_columns - 1) * columnSpacing) / _columns);
            var height = Math.Max(0d, (finalSize.Height - (_rows - 1) * rowSpacing) / _rows);

            foreach (var child in Children)
            {
                if (!child.IsVisible)
                {
                    continue;
                }

                child.Arrange(new Rect(x * (width + columnSpacing), y * (height + rowSpacing), width, height));

                x++;

                if (x >= _columns)
                {
                    x = 0;
                    y++;
                }
            }

            return finalSize;
        }

        private void UpdateRowsAndColumns()
        {
            _rows = Rows;
            _columns = Columns;

            if (FirstColumn >= Columns)
            {
                SetCurrentValue(FirstColumnProperty, 0);
            }

            // return if no shuold calculate
            if (_rows > 0 && _columns > 0)
                return;

            var itemCount = FirstColumn;

            foreach (var child in Children)
            {
                if (child.IsVisible)
                {
                    itemCount++;
                }
            }

            if (_rows == 0)
            {
                if (_columns == 0)
                {
                    _rows = _columns = (int)Math.Ceiling(Math.Sqrt(itemCount));
                }
                else
                {
                    _rows = Math.DivRem(itemCount, _columns, out int rem);

                    if (rem != 0)
                    {
                        _rows++;
                    }
                }
            }
            else if (_columns == 0)
            {
                _columns = Math.DivRem(itemCount, _rows, out int rem);

                if (rem != 0)
                {
                    _columns++;
                }
            }
        }
    }
}