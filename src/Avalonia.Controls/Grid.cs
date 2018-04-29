// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Collections;
using Avalonia.Controls.Utils;
using JetBrains.Annotations;

namespace Avalonia.Controls
{
    /// <summary>
    /// Lays out child controls according to a grid.
    /// </summary>
    public class Grid : Panel
    {
        /// <summary>
        /// Defines the Column attached property.
        /// </summary>
        public static readonly AttachedProperty<int> ColumnProperty =
            AvaloniaProperty.RegisterAttached<Grid, Control, int>(
                "Column",
                validate: ValidateColumn);

        /// <summary>
        /// Defines the ColumnSpan attached property.
        /// </summary>
        public static readonly AttachedProperty<int> ColumnSpanProperty =
            AvaloniaProperty.RegisterAttached<Grid, Control, int>("ColumnSpan", 1);

        /// <summary>
        /// Defines the Row attached property.
        /// </summary>
        public static readonly AttachedProperty<int> RowProperty =
            AvaloniaProperty.RegisterAttached<Grid, Control, int>(
                "Row",
                validate: ValidateRow);

        /// <summary>
        /// Defines the RowSpan attached property.
        /// </summary>
        public static readonly AttachedProperty<int> RowSpanProperty =
            AvaloniaProperty.RegisterAttached<Grid, Control, int>("RowSpan", 1);

        private ColumnDefinitions _columnDefinitions;

        private RowDefinitions _rowDefinitions;

        private Segment[,] _rowMatrix;

        private Segment[,] _colMatrix;

        /// <summary>
        /// Gets or sets the columns definitions for the grid.
        /// </summary>
        public ColumnDefinitions ColumnDefinitions
        {
            get
            {
                if (_columnDefinitions == null)
                {
                    ColumnDefinitions = new ColumnDefinitions();
                }

                return _columnDefinitions;
            }

            set
            {
                if (_columnDefinitions != null)
                {
                    throw new NotSupportedException("Reassigning ColumnDefinitions not yet implemented.");
                }

                _columnDefinitions = value;
                _columnDefinitions.TrackItemPropertyChanged(_ => InvalidateMeasure());
            }
        }

        /// <summary>
        /// Gets or sets the row definitions for the grid.
        /// </summary>
        public RowDefinitions RowDefinitions
        {
            get
            {
                if (_rowDefinitions == null)
                {
                    RowDefinitions = new RowDefinitions();
                }

                return _rowDefinitions;
            }

            set
            {
                if (_rowDefinitions != null)
                {
                    throw new NotSupportedException("Reassigning RowDefinitions not yet implemented.");
                }

                _rowDefinitions = value;
                _rowDefinitions.TrackItemPropertyChanged(_ => InvalidateMeasure());
            }
        }

        /// <summary>
        /// Gets the value of the Column attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's column.</returns>
        public static int GetColumn(AvaloniaObject element)
        {
            return element.GetValue(ColumnProperty);
        }

        /// <summary>
        /// Gets the value of the ColumnSpan attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's column span.</returns>
        public static int GetColumnSpan(AvaloniaObject element)
        {
            return element.GetValue(ColumnSpanProperty);
        }

        /// <summary>
        /// Gets the value of the Row attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's row.</returns>
        public static int GetRow(AvaloniaObject element)
        {
            return element.GetValue(RowProperty);
        }

        /// <summary>
        /// Gets the value of the RowSpan attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's row span.</returns>
        public static int GetRowSpan(AvaloniaObject element)
        {
            return element.GetValue(RowSpanProperty);
        }

        /// <summary>
        /// Sets the value of the Column attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The column value.</param>
        public static void SetColumn(AvaloniaObject element, int value)
        {
            element.SetValue(ColumnProperty, value);
        }

        /// <summary>
        /// Sets the value of the ColumnSpan attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The column span value.</param>
        public static void SetColumnSpan(AvaloniaObject element, int value)
        {
            element.SetValue(ColumnSpanProperty, value);
        }

        /// <summary>
        /// Sets the value of the Row attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The row value.</param>
        public static void SetRow(AvaloniaObject element, int value)
        {
            element.SetValue(RowProperty, value);
        }

        /// <summary>
        /// Sets the value of the RowSpan attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The row span value.</param>
        public static void SetRowSpan(AvaloniaObject element, int value)
        {
            element.SetValue(RowSpanProperty, value);
        }

        /// <summary>
        /// Measures the grid.
        /// </summary>
        /// <param name="constraint">The available size.</param>
        /// <returns>The desired size of the control.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            // +------- 1. Prepare the children status -------+
            // +                                              +
            // +------- ------------------------------ -------+

