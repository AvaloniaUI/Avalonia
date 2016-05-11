// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;

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
            Size totalSize = constraint;
            int colCount = ColumnDefinitions.Count;
            int rowCount = RowDefinitions.Count;
            double totalStarsX = 0;
            double totalStarsY = 0;
            bool emptyRows = rowCount == 0;
            bool emptyCols = colCount == 0;
            bool hasChildren = Children.Count > 0;

            if (emptyRows)
            {
                rowCount = 1;
            }

            if (emptyCols)
            {
                colCount = 1;
            }

            CreateMatrices(rowCount, colCount);

            if (emptyRows)
            {
                _rowMatrix[0, 0] = new Segment(0, 0, double.PositiveInfinity, GridUnitType.Star);
                _rowMatrix[0, 0].Stars = 1.0;
                totalStarsY += 1.0;
            }
            else
            {
                for (int i = 0; i < rowCount; i++)
                {
                    RowDefinition rowdef = RowDefinitions[i];
                    GridLength height = rowdef.Height;

                    rowdef.ActualHeight = double.PositiveInfinity;
                    _rowMatrix[i, i] = new Segment(0, rowdef.MinHeight, rowdef.MaxHeight, height.GridUnitType);

                    if (height.GridUnitType == GridUnitType.Pixel)
                    {
                        _rowMatrix[i, i].OfferedSize = Clamp(height.Value, _rowMatrix[i, i].Min, _rowMatrix[i, i].Max);
                        _rowMatrix[i, i].DesiredSize = _rowMatrix[i, i].OfferedSize;
                        rowdef.ActualHeight = _rowMatrix[i, i].OfferedSize;
                    }
                    else if (height.GridUnitType == GridUnitType.Star)
                    {
                        _rowMatrix[i, i].Stars = height.Value;
                        totalStarsY += height.Value;
                    }
                    else if (height.GridUnitType == GridUnitType.Auto)
                    {
                        _rowMatrix[i, i].OfferedSize = Clamp(0, _rowMatrix[i, i].Min, _rowMatrix[i, i].Max);
                        _rowMatrix[i, i].DesiredSize = _rowMatrix[i, i].OfferedSize;
                    }
                }
            }

            if (emptyCols)
            {
                _colMatrix[0, 0] = new Segment(0, 0, double.PositiveInfinity, GridUnitType.Star);
                _colMatrix[0, 0].Stars = 1.0;
                totalStarsX += 1.0;
            }
            else
            {
                for (int i = 0; i < colCount; i++)
                {
                    ColumnDefinition coldef = ColumnDefinitions[i];
                    GridLength width = coldef.Width;

                    coldef.ActualWidth = double.PositiveInfinity;
                    _colMatrix[i, i] = new Segment(0, coldef.MinWidth, coldef.MaxWidth, width.GridUnitType);

                    if (width.GridUnitType == GridUnitType.Pixel)
                    {
                        _colMatrix[i, i].OfferedSize = Clamp(width.Value, _colMatrix[i, i].Min, _colMatrix[i, i].Max);
                        _colMatrix[i, i].DesiredSize = _colMatrix[i, i].OfferedSize;
                        coldef.ActualWidth = _colMatrix[i, i].OfferedSize;
                    }
                    else if (width.GridUnitType == GridUnitType.Star)
                    {
                        _colMatrix[i, i].Stars = width.Value;
                        totalStarsX += width.Value;
                    }
                    else if (width.GridUnitType == GridUnitType.Auto)
                    {
                        _colMatrix[i, i].OfferedSize = Clamp(0, _colMatrix[i, i].Min, _colMatrix[i, i].Max);
                        _colMatrix[i, i].DesiredSize = _colMatrix[i, i].OfferedSize;
                    }
                }
            }

            List<GridNode> sizes = new List<GridNode>();
            GridNode node;
            GridNode separator = new GridNode(null, 0, 0, 0);
            int separatorIndex;

            sizes.Add(separator);

            // Pre-process the grid children so that we know what types of elements we have so
            // we can apply our special measuring rules.
            GridWalker gridWalker = new GridWalker(this, _rowMatrix, _colMatrix);

            for (int i = 0; i < 6; i++)
            {
                // These bools tell us which grid element type we should be measuring. i.e.
                // 'star/auto' means we should measure elements with a star row and auto col
                bool autoAuto = i == 0;
                bool starAuto = i == 1;
                bool autoStar = i == 2;
                bool starAutoAgain = i == 3;
                bool nonStar = i == 4;
                bool remainingStar = i == 5;

                if (hasChildren)
                {
                    ExpandStarCols(totalSize);
                    ExpandStarRows(totalSize);
                }

                foreach (Control child in Children)
                {
                    int col, row;
                    int colspan, rowspan;
                    double childSizeX = 0;
                    double childSizeY = 0;
                    bool starCol = false;
                    bool starRow = false;
                    bool autoCol = false;
                    bool autoRow = false;

                    col = Math.Min(GetColumn(child), colCount - 1);
                    row = Math.Min(GetRow(child), rowCount - 1);
                    colspan = Math.Min(GetColumnSpan(child), colCount - col);
                    rowspan = Math.Min(GetRowSpan(child), rowCount - row);

                    for (int r = row; r < row + rowspan; r++)
                    {
                        starRow |= _rowMatrix[r, r].Type == GridUnitType.Star;
                        autoRow |= _rowMatrix[r, r].Type == GridUnitType.Auto;
                    }

                    for (int c = col; c < col + colspan; c++)
                    {
                        starCol |= _colMatrix[c, c].Type == GridUnitType.Star;
                        autoCol |= _colMatrix[c, c].Type == GridUnitType.Auto;
                    }

                    // This series of if statements checks whether or not we should measure
                    // the current element and also if we need to override the sizes
                    // passed to the Measure call.

                    // If the element has Auto rows and Auto columns and does not span Star
                    // rows/cols it should only be measured in the auto_auto phase.
                    // There are similar rules governing auto/star and star/auto elements.
                    // NOTE: star/auto elements are measured twice. The first time with
                    // an override for height, the second time without it.
                    if (autoRow && autoCol && !starRow && !starCol)
                    {
                        if (!autoAuto)
                        {
                            continue;
                        }

                        childSizeX = double.PositiveInfinity;
                        childSizeY = double.PositiveInfinity;
                    }
                    else if (starRow && autoCol && !starCol)
                    {
                        if (!(starAuto || starAutoAgain))
                        {
                            continue;
                        }

                        if (starAuto && gridWalker.HasAutoStar)
                        {
                            childSizeY = double.PositiveInfinity;
                        }

                        childSizeX = double.PositiveInfinity;
                    }
                    else if (autoRow && starCol && !starRow)
                    {
                        if (!autoStar)
                        {
                            continue;
                        }

                        childSizeY = double.PositiveInfinity;
                    }
                    else if ((autoRow || autoCol) && !(starRow || starCol))
                    {
                        if (!nonStar)
                        {
                            continue;
                        }

                        if (autoRow)
                        {
                            childSizeY = double.PositiveInfinity;
                        }

                        if (autoCol)
                        {
                            childSizeX = double.PositiveInfinity;
                        }
                    }
                    else if (!(starRow || starCol))
                    {
                        if (!nonStar)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (!remainingStar)
                        {
                            continue;
                        }
                    }

                    for (int r = row; r < row + rowspan; r++)
                    {
                        childSizeY += _rowMatrix[r, r].OfferedSize;
                    }

                    for (int c = col; c < col + colspan; c++)
                    {
                        childSizeX += _colMatrix[c, c].OfferedSize;
                    }

                    child.Measure(new Size(childSizeX, childSizeY));
                    Size desired = child.DesiredSize;

                    // Elements distribute their height based on two rules:
                    // 1) Elements with rowspan/colspan == 1 distribute their height first
                    // 2) Everything else distributes in a LIFO manner.
                    // As such, add all UIElements with rowspan/colspan == 1 after the separator in
                    // the list and everything else before it. Then to process, just keep popping
                    // elements off the end of the list.
                    if (!starAuto)
                    {
                        node = new GridNode(_rowMatrix, row + rowspan - 1, row, desired.Height);
                        separatorIndex = sizes.IndexOf(separator);
                        sizes.Insert(node.Row == node.Column ? separatorIndex + 1 : separatorIndex, node);
                    }

                    node = new GridNode(_colMatrix, col + colspan - 1, col, desired.Width);

                    separatorIndex = sizes.IndexOf(separator);
                    sizes.Insert(node.Row == node.Column ? separatorIndex + 1 : separatorIndex, node);
                }

                sizes.Remove(separator);

                while (sizes.Count > 0)
                {
                    node = sizes.Last();
                    node.Matrix[node.Row, node.Column].DesiredSize = Math.Max(node.Matrix[node.Row, node.Column].DesiredSize, node.Size);
                    AllocateDesiredSize(rowCount, colCount);
                    sizes.Remove(node);
                }

                sizes.Add(separator);
            }

            // Once we have measured and distributed all sizes, we have to store
            // the results. Every time we want to expand the rows/cols, this will
            // be used as the baseline.
            SaveMeasureResults();

            sizes.Remove(separator);

            double gridSizeX = 0;
            double gridSizeY = 0;

            for (int c = 0; c < colCount; c++)
            {
                gridSizeX += _colMatrix[c, c].DesiredSize;
            }

            for (int r = 0; r < rowCount; r++)
            {
                gridSizeY += _rowMatrix[r, r].DesiredSize;
            }

            return new Size(gridSizeX, gridSizeY);
        }

        /// <summary>
        /// Arranges the grid's children.
        /// </summary>
        /// <param name="finalSize">The size allocated to the control.</param>
        /// <returns>The space taken.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            int colCount = ColumnDefinitions.Count;
            int rowCount = RowDefinitions.Count;
            int colMatrixDim = _colMatrix.GetLength(0);
            int rowMatrixDim = _rowMatrix.GetLength(0);

            RestoreMeasureResults();

            double totalConsumedX = 0;
            double totalConsumedY = 0;

            for (int c = 0; c < colMatrixDim; c++)
            {
                _colMatrix[c, c].OfferedSize = _colMatrix[c, c].DesiredSize;
                totalConsumedX += _colMatrix[c, c].OfferedSize;
            }

            for (int r = 0; r < rowMatrixDim; r++)
            {
                _rowMatrix[r, r].OfferedSize = _rowMatrix[r, r].DesiredSize;
                totalConsumedY += _rowMatrix[r, r].OfferedSize;
            }

            if (totalConsumedX != finalSize.Width)
            {
                ExpandStarCols(finalSize);
            }

            if (totalConsumedY != finalSize.Height)
            {
                ExpandStarRows(finalSize);
            }

            for (int c = 0; c < colCount; c++)
            {
                ColumnDefinitions[c].ActualWidth = _colMatrix[c, c].OfferedSize;
            }

            for (int r = 0; r < rowCount; r++)
            {
                RowDefinitions[r].ActualHeight = _rowMatrix[r, r].OfferedSize;
            }

            foreach (Control child in Children)
            {
                int col = Math.Min(GetColumn(child), colMatrixDim - 1);
                int row = Math.Min(GetRow(child), rowMatrixDim - 1);
                int colspan = Math.Min(GetColumnSpan(child), colMatrixDim - col);
                int rowspan = Math.Min(GetRowSpan(child), rowMatrixDim - row);

                double childFinalX = 0;
                double childFinalY = 0;
                double childFinalW = 0;
                double childFinalH = 0;

                for (int c = 0; c < col; c++)
                {
                    childFinalX += _colMatrix[c, c].OfferedSize;
                }

                for (int c = col; c < col + colspan; c++)
                {
                    childFinalW += _colMatrix[c, c].OfferedSize;
                }

                for (int r = 0; r < row; r++)
                {
                    childFinalY += _rowMatrix[r, r].OfferedSize;
                }

                for (int r = row; r < row + rowspan; r++)
                {
                    childFinalH += _rowMatrix[r, r].OfferedSize;
                }

                child.Arrange(new Rect(childFinalX, childFinalY, childFinalW, childFinalH));
            }

            return finalSize;
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
                    newsize = Math.Min(newsize, matrix[i, i].Max);
                    assigned |= newsize > segmentSize;
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

        private struct Segment
        {
            public double OriginalSize;
            public double Max;
            public double Min;
            public double DesiredSize;
            public double OfferedSize;
            public double Stars;
            public GridUnitType Type;

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