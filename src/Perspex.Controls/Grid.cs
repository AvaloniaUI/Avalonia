// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Perspex.Collections;

namespace Perspex.Controls
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
          PerspexProperty.RegisterAttached<Grid, Control, int>(
              "Column",
              validate: ValidateColumn);

      /// <summary>
      /// Defines the ColumnSpan attached property.
      /// </summary>
      public static readonly AttachedProperty<int> ColumnSpanProperty =
          PerspexProperty.RegisterAttached<Grid, Control, int>("ColumnSpan", 1);

      /// <summary>
      /// Defines the Row attached property.
      /// </summary>
      public static readonly AttachedProperty<int> RowProperty =
          PerspexProperty.RegisterAttached<Grid, Control, int>(
              "Row",
              validate: ValidateRow);

      /// <summary>
      /// Defines the RowSpan attached property.
      /// </summary>
      public static readonly AttachedProperty<int> RowSpanProperty =
          PerspexProperty.RegisterAttached<Grid, Control, int>("RowSpan", 1);

      private ColumnDefinitions _columnDefinitions;

      private RowDefinitions _rowDefinitions;



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
      public static int GetColumn(PerspexObject element)
      {
         return element.GetValue(ColumnProperty);
      }

      /// <summary>
      /// Gets the value of the ColumnSpan attached property for a control.
      /// </summary>
      /// <param name="element">The control.</param>
      /// <returns>The control's column span.</returns>
      public static int GetColumnSpan(PerspexObject element)
      {
         return element.GetValue(ColumnSpanProperty);
      }

      /// <summary>
      /// Gets the value of the Row attached property for a control.
      /// </summary>
      /// <param name="element">The control.</param>
      /// <returns>The control's row.</returns>
      public static int GetRow(PerspexObject element)
      {
         return element.GetValue(RowProperty);
      }

      /// <summary>
      /// Gets the value of the RowSpan attached property for a control.
      /// </summary>
      /// <param name="element">The control.</param>
      /// <returns>The control's row span.</returns>
      public static int GetRowSpan(PerspexObject element)
      {
         return element.GetValue(RowSpanProperty);
      }

      /// <summary>
      /// Sets the value of the Column attached property for a control.
      /// </summary>
      /// <param name="element">The control.</param>
      /// <param name="value">The column value.</param>
      public static void SetColumn(PerspexObject element, int value)
      {
         element.SetValue(ColumnProperty, value);
      }

      /// <summary>
      /// Sets the value of the ColumnSpan attached property for a control.
      /// </summary>
      /// <param name="element">The control.</param>
      /// <param name="value">The column span value.</param>
      public static void SetColumnSpan(PerspexObject element, int value)
      {
         element.SetValue(ColumnSpanProperty, value);
      }

      /// <summary>
      /// Sets the value of the Row attached property for a control.
      /// </summary>
      /// <param name="element">The control.</param>
      /// <param name="value">The row value.</param>
      public static void SetRow(PerspexObject element, int value)
      {
         element.SetValue(RowProperty, value);
      }

      /// <summary>
      /// Sets the value of the RowSpan attached property for a control.
      /// </summary>
      /// <param name="element">The control.</param>
      /// <param name="value">The row span value.</param>
      public static void SetRowSpan(PerspexObject element, int value)
      {
         element.SetValue(RowSpanProperty, value);
      }

      private static int ValidateColumn(PerspexObject o, int value)
      {
         if (value < 0)
         {
            throw new ArgumentException("Invalid Grid.Column value.");
         }

         return value;
      }

      private static int ValidateRow(PerspexObject o, int value)
      {
         if (value < 0)
         {
            throw new ArgumentException("Invalid Grid.Row value.");
         }

         return value;
      }

      /*
      private Segment[,] _rowMatrix;

      private Segment[,] _colMatrix;
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
        */

      private List<Cell> Group1 = new List<Cell>();
      private List<Cell> Group2 = new List<Cell>();
      private List<Cell> Group3 = new List<Cell>();
      private List<Cell> Group4 = new List<Cell>();

      private const int maxLayoutLoopCount = 5;

      private GridSegment[] _rowSegment;
      private GridSegment[] _colSegment;
      private Cell[] _cells;
      private bool HasGroup3CellsInAutoRows;
      private bool HasStarCellsU;
      private bool HasStarCellsV;

      private bool validationNeeded = true;

      double totalStarsX = 0;
      double totalStarsY = 0;

      double totalHeight;
      double totalWidth;

      private bool definitionsEmpty => (_rowDefinitions?.Count == 0 && _columnDefinitions?.Count == 0);

      protected override Size MeasureOverride(Size availableSize)
      {
         Stopwatch measureTimer = Stopwatch.StartNew();
         if (definitionsEmpty)
         {
            foreach (Control child in Children)
            {
               child.Measure(availableSize);
               totalWidth = Math.Max(child.DesiredSize.Width, totalWidth);
               totalHeight = Math.Max(child.DesiredSize.Height, totalHeight);
            }
         }
         else
         {
            Validate(availableSize);

            MeasureGroup(Group1, false, false);

            //  after Group1 is measured,  only Group3 may have cells belonging to Auto rows.
            bool canresolveStarsV = !HasGroup3CellsInAutoRows;

            if (canresolveStarsV)
            {
               if (HasStarCellsV)
               {
                  ExpandRows(availableSize.Height, totalStarsY);
               }
               MeasureGroup(Group2, false, false);

               if (HasStarCellsU)
               {
                  ExpandCols(availableSize.Width, totalStarsX);
               }
               MeasureGroup(Group3, false, false);
            }
            else
            {
               //  if at least one cell exists in Group2, it must be measured before
               //  StarsU can be resolved.
               bool canResolveStarsU = !(Group2.Count > 0);
               if (canResolveStarsU)
               {
                  if (HasStarCellsU)
                  {
                     ExpandCols(availableSize.Width, totalStarsX);
                  }

                  MeasureGroup(Group3, false, false);
                  if (HasStarCellsV)
                  {
                     ExpandRows(availableSize.Height, totalStarsY);
                  }
               }
               else
               {
                  bool hasDesiredSizeChanged = false;
                  int count = 0;

                  //СashGroup2MinWidth & Group3MinHeight
                  double[] group2MinSizesDoubles = GetMinSizes(Group2, false);
                  double[] group3MinSizesDoubles = GetMinSizes(Group3, true);

                  MeasureGroup(Group2, false, false);

                  do
                  {
                     if (hasDesiredSizeChanged)
                     {
                        // Reset cached Group3Heights
                        ApplyMinSizes(Group3, group3MinSizesDoubles, true);
                     }

                     if (HasStarCellsU)
                     {
                        ExpandCols(availableSize.Width, totalStarsX);
                     }

                     MeasureGroup(Group3, false, false);

                     //Reset cached Group2Widths
                     ApplyMinSizes(Group2, group2MinSizesDoubles, false);

                     if (HasStarCellsV)
                     {
                        ExpandRows(availableSize.Height, totalStarsY);
                     }

                     MeasureGroup(Group2, count == maxLayoutLoopCount, false, out hasDesiredSizeChanged);

                  } while (hasDesiredSizeChanged && ++count <= maxLayoutLoopCount);

               }
            }

            MeasureGroup(Group4, false, false);


            //
            //Recalculate width and height of non star cols/rows to fit to the available width and height 
            //(this is extremely important part because it will influence of the whole Grid size (especially when span is present
            // and user will resize the Grid
            //
            if (HasStarCellsV)
            {
               ExpandRows(availableSize.Height, totalStarsY);
            }
            if (HasStarCellsU)
            {
               ExpandCols(availableSize.Width, totalStarsX);
            }

            totalHeight = CalculateTotalRowSize(false);
            totalWidth = CalculateTotalColSize(false);
         }

         Debug.WriteLine("Grid measure time = " + measureTimer.ElapsedMilliseconds);
         Debug.WriteLine("Total size = " + new Size(totalWidth, totalHeight));
         return new Size(totalWidth, totalHeight);
      }

      protected override Size ArrangeOverride(Size finalSize)
      {
         Stopwatch arrangeTimer = Stopwatch.StartNew();

         if (definitionsEmpty)
         {
            foreach (Control child in Children)
            {
               child.Arrange(new Rect(finalSize));
            }
         }
         else
         {

            Double offsetX = 0.0;
            Double offsetY = 0.0;

            for (int r = 0; r < _rowDefinitions.Count; r++)
            {
               _rowDefinitions[r].ActualHeight = _rowSegment[r].MeasuredSize;
               _rowSegment[r].Offset = offsetY;
               //_rowDefinitions[r].Offset = offsetY;
               offsetY += _rowSegment[r].MeasuredSize;
            }

            for (int r = 0; r < _columnDefinitions.Count; r++)
            {
               _columnDefinitions[r].ActualWidth = _colSegment[r].MeasuredSize;
               _colSegment[r].Offset = offsetX;
               //_columnDefinitions[r].Offset = offsetX;
               offsetX += _colSegment[r].MeasuredSize;
            }

            int index = 0;
            foreach (Control child in Children)
            {
               var cell = _cells[index];

               var childFinalX = _colSegment[cell.colIndex].Offset;
               var childFinalY = _rowSegment[cell.rowIndex].Offset;

               var childFinalW = (_colSegment[cell.colIndex + cell.colSpan - 1].Offset +
                                  _colSegment[cell.colIndex + cell.colSpan - 1].MeasuredSize) -
                                 _colSegment[cell.colIndex].Offset;
               var childFinalH = (_rowSegment[cell.rowIndex + cell.rowSpan - 1].Offset +
                                  _rowSegment[cell.rowIndex + cell.rowSpan - 1].MeasuredSize) -
                                 _rowSegment[cell.rowIndex].Offset;

               child.Arrange(new Rect(childFinalX, childFinalY, childFinalW, childFinalH));
               index++;
            }
         }
         Debug.WriteLine("Grid arrange time = " + arrangeTimer.ElapsedMilliseconds);
         return finalSize;
      }

      private void Validate(Size availableSize)
      {
         if (validationNeeded)
         {
            ValidateCore(availableSize);
         }
      }

      private void ValidateCore(Size availableSize)
      {
         int colCount = ColumnDefinitions.Count;
         int rowCount = RowDefinitions.Count;

         bool emptyRows = rowCount == 0;
         bool emptyCols = colCount == 0;

         totalStarsX = 0;
         totalStarsY = 0;

         if (emptyRows)
         {
            rowCount = 1;
         }

         if (emptyCols)
         {
            colCount = 1;
         }

         ClearSegments();
         CreateSegments(rowCount, colCount);

         bool replaceRowStarsWidthAuto = double.IsPositiveInfinity(availableSize.Height);
         bool replaceColStarsWithAuto = double.IsPositiveInfinity(availableSize.Width);

         if (emptyRows)
         {
            if (!replaceRowStarsWidthAuto)
            {
               _rowSegment[0] = new GridSegment(0, 0, double.PositiveInfinity, GridUnitType.Star) { Stars = 1.0 };
               totalStarsY = 1.0;
            }
            else
            {
               _rowSegment[0] = new GridSegment(0, 0, double.PositiveInfinity, GridUnitType.Auto);
            }
         }
         else
         {
            for (int i = 0; i < rowCount; i++)
            {
               RowDefinition def = RowDefinitions[i];
               GridLength height = def.Height;

               def.ActualHeight = double.PositiveInfinity;
               _rowSegment[i] = new GridSegment(0, def.MinHeight, def.MaxHeight, height.GridUnitType);

               if (height.GridUnitType == GridUnitType.Pixel)
               {
                  _rowSegment[i].MeasuredSize = Clamp(height.Value, def.MinHeight, def.MaxHeight);
                  def.ActualHeight = _rowSegment[i].MeasuredSize;
               }
               else if (height.GridUnitType == GridUnitType.Auto)
               {
                  _rowSegment[i].MeasuredSize = Clamp(0, _rowSegment[i].Min, _rowSegment[i].Max);
               }
               else if (height.GridUnitType == GridUnitType.Star)
               {
                  if (!replaceRowStarsWidthAuto)
                  {
                     _rowSegment[i].Stars = height.Value;
                     totalStarsY += height.Value;
                  }
                  else
                  {
                     _rowSegment[i].Type = GridUnitType.Auto;
                  }
               }
               _rowSegment[i].index = i;
            }
         }

         if (emptyCols)
         {
            if (!replaceColStarsWithAuto)
            {
               _colSegment[0] = new GridSegment(0, 0, double.PositiveInfinity, GridUnitType.Star) { Stars = 1.0 };
               totalStarsX = 1.0;
            }
            else
            {
               _colSegment[0] = new GridSegment(0, 0, double.PositiveInfinity, GridUnitType.Auto);
            }
         }
         else
         {
            for (int i = 0; i < colCount; i++)
            {
               ColumnDefinition coldef = ColumnDefinitions[i];
               GridLength width = coldef.Width;

               coldef.ActualWidth = double.PositiveInfinity;
               _colSegment[i] = new GridSegment(0, coldef.MinWidth, coldef.MaxWidth, width.GridUnitType);

               if (width.GridUnitType == GridUnitType.Pixel)
               {
                  _colSegment[i].MeasuredSize = Clamp(width.Value, _colSegment[i].Min, _colSegment[i].Max);
                  coldef.ActualWidth = _colSegment[i].MeasuredSize;
               }
               else if (width.GridUnitType == GridUnitType.Star)
               {
                  if (!replaceRowStarsWidthAuto)
                  {
                     _colSegment[i].Stars = width.Value;
                     totalStarsX += width.Value;
                  }
                  else
                  {
                     _colSegment[i].Type = GridUnitType.Auto;
                  }
               }
               else if (width.GridUnitType == GridUnitType.Auto)
               {
                  _colSegment[i].MeasuredSize = Clamp(0, _colSegment[i].Min, _colSegment[i].Max);
               }
               _colSegment[i].index = i;
            }
         }

         ClearGroupLists();

         if (_cells != null)
         {
            Array.Clear(_cells, 0, _cells.Length);
         }

         if (Children.Count > 0)
         {
            _cells = new Cell[Children.Count];
            int i = 0;
            foreach (Control child in Children)
            {
               int col = Math.Min(GetColumn(child), colCount - 1);
               int row = Math.Min(GetRow(child), rowCount - 1);
               int colspan = Math.Min(GetColumnSpan(child), colCount - col);
               int rowspan = Math.Min(GetRowSpan(child), rowCount - row);
               _cells[i] = new Cell(_rowSegment[row], _colSegment[col], row, col, rowspan, colspan, i);
               AddToCorrespondingGroup(_cells[i]);
               i++;
            }
         }
         validationNeeded = false;
      }

      private double[] GetMinSizes(List<Cell> cellGroup, bool isRows)
      {
         double[] minSizes = new double[cellGroup.Count];

         for (int i = 0; i < cellGroup.Count; i++)
         {
            if (isRows)
            {
               minSizes[i] = cellGroup[i].MinHeight;
            }
            else
            {
               minSizes[i] = cellGroup[i].MinWidth;
            }
         }

         return minSizes;
      }

      private void ApplyMinSizes(List<Cell> cellGroup, double[] minSizes, bool isRows)
      {
         for (int i = 0; i < minSizes.Length; i++)
         {
            if (isRows)
            {
               cellGroup[i].SetMinHeight(minSizes[i]);
            }
            else
            {
               cellGroup[i].SetMinWidth(minSizes[i]);
            }
         }
      }

      private double GetMeasureSizeForRange(GridSegment[] segment, int startIndex, int count)
      {
         double size = 0;
         int i = startIndex + count - 1;

         do
         {
            size += (segment[i].Type == GridUnitType.Auto) ? segment[i].Min : segment[i].MeasuredSize;
         } while (--i >= startIndex);
         return size;
      }

      private Double CalculateTotalRowSize(bool excludeStars = true)
      {
         double height = 0;
         for (int i = 0; i < _rowSegment.Length; i++)
         {
            if (!excludeStars)
            {
               height += _rowSegment[i].MeasuredSize;
            }
            else
            {
               if (!_rowSegment[i].IsStar)
               {
                  height += _rowSegment[i].MeasuredSize;
               }
            }
         }

         return height;
      }

      private Double CalculateTotalColSize(bool excludeStars = true)
      {
         double width = 0;

         for (int i = 0; i < _colSegment.Length; i++)
         {
            if (!excludeStars)
            {
               width += _colSegment[i].MeasuredSize;
            }
            else
            {
               if (!_colSegment[i].IsStar)
               {
                  width += _colSegment[i].MeasuredSize;
               }
            }
         }

         return width;
      }

      private void AddToCorrespondingGroup(Cell cell)
      {
         if (cell.Group == CellGroup.Group1)
         {
            Group1.Add(cell);
         }
         else if (cell.Group == CellGroup.Group2)
         {
            Group2.Add(cell);
            HasStarCellsV |= cell.rowSegment.IsStar;
         }
         else if (cell.Group == CellGroup.Group3)
         {
            Group3.Add(cell);
            HasGroup3CellsInAutoRows |= cell.rowSegment.IsAuto;

            HasStarCellsU |= cell.colSegment.IsStar;

         }
         else if (cell.Group == CellGroup.Group4)
         {
            Group4.Add(cell);
            HasStarCellsU |= cell.rowSegment.IsStar;
            HasStarCellsV |= cell.colSegment.IsStar;
         }
      }

      private void MeasureGroup(List<Cell> group, bool ignoreDesiredSizeWidth, bool forceInfinitHeight)
      {
         bool fake;
         MeasureGroup(group, ignoreDesiredSizeWidth, forceInfinitHeight, out fake);
      }


      private void MeasureGroup(List<Cell> group, bool ignoreDesiredSizeWidth, bool forceInfinitHeight, out bool hasDesiredWidthChanged)
      {
         Dictionary<SpanKey, Double> spanStore = null;
         hasDesiredWidthChanged = false;
         bool ignoreDesiredSizeHeight = forceInfinitHeight;

         foreach (var cell in group)
         {

            var element = Children[cell.Index];
            double childSizeX = 0.0;
            double childSizeY = 0.0;

            if (cell.rowSegment.IsAuto && !cell.rowSegment.IsStar)
            {
               childSizeY = double.PositiveInfinity;
            }
            else
            {
               childSizeY = GetMeasureSizeForRange(_rowSegment, cell.rowIndex, cell.rowSpan);
            }

            if (cell.colSegment.IsAuto && !cell.colSegment.IsStar)
            {
               childSizeX = double.PositiveInfinity;
            }
            else
            {
               childSizeX = GetMeasureSizeForRange(_colSegment, cell.colIndex, cell.colSpan);
            }

            var oldWidth = element.DesiredSize.Width;
            element.Measure(new Size(childSizeX, childSizeY));
            Size desired = element.DesiredSize;

            hasDesiredWidthChanged |= (oldWidth != desired.Width);

            if (!ignoreDesiredSizeHeight)
            {
               if (cell.rowSpan == 1)
               {
                  var size = Math.Min(desired.Height, _rowSegment[cell.rowIndex].Max);
                  _rowSegment[cell.rowIndex].Min = Clamp(size, _rowSegment[cell.rowIndex].Min,
                     _rowSegment[cell.rowIndex].Max);
                  _rowSegment[cell.rowIndex].MeasuredSize = Math.Max(size, _rowSegment[cell.rowIndex].MeasuredSize);
               }
               else
               {
                  RegisterSpan(
                     ref spanStore,
                     cell.rowIndex,
                     cell.rowSpan,
                     true,
                     element.DesiredSize.Height);
               }
            }

            if (!ignoreDesiredSizeWidth)
            {
               if (cell.colSpan == 1)
               {
                  var size = Math.Min(desired.Width, _colSegment[cell.colIndex].Max);
                  _colSegment[cell.colIndex].Min = Clamp(size, _colSegment[cell.colIndex].Min,
                     _colSegment[cell.colIndex].Max);
                  _colSegment[cell.colIndex].MeasuredSize = Math.Max(size, _colSegment[cell.colIndex].MeasuredSize);
               }
               else
               {
                  RegisterSpan(
                     ref spanStore,
                     cell.colIndex,
                     cell.colSpan,
                     false,
                     element.DesiredSize.Width);
               }
            }
         }

         if (spanStore != null)
         {
            foreach (var entry in spanStore)
            {
               SpanKey key = (SpanKey)entry.Key;
               DistributeSpanSizes(key.IsRow ? _rowSegment : _colSegment, key.Start, key.Count, (double)entry.Value);
            }
            spanStore.Clear();
         }
      }

      private void DistributeSpanSizes(GridSegment[] segment, int start, int count, double size)
      {
         if (size != 0.0)
         {
            GridSegment[] tempDefinitions = new GridSegment[count];
            bool isSizeValid = true;
            int autoCount = 0;
            int end = start + count;
            double rangePrefferedSize = 0.0;
            double rangeMinSize = 0.0;
            double rangeMaxSize = 0.0;
            double maxMaxSize = 0; //  maximum of maximum sizes

            for (int i = start; i < end; i++)
            {
               rangeMinSize += segment[i].Min;
               rangePrefferedSize += segment[i].MeasuredSize;
               rangeMaxSize += segment[i].Max;
               if (maxMaxSize < segment[i].Max) maxMaxSize = segment[i].Max;
               if (segment[i].IsAuto)
               {
                  autoCount++;
               }
               tempDefinitions[i - start] = segment[i];
            }

            //  avoid processing if the range already big enough
            if (size > rangeMinSize)
            {
               if (size <= rangePrefferedSize)
               {
                  double sizeToDistribute;
                  int i;

                  Array.Sort(tempDefinitions, 0, count, s_spanPreferredDistributionOrderComparer);
                  for (i = 0, sizeToDistribute = size; i < autoCount; i++)
                  {
                     sizeToDistribute -= tempDefinitions[i].MeasuredSize;
                  }

                  for (; i < count; ++i)
                  {
                     double newMeasuredSize = sizeToDistribute / (count - i);
                     newMeasuredSize = Clamp(newMeasuredSize, tempDefinitions[i].Min, tempDefinitions[i].Max);
                     if (newMeasuredSize > tempDefinitions[i].Min)
                     {
                        tempDefinitions[i].Min = newMeasuredSize;
                     }
                     newMeasuredSize = Math.Max(newMeasuredSize, tempDefinitions[i].MeasuredSize);
                     tempDefinitions[i].MeasuredSize = Clamp(newMeasuredSize, tempDefinitions[i].Min,
                        tempDefinitions[i].Max);
                     sizeToDistribute -= tempDefinitions[i].Min;
                  }
               }
               else if (size <= rangeMaxSize)
               {
                  double sizeToDistribute;
                  int i = 0;

                  Array.Sort(tempDefinitions, 0, count, s_spanMaxDistributionOrderComparer);
                  for (i = 0, sizeToDistribute = size - rangePrefferedSize; i < count - autoCount; ++i)
                  {
                     double measuredSize = tempDefinitions[i].MeasuredSize;
                     double newMeasuredSize = measuredSize + sizeToDistribute / (count - autoCount - i);
                     newMeasuredSize = Clamp(newMeasuredSize, tempDefinitions[i].Min, tempDefinitions[i].Max);
                     tempDefinitions[i].MeasuredSize = Math.Max(newMeasuredSize, tempDefinitions[i].MeasuredSize);
                     sizeToDistribute -= (tempDefinitions[i].Min - measuredSize);
                  }

                  for (; i < count; ++i)
                  {
                     double measuredSize = tempDefinitions[i].Min;
                     double newMeasuredSize = measuredSize + sizeToDistribute / (count - i);
                     newMeasuredSize = Clamp(newMeasuredSize, tempDefinitions[i].Min, tempDefinitions[i].Max);
                     tempDefinitions[i].MeasuredSize = Math.Max(newMeasuredSize, tempDefinitions[i].MeasuredSize);
                     sizeToDistribute -= (tempDefinitions[i].Min - measuredSize);
                  }
               }
               else
               {
                  //
                  //  requestedSize bigger than max size of the range.
                  //  distribute according to the following logic:
                  //  * for all definitions distribute to equi-size min sizes.
                  //
                  double equalSize = size / count;
                  if (equalSize < maxMaxSize)
                  {
                     //  equi-size is less than maximum of maxSizes.
                     //  in this case distribute so that smaller definitions grow faster than
                     //  bigger ones.
                     double totalRemainingSize = maxMaxSize * count - rangeMaxSize;
                     double sizeToDistribute = size - rangeMaxSize;
                     for (int i = start; i < end; i++)
                     {
                        double deltaSize = (maxMaxSize - segment[i].Max) * sizeToDistribute / totalRemainingSize;
                        segment[i].Min = segment[i].Max + deltaSize;
                     }
                  }
                  else
                  {
                     //  equi-size is greater or equal to maximum of max sizes.
                     //  all definitions receive equalSize as their mim sizes.
                     for (int i = 0; i < end; i++)
                     {
                        segment[i].Min = equalSize;
                     }
                  }
               }

               for (int i = 0; i < tempDefinitions.Length; i++)
               {
                  segment[tempDefinitions[i].index] = tempDefinitions[i];
               }
            }
         }
      }

      /// <summary>
      /// Helper method to register a span information for delayed processing.
      /// </summary>
      /// <param name="store">Reference to a hashtable object used as storage.</param>
      /// <param name="start">Span starting index.</param>
      /// <param name="count">Span count.</param>
      /// <param name="isRow"><c>true</c> if this is a column span. <c>false</c> if this is a row span.</param>
      /// <param name="value">Value to store. If an entry already exists the biggest value is stored.</param>
      private static void RegisterSpan(
          ref Dictionary<SpanKey, Double> store,
          int start,
          int count,
          bool isRow,
          double value)
      {
         if (store == null)
         {
            store = new Dictionary<SpanKey, Double>();
         }

         SpanKey key = new SpanKey(start, count, isRow);
         double tempValue;
         if (store.TryGetValue(key, out tempValue))
         {
            if (value > tempValue)
            {
               store[key] = value;
            }
         }
         else
         {
            store.Add(key, value);
         }
      }

      private void ExpandRows(Double availableHeigth, double starsCount)
      {
         if (starsCount <= 0)
         {
            return;
         }
         var totalSize = CalculateTotalRowSize();
         var remainingHeight = Math.Max(0, availableHeigth - totalSize);
         if (remainingHeight > 0.0)
         {
            double onestarHeight = remainingHeight / starsCount;
            for (int i = 0; i < _rowSegment.Length; i++)
            {
               if (_rowSegment[i].Type == GridUnitType.Star)
               {
                  _rowSegment[i].MeasuredSize = _rowSegment[i].Stars * onestarHeight;
               }
            }
         }
      }

      private void ExpandCols(Double availableWidth, double starsCount)
      {
         if (starsCount <= 0)
         {
            return;
         }
         var totalSize = CalculateTotalColSize();
         var remainingWidth = Math.Max(0, availableWidth - totalSize);
         if (remainingWidth > 0.0)
         {
            double onestarWidth = remainingWidth / starsCount;
            for (int i = 0; i < _colSegment.Length; i++)
            {
               if (_colSegment[i].Type == GridUnitType.Star)
               {
                  _colSegment[i].MeasuredSize = _colSegment[i].Stars * onestarWidth;
               }
            }
         }
      }

      internal static double Clamp(double val, double min, double max)
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

      //Creating grid segment arrays (rows/cols and cells)
      private void CreateSegments(int rowCount, int colCount)
      {
         _rowSegment = new GridSegment[rowCount];
         _colSegment = new GridSegment[colCount];
      }

      private void ClearSegments()
      {
         if (_rowSegment != null)
         {
            Array.Clear(_rowSegment, 0, _rowSegment.Length);
         }
         if (_colSegment != null)
         {
            Array.Clear(_colSegment, 0, _colSegment.Length);
         }
      }

      //Remove all items from the group lists
      private void ClearGroupLists()
      {
         Group1.Clear();
         Group2.Clear();
         Group3.Clear();
         Group4.Clear();
      }



      private struct GridSegment
      {
         public double OriginalSize;
         public double Max;
         public double Min;
         public double MeasuredSize;
         public double Stars;
         public GridUnitType Type;
         public int index;
         public double Offset;

         public Boolean IsAuto => Type == GridUnitType.Auto;

         public Boolean IsStar => Type == GridUnitType.Star;

         public Boolean IsAbsolute => Type == GridUnitType.Pixel;

         public GridSegment(double offeredSize, double min, double max, GridUnitType type)
         {
            index = 0;
            OriginalSize = 0;
            Min = min;
            Max = max;
            MeasuredSize = offeredSize;
            Stars = 0;
            Type = type;
            Offset = 0;
         }
      }

      private struct Cell
      {
         public double Width
         {
            get { return colSegment.MeasuredSize; }
            set { colSegment.MeasuredSize = value; }
         }

         public double Height
         {
            get { return rowSegment.MeasuredSize; }
            set { rowSegment.MeasuredSize = value; }
         }

         public double MaxHeight => rowSegment.Max;
         public double MaxWidth => colSegment.Max;
         public double MinHeight => rowSegment.Min;
         public double MinWidth => colSegment.Min;

         public int rowIndex;
         public int colIndex;
         public int rowSpan;
         public int colSpan;
         public readonly int Index;

         public GridSegment rowSegment;
         public GridSegment colSegment;

         public void SetMinWidth(double size)
         {
            rowSegment.Min = size;
         }

         public void SetMinHeight(double size)
         {
            colSegment.Min = size;
         }

         public CellGroup Group;

         public bool IsPixelPixel => rowSegment.IsAbsolute & colSegment.IsAbsolute;
         public bool IsPixelAuto => rowSegment.IsAbsolute & colSegment.IsAuto;
         public bool IsAutoPixel => rowSegment.IsAuto & colSegment.IsAbsolute;
         public bool IsAutoAuto => rowSegment.IsAuto & colSegment.IsAuto;
         public bool IsStarAuto => rowSegment.IsStar & colSegment.IsAuto;
         public bool IsPixelStar => rowSegment.IsAbsolute & colSegment.IsStar;
         public bool IsAutoStar => rowSegment.IsAuto & colSegment.IsStar;
         public bool IsStarPixel => rowSegment.IsStar & colSegment.IsAbsolute;
         public bool IsStarStar => rowSegment.IsStar & colSegment.IsStar;

         public Cell(GridSegment row, GridSegment col, int rowIndex, int colIndex, int rowSpan, int colSpan, int index)
         {
            rowSegment = row;
            colSegment = col;
            this.rowIndex = rowIndex;
            this.colIndex = colIndex;
            this.rowSpan = rowSpan;
            this.colSpan = colSpan;
            Group = CellGroup.Undefined;
            Index = index;
            SetCategory();
         }

         private void SetCategory()
         {
            if (IsPixelPixel || IsPixelAuto || IsAutoPixel || IsAutoAuto)
            {
               Group = CellGroup.Group1;
            }
            else if (IsStarAuto)
            {
               Group = CellGroup.Group2;
            }
            else if (IsPixelStar || IsAutoStar)
            {
               Group = CellGroup.Group3;
            }
            else
            {
               Group = CellGroup.Group4;
            }
         }
      }

      /// <summary>
      /// Helper class for representing a key for a span in hashtable.
      /// </summary>
      private class SpanKey
      {
         /// <summary>
         /// Constructor.
         /// </summary>
         /// <param name="start">Starting index of the span.</param>
         /// <param name="count">Span count.</param>
         /// <param name="isRow"><c>true</c> for columns; <c>false</c> for rows.</param>
         internal SpanKey(int start, int count, bool isRow)
         {
            Start = start;
            Count = count;
            IsRow = isRow;
         }

         /// <summary>
         /// <see cref="object.GetHashCode"/>
         /// </summary>
         public override int GetHashCode()
         {
            int hash = (Start ^ (Count << 2));

            if (IsRow) hash &= 0x7ffffff;
            else hash |= 0x8000000;

            return (hash);
         }

         /// <summary>
         /// <see cref="object.Equals(object)"/>
         /// </summary>
         public override bool Equals(object obj)
         {
            SpanKey sk = obj as SpanKey;
            return (sk != null
                    && sk.Start == Start
                    && sk.Count == Count
                    && sk.IsRow == IsRow);
         }

         /// <summary>
         /// Returns start index of the span.
         /// </summary>
         internal int Start { get; }

         /// <summary>
         /// Returns span count.
         /// </summary>
         internal int Count { get; }

         /// <summary>
         /// Returns <c>true</c> if this is a column span.
         /// <c>false</c> if this is a row span.
         /// </summary>
         internal bool IsRow { get; }

      }

      private enum CellGroup
      {
         Undefined,
         Group1,
         Group2,
         Group3,
         Group4
      }


      /// <summary>
      /// SpanPreferredDistributionOrderComparer.
      /// </summary>
      private class SpanPreferredDistributionOrderComparer : IComparer
      {
         public int Compare(object x, object y)
         {
            GridSegment definitionX = (GridSegment)x;
            GridSegment definitionY = (GridSegment)y;

            int result;

            if (!CompareNullRefs(definitionX, definitionY, out result))
            {
               if (definitionX.IsAuto)
               {
                  if (definitionY.IsAuto)
                  {
                     result = definitionX.MeasuredSize.CompareTo(definitionY.MeasuredSize);
                  }
                  else
                  {
                     result = -1;
                  }
               }
               else
               {
                  if (definitionY.IsAuto)
                  {
                     result = +1;
                  }
                  else
                  {
                     result = definitionX.MeasuredSize.CompareTo(definitionY.MeasuredSize);
                  }
               }
            }

            return result;
         }
      }

      /// <summary>
      /// SpanMaxDistributionOrderComparer.
      /// </summary>
      private class SpanMaxDistributionOrderComparer : IComparer
      {
         public int Compare(object x, object y)
         {
            GridSegment definitionX = (GridSegment)x;
            GridSegment definitionY = (GridSegment)y;

            int result;

            if (!CompareNullRefs(definitionX, definitionY, out result))
            {
               if (definitionX.IsAuto)
               {
                  if (definitionY.IsAuto)
                  {
                     result = definitionX.Max.CompareTo(definitionY.Max);
                  }
                  else
                  {
                     result = +1;
                  }
               }
               else
               {
                  if (definitionY.IsAuto)
                  {
                     result = -1;
                  }
                  else
                  {
                     result = definitionX.Max.CompareTo(definitionY.Max);
                  }
               }
            }

            return result;
         }
      }

      /// <summary>
      /// Helper for Comparer methods.
      /// </summary>
      /// <returns>
      /// true iff one or both of x and y are null, in which case result holds
      /// the relative sort order.
      /// </returns>
      private static bool CompareNullRefs(object x, object y, out int result)
      {
         result = 2;

         if (x == null)
         {
            if (y == null)
            {
               result = 0;
            }
            else
            {
               result = -1;
            }
         }
         else
         {
            if (y == null)
            {
               result = 1;
            }
         }

         return (result != 2);
      }

      private static readonly IComparer s_spanPreferredDistributionOrderComparer = new SpanPreferredDistributionOrderComparer();
      private static readonly IComparer s_spanMaxDistributionOrderComparer = new SpanMaxDistributionOrderComparer();
   }
}