            // Normalize the column/columnspan and row/rowspan.
            var columnCount = ColumnDefinitions.Count;
            var rowCount = RowDefinitions.Count;
            var safeColumns = Children.OfType<Control>().ToDictionary(child => child,
                child => GetSafeSpan(columnCount, GetColumn(child), GetColumnSpan(child)));
            var safeRows = Children.OfType<Control>().ToDictionary(child => child,
                child => GetSafeSpan(rowCount, GetRow(child), GetRowSpan(child)));

            // +------- 2.                             -------+
            // +                                              +
            // +------- ------------------------------ -------+

            // Find out the children that should be Measure first (those rows/columns are Auto size.)
            var columnLayout = new GridLayout(ColumnDefinitions);
            var rowLayout = new GridLayout(RowDefinitions);
            var autoSizeColumns = columnLayout.Prepare();
            var autoSizeRows = rowLayout.Prepare();

            foreach (var pair in safeColumns)
            {
                var child = pair.Key;
                var (column, columnSpan) = pair.Value;
                var columnLast = column + columnSpan - 1;
                if (autoSizeColumns.Contains(columnLast))
                {
                    
                }
            }

            // Calculate row height list and column width list.
            var widthList = columnLayout.Measure(constraint.Width);
            var heightList = rowLayout.Measure(constraint.Height);

            // Calculate the available width list and height list for every child.
            var childrenAvailableWidths = Children.OfType<Control>().ToDictionary(child => child, child =>
            {
                var (column, columnSpan) = GetSafeSpan(widthList.Count, GetColumn(child), GetColumnSpan(child));
                return Span(widthList, column, columnSpan).Sum();
            });
            var childrenAvailableHeights = Children.OfType<Control>().ToDictionary(child => child, child =>
            {
                var (row, rowSpan) = GetSafeSpan(heightList.Count, GetRow(child), GetRowSpan(child));
                return Span(heightList, row, rowSpan).Sum();
            });

            // Measure the children.
            var availableWidth = constraint.Width;
            var availableHeight = constraint.Height;
            var desiredWidth = 0.0;
            var desiredHeight = 0.0;
            var sortedChildren = Children.OfType<Control>()
                .OrderBy(GetColumn).ThenBy(GetRow)
                .ToDictionary(child => child, child => (GetColumn(child), GetRow(child)));

            var currentDesiredWidth = 0.0;
            var currentDesiredHeight = 0.0;
            var currentColumn = 0;
            var currentRow = 0;
            foreach (var pair in sortedChildren)
            {
                var child = pair.Key;
                var (column, row) = pair.Value;
                child.Measure(new Size(childrenAvailableWidths[child], childrenAvailableHeights[child]));
                var desiredSize = child.DesiredSize;

                if (column == currentColumn)
                {
                    currentDesiredWidth = Math.Max(desiredSize.Width, currentDesiredWidth);
                }
                else
                {
                    currentDesiredWidth = desiredSize.Width;
                    currentColumn = column;
                    availableWidth -= desiredSize.Width;
                    desiredHeight -= desiredSize.Width;
                }

                if (availableWidth < desiredSize.Width)
                {
                    
                }

                availableHeight -= desiredSize.Height;
                desiredHeight -= desiredSize.Width;
            }

            return constraint;
        }

        /// <summary>
        /// Arranges the grid's children.
        /// </summary>
        /// <param name="finalSize">The size allocated to the control.</param>
        /// <returns>The space taken.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            // Calculate row height list and column width list.
            var rowLayout = new GridLayout(RowDefinitions);
            var columnLayout = new GridLayout(ColumnDefinitions);
            var heightList = rowLayout.Measure(finalSize.Height);
            var widthList = columnLayout.Measure(finalSize.Width);

