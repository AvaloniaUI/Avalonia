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

        private int _rows;
        private int _columns;

        static UniformGrid()
        {
            AffectsMeasure<UniformGrid>(RowsProperty, ColumnsProperty, FirstColumnProperty);
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

            return new Size(maxWidth * _columns, maxHeight * _rows);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var x = FirstColumn;
            var y = 0;

            var width = finalSize.Width / _columns;
            var height = finalSize.Height / _rows;

            foreach (var child in Children)
            {
                if (!child.IsVisible)
                {
                    continue;
                }

                child.Arrange(new Rect(x * width, y * height, width, height));

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
