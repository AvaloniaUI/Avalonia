// -----------------------------------------------------------------------
// <copyright file="Grid.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Grid : Panel
    {
        public static readonly PerspexProperty<int> ColumnProperty =
            PerspexProperty.RegisterAttached<Grid, Control, int>("Column");

        public static readonly PerspexProperty<int> ColumnSpanProperty =
            PerspexProperty.RegisterAttached<Grid, Control, int>("ColumnSpan", 1);

        public static readonly PerspexProperty<int> RowProperty =
            PerspexProperty.RegisterAttached<Grid, Control, int>("Row");

        public static readonly PerspexProperty<int> RowSpanProperty =
            PerspexProperty.RegisterAttached<Grid, Control, int>("RowSpan", 1);

        private ColumnDefinitions columnDefinitions;

        private RowDefinitions rowDefinitions;

        private Segment[,] rowMatrix;

        private Segment[,] colMatrix;

        public Grid()
        {
        }

        public ColumnDefinitions ColumnDefinitions
        {
            get
            {
                if (this.columnDefinitions == null)
                {
                    this.ColumnDefinitions = new ColumnDefinitions();
                }

                return this.columnDefinitions;
            }

            set
            {
                if (this.columnDefinitions != null)
                {
                    throw new NotSupportedException("Reassigning ColumnDefinitions not yet implemented.");
                }

                this.columnDefinitions = value;
                this.columnDefinitions.TrackItemPropertyChanged(_ => this.InvalidateMeasure());
            }
        }

        public RowDefinitions RowDefinitions
        {
            get
            {
                if (this.rowDefinitions == null)
                {
                    this.RowDefinitions = new RowDefinitions();
                }

                return this.rowDefinitions;
            }

            set
            {
                if (this.rowDefinitions != null)
                {
                    throw new NotSupportedException("Reassigning RowDefinitions not yet implemented.");
                }

                this.rowDefinitions = value;
                this.rowDefinitions.TrackItemPropertyChanged(_ => this.InvalidateMeasure());
            }
        }

        public static int GetColumn(PerspexObject element)
        {
            return element.GetValue(ColumnProperty);
        }

        public static int GetColumnSpan(PerspexObject element)
        {
            return element.GetValue(ColumnSpanProperty);
        }

        public static int GetRow(PerspexObject element)
        {
            return element.GetValue(RowProperty);
        }

        public static int GetRowSpan(PerspexObject element)
        {
            return element.GetValue(RowSpanProperty);
        }

        public static void SetColumn(PerspexObject element, int value)
        {
            element.SetValue(ColumnProperty, value);
        }

        public static void SetColumnSpan(PerspexObject element, int value)
        {
            element.SetValue(ColumnSpanProperty, value);
        }

        public static void SetRow(PerspexObject element, int value)
        {
            element.SetValue(RowProperty, value);
        }

        public static void SetRowSpan(PerspexObject element, int value)
        {
            element.SetValue(RowSpanProperty, value);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            Size totalSize = constraint;
            int colCount = this.ColumnDefinitions.Count;
            int rowCount = this.RowDefinitions.Count;
            double totalStarsX = 0;
            double totalStarsY = 0;
            bool emptyRows = rowCount == 0;
            bool emptyCols = colCount == 0;
            bool hasChildren = this.Children.Count > 0;

            if (emptyRows)
            {
                rowCount = 1;
            }

            if (emptyCols)
            {
                colCount = 1;
            }

            this.CreateMatrices(rowCount, colCount);

            if (emptyRows)
            {
                this.rowMatrix[0, 0] = new Segment(0, 0, double.PositiveInfinity, GridUnitType.Star);
                this.rowMatrix[0, 0].Stars = 1.0;
                totalStarsY += 1.0;
            }
            else
            {
                for (int i = 0; i < rowCount; i++)
                {
                    RowDefinition rowdef = this.RowDefinitions[i];
                    GridLength height = rowdef.Height;

                    rowdef.ActualHeight = double.PositiveInfinity;
                    this.rowMatrix[i, i] = new Segment(0, rowdef.MinHeight, rowdef.MaxHeight, height.GridUnitType);

                    if (height.GridUnitType == GridUnitType.Pixel)
                    {
                        this.rowMatrix[i, i].OfferedSize = Clamp(height.Value, this.rowMatrix[i, i].Min, this.rowMatrix[i, i].Max);
                        this.rowMatrix[i, i].DesiredSize = this.rowMatrix[i, i].OfferedSize;
                        rowdef.ActualHeight = this.rowMatrix[i, i].OfferedSize;
                    }
                    else if (height.GridUnitType == GridUnitType.Star)
                    {
                        this.rowMatrix[i, i].Stars = height.Value;
                        totalStarsY += height.Value;
                    }
                    else if (height.GridUnitType == GridUnitType.Auto)
                    {
                        this.rowMatrix[i, i].OfferedSize = Clamp(0, this.rowMatrix[i, i].Min, this.rowMatrix[i, i].Max);
                        this.rowMatrix[i, i].DesiredSize = this.rowMatrix[i, i].OfferedSize;
                    }
                }
            }

            if (emptyCols)
            {
                this.colMatrix[0, 0] = new Segment(0, 0, double.PositiveInfinity, GridUnitType.Star);
                this.colMatrix[0, 0].Stars = 1.0;
                totalStarsX += 1.0;
            }
            else
            {
                for (int i = 0; i < colCount; i++)
                {
                    ColumnDefinition coldef = this.ColumnDefinitions[i];
                    GridLength width = coldef.Width;

                    coldef.ActualWidth = double.PositiveInfinity;
                    this.colMatrix[i, i] = new Segment(0, coldef.MinWidth, coldef.MaxWidth, width.GridUnitType);

                    if (width.GridUnitType == GridUnitType.Pixel)
                    {
                        this.colMatrix[i, i].OfferedSize = Clamp(width.Value, this.colMatrix[i, i].Min, this.colMatrix[i, i].Max);
                        this.colMatrix[i, i].DesiredSize = this.colMatrix[i, i].OfferedSize;
                        coldef.ActualWidth = this.colMatrix[i, i].OfferedSize;
                    }
                    else if (width.GridUnitType == GridUnitType.Star)
                    {
                        this.colMatrix[i, i].Stars = width.Value;
                        totalStarsX += width.Value;
                    }
                    else if (width.GridUnitType == GridUnitType.Auto)
                    {
                        this.colMatrix[i, i].OfferedSize = Clamp(0, this.colMatrix[i, i].Min, this.colMatrix[i, i].Max);
                        this.colMatrix[i, i].DesiredSize = this.colMatrix[i, i].OfferedSize;
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
            GridWalker gridWalker = new GridWalker(this, this.rowMatrix, this.colMatrix);

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
                    this.ExpandStarCols(totalSize);
                    this.ExpandStarRows(totalSize);
                }

                foreach (Control child in this.Children)
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
                        starRow |= this.rowMatrix[r, r].Type == GridUnitType.Star;
                        autoRow |= this.rowMatrix[r, r].Type == GridUnitType.Auto;
                    }

                    for (int c = col; c < col + colspan; c++)
                    {
                        starCol |= this.colMatrix[c, c].Type == GridUnitType.Star;
                        autoCol |= this.colMatrix[c, c].Type == GridUnitType.Auto;
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
                        childSizeY += this.rowMatrix[r, r].OfferedSize;
                    }

                    for (int c = col; c < col + colspan; c++)
                    {
                        childSizeX += this.colMatrix[c, c].OfferedSize;
                    }

                    child.Measure(new Size(childSizeX, childSizeY));
                    Size desired = child.DesiredSize.Value;

                    // Elements distribute their height based on two rules:
                    // 1) Elements with rowspan/colspan == 1 distribute their height first
                    // 2) Everything else distributes in a LIFO manner.
                    // As such, add all UIElements with rowspan/colspan == 1 after the separator in
                    // the list and everything else before it. Then to process, just keep popping
                    // elements off the end of the list.
                    if (!starAuto)
                    {
                        node = new GridNode(this.rowMatrix, row + rowspan - 1, row, desired.Height);
                        separatorIndex = sizes.IndexOf(separator);
                        sizes.Insert(node.Row == node.Column ? separatorIndex + 1 : separatorIndex, node);
                    }

                    node = new GridNode(this.colMatrix, col + colspan - 1, col, desired.Width);

                    separatorIndex = sizes.IndexOf(separator);
                    sizes.Insert(node.Row == node.Column ? separatorIndex + 1 : separatorIndex, node);
                }

                sizes.Remove(separator);

                while (sizes.Count > 0)
                {
                    node = sizes.Last();
                    node.Matrix[node.Row, node.Column].DesiredSize = Math.Max(node.Matrix[node.Row, node.Column].DesiredSize, node.Size);
                    this.AllocateDesiredSize(rowCount, colCount);
                    sizes.Remove(node);
                }

                sizes.Add(separator);
            }

            // Once we have measured and distributed all sizes, we have to store
            // the results. Every time we want to expand the rows/cols, this will
            // be used as the baseline.
            this.SaveMeasureResults();

            sizes.Remove(separator);

            double gridSizeX = 0;
            double gridSizeY = 0;
            
            for (int c = 0; c < colCount; c++)
            {
                gridSizeX += this.colMatrix[c, c].DesiredSize;
            }

            for (int r = 0; r < rowCount; r++)
            {
                gridSizeY += this.rowMatrix[r, r].DesiredSize;
            }

            return new Size(gridSizeX, gridSizeY);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            int colCount = this.ColumnDefinitions.Count;
            int rowCount = this.RowDefinitions.Count;
            int colMatrixDim = this.colMatrix.GetUpperBound(0) + 1;
            int rowMatrixDim = this.rowMatrix.GetUpperBound(0) + 1;

            this.RestoreMeasureResults();

            double totalConsumedX = 0;
            double totalConsumedY = 0;

            for (int c = 0; c < colMatrixDim; c++) 
            {
                this.colMatrix[c, c].OfferedSize = this.colMatrix[c, c].DesiredSize;
                totalConsumedX += this.colMatrix[c, c].OfferedSize;
            }

            for (int r = 0; r < rowMatrixDim; r++) 
            {
                this.rowMatrix[r, r].OfferedSize = this.rowMatrix[r, r].DesiredSize;
                totalConsumedY += this.rowMatrix[r, r].OfferedSize;
            }

            if (totalConsumedX != finalSize.Width)
            {
                this.ExpandStarCols(finalSize);
            }

            if (totalConsumedY != finalSize.Height)
            {
                this.ExpandStarRows(finalSize);
            }

            for (int c = 0; c < colCount; c++)
            {
                this.ColumnDefinitions[c].ActualWidth = this.colMatrix[c, c].OfferedSize;
            }

            for (int r = 0; r < rowCount; r++)
            {
                this.RowDefinitions[r].ActualHeight = this.rowMatrix[r, r].OfferedSize;
            }

            foreach (Control child in this.Children)
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
                    childFinalX += this.colMatrix[c, c].OfferedSize;
                }

                for (int c = col; c < col + colspan; c++)
                {
                    childFinalW += this.colMatrix[c, c].OfferedSize;
                }

                for (int r = 0; r < row; r++)
                {
                    childFinalY += this.rowMatrix[r, r].OfferedSize;
                }

                for (int r = row; r < row + rowspan; r++)
                {
                    childFinalH += this.rowMatrix[r, r].OfferedSize;
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
            if (this.rowMatrix == null || this.colMatrix == null ||
                this.rowMatrix.GetUpperBound(0) != rowCount - 1 ||
                this.colMatrix.GetUpperBound(0) != colCount - 1)
            {
                this.rowMatrix = new Segment[rowCount, rowCount];
                this.colMatrix = new Segment[colCount, colCount];
            }
        }

        private void ExpandStarCols(Size availableSize)
        {
            int columnsCount = this.ColumnDefinitions.Count;
            double width = availableSize.Width;

            for (int i = 0; i < this.colMatrix.GetUpperBound(0) + 1; i++) 
            {
                if (this.colMatrix[i, i].Type == GridUnitType.Star)
                {
                    this.colMatrix[i, i].OfferedSize = 0;
                }
                else
                {
                    width = Math.Max(availableSize.Width - this.colMatrix[i, i].OfferedSize, 0);
                }
            }

            this.AssignSize(this.colMatrix, 0, this.colMatrix.GetUpperBound(0), ref width, GridUnitType.Star, false);
            width = Math.Max(0, width);

            if (columnsCount > 0) 
            {
                for (int i = 0; i < this.colMatrix.GetUpperBound(0) + 1; i++)
                {
                    if (this.colMatrix[i, i].Type == GridUnitType.Star)
                    {
                        this.ColumnDefinitions[i].ActualWidth = this.colMatrix[i, i].OfferedSize;
                    }
                }
            }
        }

        private void ExpandStarRows(Size availableSize)
        {
            int rowCount = this.RowDefinitions.Count;
            double height = availableSize.Height;

            // When expanding star rows, we need to zero out their height before
            // calling AssignSize. AssignSize takes care of distributing the 
            // available size when there are Mins and Maxs applied.
            for (int i = 0; i < this.rowMatrix.GetUpperBound(0) + 1; i++) 
            {
                if (this.rowMatrix[i, i].Type == GridUnitType.Star)
                {
                    this.rowMatrix[i, i].OfferedSize = 0.0;
                }
                else
                {
                    height = Math.Max(availableSize.Height - this.rowMatrix[i, i].OfferedSize, 0);
                }
            }

            this.AssignSize(this.rowMatrix, 0, this.rowMatrix.GetUpperBound(0), ref height, GridUnitType.Star, false);

            if (rowCount > 0) 
            {
                for (int i = 0; i < this.rowMatrix.GetUpperBound(0) + 1; i++)
                {
                    if (this.rowMatrix[i, i].Type == GridUnitType.Star)
                    {
                        this.RowDefinitions[i].ActualHeight = this.rowMatrix[i, i].OfferedSize;
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
                Segment[,] matrix = i == 0 ? this.rowMatrix : this.colMatrix;
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
                                this.AssignSize(matrix, col, row, ref additional, GridUnitType.Star, true);
                            } 
                            else 
                            {
                                this.AssignSize(matrix, col, row, ref additional, GridUnitType.Pixel, true);
                                this.AssignSize(matrix, col, row, ref additional, GridUnitType.Auto, true);
                            }
                        }
                    }
                }
            }

            for (int r = 0; r < this.rowMatrix.GetUpperBound(0) + 1; r++)
            {
                this.rowMatrix[r, r].OfferedSize = this.rowMatrix[r, r].DesiredSize;
            }

            for (int c = 0; c < this.colMatrix.GetUpperBound(0) + 1; c++)
            {
                this.colMatrix[c, c].OfferedSize = this.colMatrix[c, c].DesiredSize;
            }
        }

        private void SaveMeasureResults()
        {
            for (int i = 0; i < this.rowMatrix.GetUpperBound(0) + 1; i++)
            {
                for (int j = 0; j < this.rowMatrix.GetUpperBound(0) + 1; j++)
                {
                    this.rowMatrix[i, j].OriginalSize = this.rowMatrix[i, j].OfferedSize;
                }
            }

            for (int i = 0; i < this.colMatrix.GetUpperBound(0); i++)
            {
                for (int j = 0; j < this.colMatrix.GetUpperBound(0); j++)
                {
                    this.colMatrix[i, j].OriginalSize = this.colMatrix[i, j].OfferedSize;
                }
            }
        }

        private void RestoreMeasureResults()
        {
            for (int i = 0; i < this.rowMatrix.GetUpperBound(0) + 1; i++)
            {
                for (int j = 0; j < this.rowMatrix.GetUpperBound(0) + 1; j++)
                {
                    this.rowMatrix[i, j].OfferedSize = this.rowMatrix[i, j].OriginalSize;
                }
            }

            for (int i = 0; i < this.colMatrix.GetUpperBound(0) + 1; i++)
            {
                for (int j = 0; j < this.colMatrix.GetUpperBound(0) + 1; j++)
                {
                    this.colMatrix[i, j].OfferedSize = this.colMatrix[i, j].OriginalSize;
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
                this.OriginalSize = 0;
                this.Min = min;
                this.Max = max;
                this.DesiredSize = 0;
                this.OfferedSize = offeredSize;
                this.Stars = 0;
                this.Type = type;
            }

            public void Init(double offeredSize, double min, double max, GridUnitType type)
            {
                this.OfferedSize = offeredSize;
                this.Min = min;
                this.Max = max;
                this.Type = type;
            }
        }

        private struct GridNode
        {
            public int Row;
            public int Column;
            public double Size;
            public Segment[,] Matrix;

            public GridNode(Segment[,] matrix, int row, int col, double size)
            {
                this.Matrix = matrix;
                this.Row = row;
                this.Column = col;
                this.Size = size;
            }
        }

        private class GridWalker 
        {
            public GridWalker(Grid grid, Segment[,] rowMatrix, Segment[,] colMatrix)
            {
                foreach (Control child in grid.Children)
                {
                    bool starCol = false;
                    bool starRow = false;
                    bool autoCol = false;
                    bool autoRow = false;

                    int col = Math.Min(Grid.GetColumn(child), colMatrix.GetUpperBound(0));
                    int row = Math.Min(Grid.GetRow(child), rowMatrix.GetUpperBound(0));
                    int colspan = Math.Min(Grid.GetColumnSpan(child), colMatrix.GetUpperBound(0));
                    int rowspan = Math.Min(Grid.GetRowSpan(child), rowMatrix.GetUpperBound(0));

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

                    this.HasAutoAuto |= autoRow && autoCol && !starRow && !starCol;
                    this.HasStarAuto |= starRow && autoCol;
                    this.HasAutoStar |= autoRow && starCol;
                }
            }

            public bool HasAutoAuto { get; private set; }

            public bool HasStarAuto { get; private set; }
            
            public bool HasAutoStar { get; private set; }
        }
    }
}
