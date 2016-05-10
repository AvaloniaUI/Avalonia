// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Perspex.Collections;
using Perspex.VisualTree;

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

      private bool ReplaceRowStarsWithAuto;
      private bool ReplaceColStarsWithAuto;

      private const double maxSegmentSize = 1e298; //  used as maximum for clipping star values during normalization

      private double totalHeight;
      private double totalWidth;

      private bool definitionsEmpty
         =>
            ((_rowDefinitions == null || _rowDefinitions.Count == 0) &&
             (_columnDefinitions == null || _columnDefinitions.Count == 0));

      private RowDefinition _rowDefition;
      private ColumnDefinition _colDefinition;

      private static readonly IComparer _spanPreferredDistributionOrderComparer = new SpanPreferredDistributionOrderComparer();
      private static readonly IComparer _spanMaxDistributionOrderComparer = new SpanMaxDistributionOrderComparer();

      /// <summary>
      /// Measures the control and its child elements as part of a layout pass.
      /// </summary>
      /// <param name="availableSize">The size available to the control.</param>
      /// <returns>The desired size for the control.</returns>
      protected override Size MeasureOverride(Size availableSize)
      {
         Stopwatch measureTimer = Stopwatch.StartNew();
         Size totalSize;
         if (definitionsEmpty)
         {
            double w = 0, h = 0;
            foreach (Control child in Children)
            {
               child.Measure(availableSize);
               w = Math.Max(child.DesiredSize.Width, totalWidth);
               h = Math.Max(child.DesiredSize.Height, totalHeight);
            }
            totalSize = new Size(w, h).Constrain(availableSize);
         }
         else
         {
            ReplaceRowStarsWithAuto = double.IsPositiveInfinity(availableSize.Height);
            ReplaceColStarsWithAuto = double.IsPositiveInfinity(availableSize.Width);

            Validate();

            MeasureGroup(Group1, false, false);

            //  after Group1 is measured,  only Group3 may have cells belonging to Auto rows.
            bool canresolveStarsV = !HasGroup3CellsInAutoRows;

            if (canresolveStarsV)
            {
               if (HasStarCellsV)
               {
                  CalculateStarsSize(_rowSegment, availableSize.Height);
               }
               MeasureGroup(Group2, false, false);

               if (HasStarCellsU)
               {
                  CalculateStarsSize(_colSegment, availableSize.Width);
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
                     CalculateStarsSize(_colSegment, availableSize.Width);
                  }

                  MeasureGroup(Group3, false, false);
                  if (HasStarCellsV)
                  {
                     CalculateStarsSize(_rowSegment, availableSize.Height);
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
                        ApplySavedMinSizes(group3MinSizesDoubles, true);
                     }

                     if (HasStarCellsU)
                     {
                        CalculateStarsSize(_colSegment, availableSize.Width);
                     }

                     MeasureGroup(Group3, false, false);

                     //Reset cached Group2Widths
                     ApplySavedMinSizes(group2MinSizesDoubles, false);

                     if (HasStarCellsV)
                     {
                        CalculateStarsSize(_rowSegment, availableSize.Height);
                     }

                     MeasureGroup(Group2, count == maxLayoutLoopCount, false, out hasDesiredSizeChanged);

                  } while (hasDesiredSizeChanged && ++count <= maxLayoutLoopCount);

               }
            }

            MeasureGroup(Group4, false, false);

            totalHeight = CalculateTotalSize(_rowSegment);
            totalWidth = CalculateTotalSize(_colSegment);
            totalSize = new Size(totalWidth, totalHeight);
         }

         //Debug.WriteLine("Grid measure time = " + measureTimer.ElapsedMilliseconds);
         return totalSize;
      }

      /// <summary>
      /// Positions child elements as part of a layout pass.
      /// </summary>
      /// <param name="finalSize">The size available to the control.</param>
      /// <returns>The actual size used.</returns>
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
            CalculateFinalSegmentSize(_rowSegment, finalSize.Height, true);
            CalculateFinalSegmentSize(_colSegment, finalSize.Width, false);

            int index = 0;
            foreach (Control child in Children)
            {
               var cell = _cells[index];

               var childFinalX = _colSegment[cell.ColIndex].Offset;
               var childFinalY = _rowSegment[cell.RowIndex].Offset;

               var childFinalW = (_colSegment[cell.ColIndex + cell.ColSpan - 1].Offset +
                                     _colSegment[cell.ColIndex + cell.ColSpan - 1].SizeCache) -
                                    _colSegment[cell.ColIndex].Offset;

               var childFinalH = (_rowSegment[cell.RowIndex + cell.RowSpan - 1].Offset +
                                     _rowSegment[cell.RowIndex + cell.RowSpan - 1].SizeCache) -
                                    _rowSegment[cell.RowIndex].Offset;

               child.Arrange(new Rect(childFinalX, childFinalY, childFinalW, childFinalH));
               index++;
            }
         }
         //Debug.WriteLine("Grid arrange time = " + arrangeTimer.ElapsedMilliseconds);
         return finalSize;
      }

      private void CalculateFinalSegmentSize(GridSegment[] segment, double finalSize, bool isRow)
      {
         int starDefinitionCount = 0;
         double allPreferredArrangeSize = 0;
         GridSegment[] starDefitions = new GridSegment[segment.Length];
         int[] segmentIndices = new int[segment.Length];
         int nonstarIndex = segment.Length;

         for (int i = 0; i < segment.Length; ++i)
         {
            var currentSegment = segment[i];
            if (currentSegment.IsStar)
            {
               double starValue = currentSegment.Stars;

               if (starValue == 0)
               {
                  currentSegment.MeasuredSize = 0;
                  currentSegment.SizeCache = 0;
               }
               else
               {
                  starValue = Math.Min(starValue, maxSegmentSize);

                  segment[i].MeasuredSize = starValue;
                  double maxSize = Math.Max(segment[i].Min, segment[i].Max);
                  maxSize = Math.Min(maxSize, maxSegmentSize);
                  segment[i].SizeCache = maxSize / starValue;
               }
               starDefitions[starDefinitionCount] = currentSegment;
               segmentIndices[starDefinitionCount] = i;
               starDefinitionCount++;
            }
            else
            {
               double userSize = 0;

               switch (currentSegment.OriginalType)
               {
                  case GridUnitType.Auto:
                     userSize = currentSegment.MinMeasuredSize;
                     break;
                  case GridUnitType.Pixel:
                     userSize = currentSegment.Min;
                     break;
               }

               currentSegment.SizeCache = Math.Max(currentSegment.MinMeasuredSize, Math.Min(userSize, currentSegment.Max));

               allPreferredArrangeSize += currentSegment.SizeCache;
               segmentIndices[--nonstarIndex] = i;
            }
         }

         if (starDefinitionCount > 0)
         {
            double allStarWeights = 0;

            for (int i = starDefinitionCount - 1; i >= 0; --i)
            {
               allStarWeights += starDefitions[i].MeasuredSize;
               starDefitions[i].SizeCache = allStarWeights;
            }

            for (int i = 0; i < starDefinitionCount; ++i)
            {
               double resolvedSize;
               double starValue = starDefitions[i].MeasuredSize;

               if (starValue == 0.0)
               {
                  resolvedSize = starDefitions[i].MinMeasuredSize;
               }
               else
               {
                  double userSize = Math.Max(finalSize - allPreferredArrangeSize, 0.0) * (starValue / starDefitions[i].SizeCache);
                  resolvedSize = Math.Min(userSize, starDefitions[i].Max);
                  resolvedSize = Math.Max(starDefitions[i].MinMeasuredSize, resolvedSize);
               }

               starDefitions[i].SizeCache = resolvedSize;
               allPreferredArrangeSize += resolvedSize;
            }

         }

         if (allPreferredArrangeSize > finalSize && !(allPreferredArrangeSize - finalSize > Double.Epsilon))
         {
            DistributionOrderIndexComparer distributionOrderIndexComparer = new DistributionOrderIndexComparer(segment);
            Array.Sort(segmentIndices, 0, segment.Length, distributionOrderIndexComparer);
            double sizeToDistribute = finalSize - allPreferredArrangeSize;
            for (int i = 0; i < segment.Length; i++)
            {
               int segmentIndex = segmentIndices[i];
               double final = segment[i].SizeCache + (sizeToDistribute / segment.Length - i);
               final = Math.Max(final, segment[segmentIndex].MinMeasuredSize);
               final = Math.Min(final, segment[segmentIndex].SizeCache);

               sizeToDistribute -= (finalSize - segment[segmentIndex].SizeCache);
               segment[segmentIndex].SizeCache = final;

               //double final = segment[i].SizeCache + (sizeToDistribute / segment.Length - i);
               //final = Math.Max(final, segment[i].MinMeasuredSize);
               //final = Math.Min(final, segment[i].SizeCache);

               //sizeToDistribute -= (finalSize - segment[i].SizeCache);
               //segment[i].SizeCache = final;
            }
         }

         segment[0].Offset = 0.0;

         Double offset = 0.0;
         if (isRow && RowDefinitions.Count > 0)
         {
            for (int r = 0; r < _rowDefinitions.Count; r++)
            {
               _rowDefinitions[r].ActualHeight = _rowSegment[r].SizeCache;
               _rowDefinitions[r].MinMeasuredSize = _rowSegment[r].MinMeasuredSize;
               _rowSegment[r].Offset = offset;
               _rowDefinitions[r].Offset = offset;
               offset += _rowSegment[r].SizeCache;
               _rowSegment[r].Min = _rowSegment[r].SizeCache;
            }
         }
         else if (!isRow && ColumnDefinitions.Count > 0)
         {
            for (int r = 0; r < _columnDefinitions.Count; r++)
            {
               _columnDefinitions[r].ActualWidth = _colSegment[r].SizeCache;
               _columnDefinitions[r].MinMeasuredSize = _colSegment[r].MinMeasuredSize;
               _colSegment[r].Offset = offset;
               _columnDefinitions[r].Offset = offset;
               offset += _colSegment[r].SizeCache;
               _colSegment[r].Min = _colSegment[r].SizeCache;
            }
         }
      }

      /// <summary>
      /// DistributionOrderComparer.
      /// </summary>
      private class DistributionOrderIndexComparer : IComparer
      {
         private readonly GridSegment[] definitions;

         internal DistributionOrderIndexComparer(GridSegment[] definitions)
         {
            this.definitions = definitions;
         }

         public int Compare(object x, object y)
         {
            int? indexX = x as int?;
            int? indexY = y as int?;

            GridSegment definitionX = null;
            GridSegment definitionY = null;

            if (indexX != null)
            {
               definitionX = definitions[indexX.Value];
            }
            if (indexY != null)
            {
               definitionY = definitions[indexY.Value];
            }

            int result;

            if (!CompareNullRefs(definitionX, definitionY, out result))
            {
               double xprime = definitionX.SizeCache - definitionX.MinMeasuredSize;
               double yprime = definitionY.SizeCache - definitionY.MinMeasuredSize;
               result = xprime.CompareTo(yprime);
            }

            return result;
         }
      }


      private void Validate()
      {
         int colCount = ColumnDefinitions.Count;
         int rowCount = RowDefinitions.Count;

         bool emptyRows = rowCount == 0;
         bool emptyCols = colCount == 0;

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



         if (emptyRows)
         {
            _rowSegment[0] = new GridSegment(0, 0, double.PositiveInfinity, GridUnitType.Star) { Stars = 1.0 };
            _rowDefition = new RowDefinition();
            _rowSegment[0].MeasuredSize = Double.PositiveInfinity;
            _rowSegment[0].MeasureType = ReplaceRowStarsWithAuto ? InnerGridUnitType.Auto : InnerGridUnitType.Star;
         }
         else
         {
            for (int i = 0; i < rowCount; i++)
            {
               RowDefinition def = RowDefinitions[i];
               GridLength height = def.Height;

               double minSize = def.MinHeight;
               double maxSize = def.MaxHeight;
               double measured = 0;

               GridSegment segment = new GridSegment(0, def.MinHeight, def.MaxHeight, height.GridUnitType);

               switch (def.Height.GridUnitType)
               {
                  case GridUnitType.Pixel:
                     segment.MeasureType = InnerGridUnitType.Pixel;
                     measured = def.Height.Value;
                     minSize = Math.Max(minSize, Math.Min(measured, maxSize));
                     break;
                  case GridUnitType.Auto:
                     segment.MeasureType = InnerGridUnitType.Auto;
                     measured = Double.PositiveInfinity;
                     break;
                  case GridUnitType.Star:
                     segment.MeasureType = ReplaceRowStarsWithAuto ? InnerGridUnitType.Auto : InnerGridUnitType.Star;
                     segment.Stars = height.Value;
                     measured = Double.PositiveInfinity;
                     break;
               }

               segment.Min = minSize;
               segment.MeasuredSize = Math.Max(minSize, Math.Min(measured, maxSize));

               _rowSegment[i] = segment;
            }
         }

         if (emptyCols)
         {
            _colSegment[0] = new GridSegment(0, 0, double.PositiveInfinity, GridUnitType.Star) { Stars = 1.0 };
            _colDefinition = new ColumnDefinition();
            _colSegment[0].MeasuredSize = Double.PositiveInfinity;
            _colSegment[0].MeasureType = ReplaceColStarsWithAuto ? InnerGridUnitType.Auto : InnerGridUnitType.Star;
         }
         else
         {
            for (int i = 0; i < colCount; i++)
            {
               ColumnDefinition def = ColumnDefinitions[i];
               GridLength width = def.Width;

               double minSize = def.MinWidth;
               double maxSize = def.MaxWidth;
               double measured = 0;

               GridSegment segment = new GridSegment(0, def.MinWidth, def.MaxWidth, width.GridUnitType);
               switch (def.Width.GridUnitType)
               {
                  case GridUnitType.Pixel:
                     segment.MeasureType = InnerGridUnitType.Pixel;
                     measured = def.Width.Value;
                     minSize = Math.Max(minSize, Math.Min(measured, maxSize));
                     break;
                  case GridUnitType.Auto:
                     segment.MeasureType = InnerGridUnitType.Auto;
                     measured = Double.PositiveInfinity;
                     break;
                  case GridUnitType.Star:
                     segment.MeasureType = ReplaceColStarsWithAuto ? InnerGridUnitType.Auto : InnerGridUnitType.Star;
                     measured = Double.PositiveInfinity;
                     segment.Stars = width.Value;
                     break;
               }

               segment.Min = minSize;
               segment.MeasuredSize = Math.Max(minSize, Math.Min(measured, maxSize));

               _colSegment[i] = segment;
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

               if (rowspan > 1)
               {
                  for (int j = row; j < row + rowspan; j++)
                  {
                     _rowSegment[j].SpanPresent = true;
                  }
               }

               if (colspan > 1)
               {
                  for (int j = col; j < col + colspan; j++)
                  {
                     _colSegment[col].SpanPresent = true;
                  }
               }

               var cell = new Cell(row, col, rowspan, colspan, i);
               cell.SizeTypeU = GetInnerGridUnitTypeForRange(_colSegment, col, colspan);
               cell.SizeTypeV = GetInnerGridUnitTypeForRange(_rowSegment, row, rowspan);

               HasStarCellsU |= cell.IsStarU;
               HasStarCellsV |= cell.IsStarV;

               if (!cell.IsStarV)
               {
                  if (!cell.IsStarU)
                  {
                     cell.Group = CellGroup.Group1;
                  }
                  else
                  {
                     cell.Group = CellGroup.Group3;
                     HasGroup3CellsInAutoRows |= cell.IsAutoV;
                  }
               }
               else
               {
                  if (cell.IsAutoU && !cell.IsStarU)
                  {
                     cell.Group = CellGroup.Group2;
                  }
                  else
                  {
                     cell.Group = CellGroup.Group4;
                  }
               }

               AddToCorrespondingGroup(cell);
               _cells[i] = cell;
               i++;
            }
         }


         if (!emptyRows && rowCount > 1)
         {
            for (int i = 0; i < rowCount; i++)
            {
               RowDefinition def = RowDefinitions[i];
               GridLength height = def.Height;

               double minSize = def.MinHeight;
               double maxSize = def.MaxHeight;
               double measured = 0;

               GridSegment segment = _rowSegment[i];
               if (segment.SpanPresent)
               {
                  segment.MinMeasuredSize = def.MinMeasuredSize;
               }

               switch (def.Height.GridUnitType)
               {
                  case GridUnitType.Pixel:
                     measured = def.Height.Value;
                     minSize = Math.Max(minSize, Math.Min(measured, maxSize));
                     if (segment.SpanPresent)
                     {
                        minSize = Math.Max(minSize, segment.MinMeasuredSize);
                     }
                     break;
                  case GridUnitType.Auto:
                     minSize = Math.Max(minSize, Math.Min(measured, maxSize));
                     if (segment.SpanPresent)
                     {
                        minSize = Math.Max(minSize, segment.MinMeasuredSize);
                     }
                     break;
                  case GridUnitType.Star:
                     if (segment.SpanPresent)
                     {
                        minSize = Math.Max(minSize, segment.MinMeasuredSize);
                     }
                     if (ReplaceRowStarsWithAuto)
                     {
                        measured = 0;
                     }
                     else
                     {
                        segment.Stars = height.Value;
                     }

                     break;
               }

               segment.Min = minSize;
               segment.MeasuredSize = Math.Max(minSize, Math.Min(measured, maxSize));
            }
         }

         if (!emptyCols && colCount > 1)
         {
            for (int i = 0; i < colCount; i++)
            {
               ColumnDefinition def = ColumnDefinitions[i];
               GridLength width = def.Width;

               double minSize = def.MinWidth;
               double maxSize = def.MaxWidth;
               double measured = 0;

               GridSegment segment = _colSegment[i];
               if (segment.SpanPresent)
               {
                  segment.MinMeasuredSize = def.MinMeasuredSize;
               }

               switch (def.Width.GridUnitType)
               {
                  case GridUnitType.Pixel:
                     measured = def.Width.Value;
                     minSize = Math.Max(minSize, Math.Min(measured, maxSize));
                     if (segment.SpanPresent)
                     {
                        minSize = Math.Max(minSize, segment.MinMeasuredSize);
                     }
                     break;
                  case GridUnitType.Auto:
                     minSize = Math.Max(minSize, Math.Min(measured, maxSize));
                     if (segment.SpanPresent)
                     {
                        minSize = Math.Max(minSize, segment.MinMeasuredSize);
                     }
                     break;
                  case GridUnitType.Star:
                     if (segment.SpanPresent)
                     {
                        minSize = Math.Max(minSize, segment.MinMeasuredSize);
                     }
                     if (ReplaceColStarsWithAuto)
                     {
                        measured = 0;
                     }
                     else
                     {
                        segment.Stars = width.Value;
                     }
                     break;
               }

               segment.Min = minSize;
               segment.MeasuredSize = Math.Max(minSize, Math.Min(measured, maxSize));
            }
         }
      }

      private InnerGridUnitType GetInnerGridUnitTypeForRange(GridSegment[] segment, int start, int count)
      {
         InnerGridUnitType type = InnerGridUnitType.None;
         for (int i = start + count - 1; i >= start; --i)
         {
            type |= segment[i].MeasureType;
         }
         return type;
      }

      private double[] GetMinSizes(List<Cell> cellGroup, bool isRows)
      {
         double[] minSizes = new double[isRows ? _rowSegment.Length : _colSegment.Length];

         for (int i = 0; i < cellGroup.Count; i++)
         {
            if (isRows)
            {
               minSizes[cellGroup[i].RowIndex] = _rowSegment[cellGroup[i].RowIndex].Min;
            }
            else
            {
               minSizes[cellGroup[i].ColIndex] = _colSegment[cellGroup[i].ColIndex].Min;
            }
         }

         return minSizes;
      }

      private void ApplySavedMinSizes(double[] minSizes, bool isRows)
      {
         for (int i = 0; i < minSizes.Length; i++)
         {
            if (isRows)
            {
               _rowSegment[i].Min = minSizes[i];
            }
            else
            {
               _colSegment[i].Min = minSizes[i];
            }
         }
      }

      private double GetMeasureSizeForRange(GridSegment[] segment, int startIndex, int count)
      {
         double size = 0;
         //Go from end of the range to the beginning
         for (int i = startIndex + count - 1; i >= startIndex; --i)
         {
            size += (segment[i].MeasureType == InnerGridUnitType.Auto) ? segment[i].Min : segment[i].MeasuredSize;
         }

         return size;
      }

      private Double CalculateTotalSize(GridSegment[] segment)
      {
         double size = 0;

         for (int i = 0; i < segment.Length; i++)
         {
            size += segment[i].Min;
         }

         return size;
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
            HasStarCellsV |= _rowSegment[cell.RowIndex].IsStar;
         }
         else if (cell.Group == CellGroup.Group3)
         {
            Group3.Add(cell);
            HasGroup3CellsInAutoRows |= _rowSegment[cell.RowIndex].IsAuto;

            HasStarCellsU |= _colSegment[cell.ColIndex].IsStar;

         }
         else if (cell.Group == CellGroup.Group4)
         {
            Group4.Add(cell);
            HasStarCellsU |= _rowSegment[cell.RowIndex].IsStar;
            HasStarCellsV |= _colSegment[cell.ColIndex].IsStar;
         }
      }

      private void MeasureGroup(List<Cell> group, bool ignoreDesiredSizeWidth, bool forceInfinitHeight)
      {
         bool fake;
         MeasureGroup(group, ignoreDesiredSizeWidth, forceInfinitHeight, out fake);
      }

      private void MeasureGroup(List<Cell> group, bool ignoreDesiredSizeWidth, bool forceInfinitHeight,
         out bool hasDesiredWidthChanged)
      {
         Dictionary<SpanKey, Double> spanStore = null;
         hasDesiredWidthChanged = false;
         bool ignoreDesiredSizeHeight = forceInfinitHeight;

         foreach (var cell in group)
         {
            var element = Children[cell.ChildIndex];
            double childSizeX;
            double childSizeY;
            GridSegment row = _rowSegment[cell.RowIndex];
            GridSegment column = _colSegment[cell.ColIndex];
            ColumnDefinition columnDef = null;
            RowDefinition rowDef = null;
            rowDef = _rowDefinitions.Count > 0 ? _rowDefinitions[cell.RowIndex] : _rowDefition;
            columnDef = _columnDefinitions.Count > 0 ? _columnDefinitions[cell.ColIndex] : _colDefinition;

            if (cell.IsAutoV && !cell.IsStarV)
            {
               childSizeY = double.PositiveInfinity;
            }
            else
            {
               childSizeY = GetMeasureSizeForRange(_rowSegment, cell.RowIndex, cell.RowSpan);
            }

            if (forceInfinitHeight)
            {
               childSizeY = Double.PositiveInfinity;
            }

            if (cell.IsAutoU && !cell.IsStarU)
            {
               childSizeX = double.PositiveInfinity;
            }
            else
            {
               childSizeX = GetMeasureSizeForRange(_colSegment, cell.ColIndex, cell.ColSpan);
            }

            var oldWidth = element.DesiredSize.Width;
            element.Measure(new Size(childSizeX, childSizeY));
            Size desired = element.DesiredSize;

            hasDesiredWidthChanged |= (oldWidth != desired.Width);

            if (!ignoreDesiredSizeHeight)
            {
               if (cell.RowSpan == 1)
               {
                  row.Min = Clamp(desired.Height, row.Min, row.Max);
                  row.MinMeasuredSize = Math.Max(Clamp(desired.Height, rowDef.MinHeight, rowDef.MaxHeight), row.MinMeasuredSize);
               }
               else
               {
                  RegisterSpan(
                     ref spanStore,
                     cell.RowIndex,
                     cell.RowSpan,
                     true,
                     desired.Height);
               }
            }

            if (!ignoreDesiredSizeWidth)
            {
               if (cell.ColSpan == 1)
               {
                  column.Min = Clamp(desired.Width, column.Min, column.Max);
                  column.MinMeasuredSize = Math.Max(Clamp(desired.Width, columnDef.MinWidth, columnDef.MaxWidth), column.MinMeasuredSize);
               }
               else
               {
                  RegisterSpan(
                     ref spanStore,
                     cell.ColIndex,
                     cell.ColSpan,
                     false,
                     desired.Width);
               }
            }
         }

         if (spanStore != null)
         {
            foreach (var entry in spanStore)
            {
               SpanKey key = entry.Key;
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
            int autoCount = 0;
            int end = start + count;
            double rangePreferredSize = 0.0;
            double rangeMinSize = 0.0;
            double rangeMaxSize = 0.0;
            double maxMaxSize = 0; //  maximum of maximum sizes

            for (int i = start; i < end; i++)
            {
               double minSize = segment[i].Min;
               double preferred = segment[i].MeasuredSize;
               double maxSize = Math.Max(segment[i].Max, minSize);

               rangeMinSize += minSize;
               rangePreferredSize += preferred;
               rangeMaxSize += maxSize;

               segment[i].SizeCache = maxSize;

               if (maxMaxSize < maxSize) maxMaxSize = maxSize;
               if (segment[i].IsAuto)
               {
                  autoCount++;
               }
               tempDefinitions[i - start] = segment[i];
            }

            //  avoid processing if the range already big enough
            if (size > rangeMinSize)
            {
               if (size <= rangePreferredSize)
               {
                  double sizeToDistribute;
                  int i;

                  Array.Sort(tempDefinitions, 0, count, _spanPreferredDistributionOrderComparer);
                  for (i = 0, sizeToDistribute = size; i < autoCount; ++i)
                  {
                     sizeToDistribute -= tempDefinitions[i].Min;
                  }

                  for (; i < count; ++i)
                  {
                     double newMeasuredSize = Math.Min(sizeToDistribute / (count - i), tempDefinitions[i].MinMeasuredSize);
                     if (newMeasuredSize > tempDefinitions[i].Min)
                     {
                        tempDefinitions[i].Min = newMeasuredSize;
                     }
                     sizeToDistribute -= tempDefinitions[i].Min;
                  }
               }
               else if (size <= rangeMaxSize)
               {
                  double sizeToDistribute;
                  int i;

                  Array.Sort(tempDefinitions, 0, count, _spanMaxDistributionOrderComparer);

                  for (i = 0, sizeToDistribute = size - rangePreferredSize; i < count - autoCount; ++i)
                  {
                     double measuredSize = tempDefinitions[i].MeasuredSize;
                     double newMeasuredSize = measuredSize + sizeToDistribute / (count - autoCount - i);
                     newMeasuredSize = Clamp(newMeasuredSize, tempDefinitions[i].Min, tempDefinitions[i].Max);
                     tempDefinitions[i].Min = Math.Max(newMeasuredSize, tempDefinitions[i].MeasuredSize);
                     sizeToDistribute -= (tempDefinitions[i].Min - measuredSize);
                  }

                  for (; i < count; ++i)
                  {
                     double measuredSize = tempDefinitions[i].Min;
                     double newMeasuredSize = measuredSize + sizeToDistribute / (count - i);
                     newMeasuredSize = Clamp(newMeasuredSize, tempDefinitions[i].Min, tempDefinitions[i].Max);
                     tempDefinitions[i].Min = Math.Max(newMeasuredSize, tempDefinitions[i].MeasuredSize);
                     //tempDefinitions[i].MinMeasuredSize = Math.Max(newMeasuredSize, tempDefinitions[i].MeasuredSize);
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
                     for (int i = 0; i < count; ++i)
                     {
                        double deltaSize = (maxMaxSize - tempDefinitions[i].Max) * sizeToDistribute / totalRemainingSize;
                        tempDefinitions[i].Min = tempDefinitions[i].Max + deltaSize;
                     }
                  }
                  else
                  {
                     //  equi-size is greater or equal to maximum of max sizes.
                     //  all definitions receive equalSize as their mim sizes.
                     for (int i = 0; i < count; i++)
                     {
                        tempDefinitions[i].Min = equalSize;
                     }
                  }
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

      private void CalculateStarsSize(GridSegment[] segment, double availableSize)
      {
         int starDefinitionCount = 0;
         GridSegment[] starSegments = new GridSegment[segment.Length];
         double takenSize = 0;
         for (int i = 0; i < segment.Length; ++i)
         {
            switch (segment[i].OriginalType)
            {
               case GridUnitType.Auto:
                  takenSize += segment[i].Min;
                  break;
               case (GridUnitType.Pixel):
                  takenSize += segment[i].MeasuredSize;
                  break;
               case (GridUnitType.Star):
                  {
                     starSegments[starDefinitionCount++] = segment[i];

                     double starValue = segment[i].Stars;
                     if (starValue <= 0.0)
                     {
                        segment[i].MeasuredSize = 0;
                        segment[i].SizeCache = 0;
                     }
                     else
                     {
                        //Clamp starValue by maximum possible size to avoid Infinity value
                        starValue = Math.Min(starValue, maxSegmentSize);

                        //  Note: normalized star value is temporary cached into MeasureSize
                        segment[i].MeasuredSize = starValue;
                        double maxSize = Math.Max(segment[i].Min, segment[i].Max);
                        maxSize = Math.Min(maxSize, maxSegmentSize);
                        segment[i].SizeCache = maxSize / starValue;
                     }
                  }
                  break;
            }
         }

         if (starDefinitionCount > 0)
         {
            double allStarWeights = 0;

            for (int i = starDefinitionCount - 1; i >= 0; --i)
            {
               allStarWeights += starSegments[i].MeasuredSize;
               starSegments[i].SizeCache = allStarWeights;
            }

            for (int i = 0; i < starDefinitionCount; ++i)
            {
               double resolvedSize;
               double starValue = starSegments[i].MeasuredSize;

               if (starValue == 0.0)
               {
                  resolvedSize = starSegments[i].MinMeasuredSize;
               }
               else
               {
                  double userSize = Math.Max(availableSize - takenSize, 0.0) * (starValue / starSegments[i].SizeCache);
                  resolvedSize = Math.Min(userSize, starSegments[i].Max);
                  resolvedSize = Math.Max(starSegments[i].MinMeasuredSize, resolvedSize);
               }

               starSegments[i].MeasuredSize = resolvedSize;
               takenSize += resolvedSize;
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

      [Flags]
      private enum InnerGridUnitType
      {
         None = 0,
         Pixel = 1,
         Star = 2,
         Auto = 4
      }



      private class GridSegment
      {
         public double MinMeasuredSize;
         public double SizeCache;
         public double Max;
         public double Min;
         public double MeasuredSize;
         public double Stars;
         public GridUnitType OriginalType { get; }
         public InnerGridUnitType MeasureType { get; set; }
         public double Offset;
         public bool SpanPresent = false;

         public Boolean IsAuto => OriginalType == GridUnitType.Auto;

         public Boolean IsStar => OriginalType == GridUnitType.Star;

         public Boolean IsAbsolute => OriginalType == GridUnitType.Pixel;

         public GridSegment(double offeredSize, double min, double max, GridUnitType originalType)
         {
            SizeCache = 0;
            Min = min;
            Max = max;
            MeasuredSize = offeredSize;
            Stars = 0;
            OriginalType = originalType;
            Offset = 0;
         }
      }

      private struct Cell
      {
         public readonly int RowIndex;
         public readonly int ColIndex;
         public readonly int RowSpan;
         public readonly int ColSpan;
         public readonly int ChildIndex;

         //Contains combination of types by row and column including span definitions
         public InnerGridUnitType SizeTypeU;
         public InnerGridUnitType SizeTypeV;

         public bool IsStarU => ((SizeTypeU & InnerGridUnitType.Star) != 0);
         public bool IsStarV => ((SizeTypeV & InnerGridUnitType.Star) != 0);
         public bool IsAutoU => ((SizeTypeU & InnerGridUnitType.Auto) != 0);
         public bool IsAutoV => ((SizeTypeV & InnerGridUnitType.Auto) != 0);

         public CellGroup Group;

         public Cell(int rowIndex, int colIndex, int rowSpan, int colSpan, int childIndex)
         {
            SizeTypeU = InnerGridUnitType.None;
            SizeTypeV = InnerGridUnitType.None;
            this.RowIndex = rowIndex;
            this.ColIndex = colIndex;
            this.RowSpan = rowSpan;
            this.ColSpan = colSpan;
            Group = CellGroup.Undefined;
            ChildIndex = childIndex;
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
         /// <param name="isRow"><c>true</c> for rows; <c>false</c> for columns.</param>
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
                     result = definitionX.Min.CompareTo(definitionY.Min);
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
                     result = definitionX.SizeCache.CompareTo(definitionY.SizeCache);
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
                     result = definitionX.SizeCache.CompareTo(definitionY.SizeCache);
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
   }
}