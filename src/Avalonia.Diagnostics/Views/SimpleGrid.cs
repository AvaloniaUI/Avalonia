// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Avalonia.Controls;

namespace Avalonia.Diagnostics.Views
{
    /// <summary>
    /// A simple grid control that lays out columns with a equal width and rows to their desired
    /// size.
    /// </summary>
    /// <remarks>
    /// This is used in the devtools because our <see cref="Grid"/> performance sucks.
    /// </remarks>
    public class SimpleGrid : Panel
    {
        private readonly List<double> _columnWidths = new List<double>();
        private readonly List<double> _rowHeights = new List<double>();
        private double _totalWidth;
        private double _totalHeight;

        /// <summary>
        /// Defines the Column attached property.
        /// </summary>
        public static readonly AttachedProperty<int> ColumnProperty =
            AvaloniaProperty.RegisterAttached<SimpleGrid, Control, int>("Column");

        /// <summary>
        /// Defines the Row attached property.
        /// </summary>
        public static readonly AttachedProperty<int> RowProperty =
            AvaloniaProperty.RegisterAttached<SimpleGrid, Control, int>("Row");

        /// <summary>
        /// Gets the value of the Column attached property for a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The control's column.</returns>
        public static int GetColumn(IControl control)
        {
            return control.GetValue(ColumnProperty);
        }

        /// <summary>
        /// Gets the value of the Row attached property for a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The control's row.</returns>
        public static int GetRow(IControl control)
        {
            return control.GetValue(RowProperty);
        }

        /// <summary>
        /// Sets the value of the Column attached property for a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The column value.</param>
        public static void SetColumn(IControl control, int value)
        {
            control.SetValue(ColumnProperty, value);
        }


        /// <summary>
        /// Sets the value of the Row attached property for a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The row value.</param>
        public static void SetRow(IControl control, int value)
        {
            control.SetValue(RowProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            _columnWidths.Clear();
            _rowHeights.Clear();
            _totalWidth = 0;
            _totalHeight = 0;

            foreach (var child in Children)
            {
                var column = GetColumn(child);
                var row = GetRow(child);

                child.Measure(availableSize);

                var desired = child.DesiredSize;
                UpdateCell(_columnWidths, column, desired.Width, ref _totalWidth);
                UpdateCell(_rowHeights, row, desired.Height, ref _totalHeight);
            }

            return new Size(_totalWidth, _totalHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var columnWidth = finalSize.Width / _columnWidths.Count;

            foreach (var child in Children)
            {
                var column = GetColumn(child);
                var row = GetRow(child);
                var rect = new Rect(column * columnWidth, GetRowTop(row), columnWidth, _rowHeights[row]);
                child.Arrange(rect);
            }

            return new Size(finalSize.Width, _totalHeight);
        }

        private double UpdateCell(IList<double> cells, int cell, double value, ref double total)
        {
            while (cells.Count < cell + 1)
            {
                cells.Add(0);
            }

            var existing = cells[cell];

            if (value > existing)
            {
                cells[cell] = value;
                total += value - existing;
                return value;
            }
            else
            {
                return existing;
            }
        }

        private double GetRowTop(int row)
        {
            var result = 0.0;

            for (var i = 0; i < row; ++i)
            {
                result += _rowHeights[i];
            }

            return result;
        }
    }
}