            var rowMeasure = new Dictionary<Control, (double row, double rowspan)>();
            var columnMeasure = new Dictionary<Control, (double column, double columnspan)>();
            foreach (var child in Children.OfType<Control>().OrderBy(GetRow))
            {
                var (row, rowSpan) = GetSafeSpan(heightList.Count, GetRow(child), GetRowSpan(child));
                rowMeasure.Add(child, (row, rowSpan));
            }
            foreach (var child in Children.OfType<Control>().OrderBy(GetColumn))
            {
                var (column, columnSpan) = GetSafeSpan(widthList.Count, GetColumn(child), GetColumnSpan(child));
                columnMeasure.Add(child, (column, columnSpan));
            }

        }

        /// <summary>
        /// Gets the safe row/column and rowspan/columnspan for a specified range.
        /// The user may assign the row/column properties out of the row count or column cout, this method helps to keep them in.
        /// </summary>
        /// <param name="length">The rows count or the columns count.</param>
        /// <param name="userIndex">The row or column that the user assigned.</param>
        /// <param name="userSpan">The rowspan or columnspan that the user assigned.</param>
        /// <returns>The safe row/column and rowspan/columnspan.</returns>
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (int index, int span) GetSafeSpan(int length, int userIndex, int userSpan)
        {
            var index = userIndex;
            var span = userSpan;
            if (userIndex > length)
            {
                index = length;
                span = 1;
            }
            else if (userIndex + userSpan > length)
            {
                span = length - userIndex + 1;
            }

            return (index, span);
        }

        /// <summary>
        /// Return part of a list from the specified start index and its span length.
        /// If Avalonia upgrade .NET Core to 2.1 and introduce C# 7.2, we can use Span to do this.
        /// </summary>
        [Pure]
        private static IEnumerable<double> Span(IList<double> list, int index, int span)
        {
#if DEBUG
            // We do not verify arguments in RELEASE because this is a private method,
            // and we must write the correct code before publishing.
            if (index >= list.Count) throw new ArgumentOutOfRangeException(nameof(index));
            if (span <= 1) throw new ArgumentException("Argument span should not be smaller than 1.", nameof(span));
            if (index + span > list.Count) throw new ArgumentOutOfRangeException(nameof(index));
#endif
            if (span == 1)
            {
                yield return list[index];
                yield break;
            }

            for (var i = index; i < index + span; i++)
            {
                yield return list[i];
            }
        }

        private static double Clamp(double val, double min, double max)
        {
            if (val < min)
            {
                return min;
            }
            else if (val > max)
            {
                return max;
            }
            else
            {
                return val;
            }
        }

        private static int ValidateColumn(AvaloniaObject o, int value)
        {
            if (value < 0)
            {
                throw new ArgumentException("Invalid Grid.Column value.");
            }

            return value;
        }

        private static int ValidateRow(AvaloniaObject o, int value)
        {
            if (value < 0)
            {
                throw new ArgumentException("Invalid Grid.Row value.");
            }

            return value;
        }

        private void CreateMatrices(int rowCount, int colCount)
        {
            if (_rowMatrix == null || _colMatrix == null ||
                _rowMatrix.GetLength(0) != rowCount ||
                _colMatrix.GetLength(0) != colCount)
            {
                _rowMatrix = new Segment[rowCount, rowCount];
                _colMatrix = new Segment[colCount, colCount];
            }
            else
            {
                Array.Clear(_rowMatrix, 0, _rowMatrix.Length);
                Array.Clear(_colMatrix, 0, _colMatrix.Length);
            }
        }

        private void ExpandStarCols(Size availableSize)
        {
            int matrixCount = _colMatrix.GetLength(0);
            int columnsCount = ColumnDefinitions.Count;
            double width = availableSize.Width;

            for (int i = 0; i < matrixCount; i++)
            {
                if (_colMatrix[i, i].Type == GridUnitType.Star)
                {
                    _colMatrix[i, i].OfferedSize = 0;
                }
                else
                {
                    width = Math.Max(width - _colMatrix[i, i].OfferedSize, 0);
                }
            }

            AssignSize(_colMatrix, 0, matrixCount - 1, ref width, GridUnitType.Star, false);
            width = Math.Max(0, width);

            if (columnsCount > 0)
            {
                for (int i = 0; i < matrixCount; i++)
                {
                    if (_colMatrix[i, i].Type == GridUnitType.Star)
                    {
                        ColumnDefinitions[i].ActualWidth = _colMatrix[i, i].OfferedSize;
                    }
                }
            }
        }

        private void ExpandStarRows(Size availableSize)
        {
            int matrixCount = _rowMatrix.GetLength(0);
            int rowCount = RowDefinitions.Count;
            double height = availableSize.Height;

            // When expanding star rows, we need to zero out their height before
            // calling AssignSize. AssignSize takes care of distributing the
            // available size when there are Mins and Maxs applied.
            for (int i = 0; i < matrixCount; i++)
            {
                if (_rowMatrix[i, i].Type == GridUnitType.Star)
                {
                    _rowMatrix[i, i].OfferedSize = 0.0;
                }
                else
                {
                    height = Math.Max(height - _rowMatrix[i, i].OfferedSize, 0);
                }
            }

            AssignSize(_rowMatrix, 0, matrixCount - 1, ref height, GridUnitType.Star, false);

            if (rowCount > 0)
            {
                for (int i = 0; i < matrixCount; i++)
                {
                    if (_rowMatrix[i, i].Type == GridUnitType.Star)
                    {
                        RowDefinitions[i].ActualHeight = _rowMatrix[i, i].OfferedSize;
                    }
                }
            }
        }

        private void AssignSize(
            Segment[,] matrix,
            int start,
            int end,
            ref double size,
            GridUnitType type,
            bool desiredSize)
        {
            double count = 0;
            bool assigned;

            // Count how many segments are of the correct type. If we're measuring Star rows/cols
            // we need to count the number of stars instead.
            for (int i = start; i <= end; i++)
            {
                double segmentSize = desiredSize ? matrix[i, i].DesiredSize : matrix[i, i].OfferedSize;
                if (segmentSize < matrix[i, i].Max)
                {
                    count += type == GridUnitType.Star ? matrix[i, i].Stars : 1;
                }
            }

            do
            {
                double contribution = size / count;

                assigned = false;

                for (int i = start; i <= end; i++)
                {
                    double segmentSize = desiredSize ? matrix[i, i].DesiredSize : matrix[i, i].OfferedSize;

                    if (!(matrix[i, i].Type == type && segmentSize < matrix[i, i].Max))
                    {
                        continue;
                    }

                    double newsize = segmentSize;
                    newsize += contribution * (type == GridUnitType.Star ? matrix[i, i].Stars : 1);
                    double newSizeIgnoringMinMax = newsize;
                    newsize = Math.Min(newsize, matrix[i, i].Max);
                    newsize = Math.Max(newsize, matrix[i, i].Min);
                    assigned |= !Equals(newsize, newSizeIgnoringMinMax);
                    size -= newsize - segmentSize;

                    if (desiredSize)
                    {
                        matrix[i, i].DesiredSize = newsize;
                    }
                    else
                    {
                        matrix[i, i].OfferedSize = newsize;
                    }
                }
            }
            while (assigned);
        }

        private void AllocateDesiredSize(int rowCount, int colCount)
        {
            // First allocate the heights of the RowDefinitions, then allocate
            // the widths of the ColumnDefinitions.
            for (int i = 0; i < 2; i++)
            {
                Segment[,] matrix = i == 0 ? _rowMatrix : _colMatrix;
                int count = i == 0 ? rowCount : colCount;

                for (int row = count - 1; row >= 0; row--)
                {
                    for (int col = row; col >= 0; col--)
                    {
                        bool spansStar = false;
                        for (int j = row; j >= col; j--)
                        {
                            spansStar |= matrix[j, j].Type == GridUnitType.Star;
                        }

                        // This is the amount of pixels which must be available between the grid rows
                        // at index 'col' and 'row'. i.e. if 'row' == 0 and 'col' == 2, there must
                        // be at least 'matrix [row][col].size' pixels of height allocated between
                        // all the rows in the range col -> row.
                        double current = matrix[row, col].DesiredSize;

                        // Count how many pixels have already been allocated between the grid rows
                        // in the range col -> row. The amount of pixels allocated to each grid row/column
                        // is found on the diagonal of the matrix.
                        double totalAllocated = 0;

                        for (int k = row; k >= col; k--)
                        {
                            totalAllocated += matrix[k, k].DesiredSize;
                        }

                        // If the size requirement has not been met, allocate the additional required
                        // size between 'pixel' rows, then 'star' rows, finally 'auto' rows, until all
                        // height has been assigned.
                        if (totalAllocated < current)
                        {
                            double additional = current - totalAllocated;

                            if (spansStar)
                            {
                                AssignSize(matrix, col, row, ref additional, GridUnitType.Star, true);
                            }
                            else
                            {
                                AssignSize(matrix, col, row, ref additional, GridUnitType.Pixel, true);
                                AssignSize(matrix, col, row, ref additional, GridUnitType.Auto, true);
                            }
                        }
                    }
                }
            }

            int rowMatrixDim = _rowMatrix.GetLength(0);
            int colMatrixDim = _colMatrix.GetLength(0);

            for (int r = 0; r < rowMatrixDim; r++)
            {
                _rowMatrix[r, r].OfferedSize = _rowMatrix[r, r].DesiredSize;
            }

            for (int c = 0; c < colMatrixDim; c++)
            {
                _colMatrix[c, c].OfferedSize = _colMatrix[c, c].DesiredSize;
            }
        }

        private void SaveMeasureResults()
        {
            int rowMatrixDim = _rowMatrix.GetLength(0);
            int colMatrixDim = _colMatrix.GetLength(0);

            for (int i = 0; i < rowMatrixDim; i++)
            {
                for (int j = 0; j < rowMatrixDim; j++)
                {
                    _rowMatrix[i, j].OriginalSize = _rowMatrix[i, j].OfferedSize;
                }
            }

            for (int i = 0; i < colMatrixDim; i++)
            {
                for (int j = 0; j < colMatrixDim; j++)
                {
                    _colMatrix[i, j].OriginalSize = _colMatrix[i, j].OfferedSize;
                }
            }
        }

        private void RestoreMeasureResults()
        {
            int rowMatrixDim = _rowMatrix.GetLength(0);
            int colMatrixDim = _colMatrix.GetLength(0);

            for (int i = 0; i < rowMatrixDim; i++)
            {
                for (int j = 0; j < rowMatrixDim; j++)
                {
                    _rowMatrix[i, j].OfferedSize = _rowMatrix[i, j].OriginalSize;
                }
            }

            for (int i = 0; i < colMatrixDim; i++)
            {
                for (int j = 0; j < colMatrixDim; j++)
                {
                    _colMatrix[i, j].OfferedSize = _colMatrix[i, j].OriginalSize;
                }
            }
        }

        /// <summary>
        /// Stores the layout values of of <see cref="RowDefinitions"/> of <see cref="ColumnDefinitions"/>.
        /// </summary>
        private struct Segment
        {
            /// <summary>
            /// Gets or sets the base size of this segment.
            /// The value is from the user's code or from the stored measuring values.
            /// </summary>
            public double OriginalSize;

            /// <summary>
            /// Gets the maximum size of this segment.
            /// The value is from the user's code.
            /// </summary>
            public readonly double Max;

            /// <summary>
            /// Gets the minimum size of this segment.
            /// The value is from the user's code.
            /// </summary>
            public readonly double Min;

            /// <summary>
            /// Gets or sets the row/column partial desired size of the <see cref="Grid"/>.
            /// </summary>
            public double DesiredSize;

            /// <summary>
            /// Gets or sets the row/column offered size that will be used to measure the children.
            /// </summary>
            public double OfferedSize;

            /// <summary>
            /// Gets or sets the star unit size if the <see cref="Type"/> is <see cref="GridUnitType.Star"/>.
            /// </summary>
            public double Stars;

            /// <summary>
            /// Gets the segment size unit type.
            /// </summary>
            public readonly GridUnitType Type;

            public Segment(double offeredSize, double min, double max, GridUnitType type)
            {
                OriginalSize = 0;
                Min = min;
                Max = max;
                DesiredSize = 0;
                OfferedSize = offeredSize;
                Stars = 0;
                Type = type;
            }
        }

        private struct GridNode
        {
            public readonly int Row;
            public readonly int Column;
            public readonly double Size;
            public readonly Segment[,] Matrix;

            public GridNode(Segment[,] matrix, int row, int col, double size)
            {
                Matrix = matrix;
                Row = row;
                Column = col;
                Size = size;
            }
        }

        private class GridWalker
        {
            public GridWalker(Grid grid, Segment[,] rowMatrix, Segment[,] colMatrix)
            {
                int rowMatrixDim = rowMatrix.GetLength(0);
                int colMatrixDim = colMatrix.GetLength(0);

                foreach (Control child in grid.Children)
                {
                    bool starCol = false;
                    bool starRow = false;
                    bool autoCol = false;
                    bool autoRow = false;

                    int col = Math.Min(GetColumn(child), colMatrixDim - 1);
                    int row = Math.Min(GetRow(child), rowMatrixDim - 1);
                    int colspan = Math.Min(GetColumnSpan(child), colMatrixDim - 1);
                    int rowspan = Math.Min(GetRowSpan(child), rowMatrixDim - 1);

                    for (int r = row; r < row + rowspan; r++)
                    {
                        starRow |= rowMatrix[r, r].Type == GridUnitType.Star;
                        autoRow |= rowMatrix[r, r].Type == GridUnitType.Auto;
                    }

                    for (int c = col; c < col + colspan; c++)
                    {
                        starCol |= colMatrix[c, c].Type == GridUnitType.Star;
                        autoCol |= colMatrix[c, c].Type == GridUnitType.Auto;
                    }

                    HasAutoAuto |= autoRow && autoCol && !starRow && !starCol;
                    HasStarAuto |= starRow && autoCol;
                    HasAutoStar |= autoRow && starCol;
                }
            }

            public bool HasAutoAuto { get; }

            public bool HasStarAuto { get; }

            public bool HasAutoStar { get; }
        }
    }
}