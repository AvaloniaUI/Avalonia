// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Perspex.Controls.Primitives;
using Perspex.Input;
using Perspex.VisualTree;

namespace Perspex.Controls
{
   public enum GridResizeBehavior
   {
      PreviousAndNext = 1,
      PreviousAndCurrent = 2,
      CurrentAndNext = 3
   }

   /// <summary>
   /// Grid resize scheme
   /// </summary>
   public enum GridResizeScheme
   {
      /// <summary>
      /// The same GridSplitter resize schemen as in original WPF
      /// </summary>
      WPF,

      /// <summary>
      /// Changed schemen with a bit different behaviour
      /// </summary>
      NonWPF
   }

   /// <summary>
   /// Represents the control that redistributes space between columns or rows of a Grid control.
   /// </summary>
   /// <remarks>
   /// Unlike WPF GridSplitter, Perspex GridSplitter has only one Behavior, GridResizeBehavior.PreviousAndNext.
   /// </remarks>
   public class GridSplitter : Thumb
   {
      /// <summary>
      /// Defines the <see cref="Orientation"/> property.
      /// </summary>
      public static readonly StyledProperty<Orientation> OrientationProperty =
         PerspexProperty.Register<GridSplitter, Orientation>(nameof(Orientation));


      public static readonly StyledProperty<GridResizeBehavior> ResizeBehaviorProperty =
         PerspexProperty.Register<GridSplitter, GridResizeBehavior>(nameof(ResizeBehavior),
            GridResizeBehavior.PreviousAndNext);

      public static readonly StyledProperty<GridResizeScheme> ResizeSchemeProperty =
         PerspexProperty.Register<GridSplitter, GridResizeScheme>(nameof(ResizeScheme));

      public static readonly StyledProperty<Boolean> DeferredResizeEnabledProperty =
         PerspexProperty.Register<GridSplitter, Boolean>(nameof(DeferredResizeEnabled));

      public GridResizeBehavior ResizeBehavior
      {
         get { return GetValue(ResizeBehaviorProperty); }
         set { SetValue(ResizeBehaviorProperty, value); }
      }

      public GridResizeScheme ResizeScheme
      {
         get { return GetValue<GridResizeScheme>(ResizeSchemeProperty); }
         set { SetValue(ResizeSchemeProperty, value); }
      }

      public Boolean DeferredResizeEnabled
      {
         get { return GetValue<Boolean>(DeferredResizeEnabledProperty); }
         set { SetValue(DeferredResizeEnabledProperty, value); }
      }

      protected Grid _grid;

      private DefinitionBase _definition1;

      private DefinitionBase _definition2;

      private List<DefinitionBase> _definitions;

      private SplitBehaviour _splitBehaviour;

      private bool _isResizeBehaviorValid = true;

      /// <summary>
      /// Gets or sets the orientation of the GridsSlitter.
      /// </summary>
      /// <remarks>
      /// if null, it's inferred from column/row definition (should be auto).
      /// </remarks>
      public Orientation Orientation
      {
         get { return GetValue(OrientationProperty); }
         set { SetValue(OrientationProperty, value); }
      }

      /// <summary>
      /// Initializes static members of the <see cref="GridSplitter"/> class. 
      /// </summary>
      static GridSplitter()
      {
         PseudoClass(OrientationProperty, o => o == Perspex.Controls.Orientation.Vertical, ":vertical");
         PseudoClass(OrientationProperty, o => o == Perspex.Controls.Orientation.Horizontal, ":horizontal");
         AffectsArrange(ResizeBehaviorProperty);
         ResizeBehaviorProperty.Changed.Subscribe(ResizeBehaviorChanged);
         ResizeSchemeProperty.Changed.Subscribe(ResizeSchemenChanged);

      }

      private static void ResizeBehaviorChanged(PerspexPropertyChangedEventArgs e)
      {
         var splitter = e.Sender as GridSplitter;
         if (splitter != null && ((IVisual)splitter).IsAttachedToVisualTree)
         {
            splitter.PrepareGridSplitter();
         }
      }

      private static void ResizeSchemenChanged(PerspexPropertyChangedEventArgs e)
      {
         var splitter = e.Sender as GridSplitter;
         if (splitter != null && splitter._grid != null)
         {
            splitter.PrepareGridSplitter();
         }
      }

      private double definition1LengthNew;
      private double definition2LengthNew;
      private double prevDelta = 0;

      private void GetDeltaConstraints(out double min, out double max)
      {
         double definition1Len = GetActualLength(_definition1);
         double definition1Min = GetMinLength(_definition1);
         double definition1Max = GetMaxLength(_definition1);

         double definition2Len = GetActualLength(_definition2);
         double definition2Min = GetMinLength(_definition2);
         double definition2Max = GetMaxLength(_definition2);

         if (_splitBehaviour == SplitBehaviour.ResizeBoth)
         {
            // Determine the minimum and maximum the columns can be resized
            min = -Math.Min(definition1Len - definition1Min, definition2Max - definition2Len);
            max = Math.Min(definition1Max - definition1Len, definition2Len - definition2Min);
         }
         else if (_splitBehaviour == SplitBehaviour.ResizeFirst)
         {
            min = definition1Min - definition1Len;
            max = definition1Max - definition1Len;
         }
         else if (_splitBehaviour == SplitBehaviour.ResizeSecond)
         {
            min = definition2Len - definition2Max;
            max = definition2Len - definition2Min;
         }
         else
         {
            min = definition1Min - definition1Len;
            max = Math.Min(definition1Max - definition1Len, definition2Len - definition2Min);
         }
      }

      protected override void OnDragDelta(VectorEventArgs e)
      {
         if (_isResizeBehaviorValid)
         {
            var delta = Orientation == Orientation.Vertical ? e.Vector.X : e.Vector.Y;
            double max;
            double min;
            GetDeltaConstraints(out min, out max);
            delta = Math.Min(Math.Max(delta, min), max);

            if (prevDelta != delta)
            {
               prevDelta = delta;
               double actualPrev = GetActualLength(_definition1);
               double actualNext = GetActualLength(_definition2);

               // With floating point operations there may be loss of precision to some degree. Eg. Adding a very 
               // small value to a very large one might result in the small value being ignored. In the following 
               // steps there are two floating point operations viz. actualLength1+delta and actualLength2-delta. 
               // It is possible that the addition resulted in loss of precision and the delta value was ignored, whereas 
               // the subtraction actual absorbed the delta value. This now means that 
               // (definition1LengthNew + definition2LengthNewis) 2 factors of precision away from 
               // (actualLength1 + actualLength2). This can cause a problem in the subsequent drag iteration where 
               // this will be interpreted as the cancellation of the resize operation. To avoid this imprecision we use 
               // make definition2LengthNew be a function of definition1LengthNew so that the precision or the loss 
               // thereof can be counterbalanced.
               definition1LengthNew = actualPrev + delta;
               definition2LengthNew = actualPrev + actualNext - definition1LengthNew;

               if (!DeferredResizeEnabled)
               {
                  SetLength(definition1LengthNew, definition2LengthNew);
               }
            }
         }
      }

      protected override void OnDragCompleted(VectorEventArgs e)
      {
         if (_isResizeBehaviorValid && DeferredResizeEnabled)
         {
            SetLength(definition1LengthNew, definition2LengthNew);
         }
      }

      private void SetLength(double prevDefinitionPixels, double nextDefinitionPixels)
      {
         if (_splitBehaviour == SplitBehaviour.ResizeBoth)
         {
            foreach (var definition in _definitions)
            {
               if (definition == _definition1)
               {
                  SetLengthInStars(_definition1, prevDefinitionPixels);
               }
               else if (definition == _definition2)
               {
                  SetLengthInStars(_definition2, nextDefinitionPixels);
               }
               else if (IsStar(definition))
               {
                  SetLengthInStars(definition, GetActualLength(definition)); // same size but in stars.
               }
            }
         }
         else if (_splitBehaviour == SplitBehaviour.ResizeFirst)
         {
            SetLengthInPixels(_definition1, prevDefinitionPixels);
         }
         else if (_splitBehaviour == SplitBehaviour.ResizeSecond)
         {
            SetLengthInPixels(_definition2, nextDefinitionPixels);
         }
         else if (_splitBehaviour == SplitBehaviour.ResizeLeftPlusStar)
         {
            SetLengthInPixels(_definition1, prevDefinitionPixels);
            SetLengthInStars(_definition2, nextDefinitionPixels);
         }
      }

      private void SetLengthInPixels(DefinitionBase definition, double value)
      {
         if (value < 0)
         {
            //size of the Grid definition could not be less than Zero
            value = 0;
         }
         var columnDefinition = definition as ColumnDefinition;
         if (columnDefinition != null)
         {
            columnDefinition.Width = new GridLength(value, GridUnitType.Pixel);
         }
         else
         {
            ((RowDefinition)definition).Height = new GridLength(value, GridUnitType.Pixel);
         }
      }

      private void SetLengthInStars(DefinitionBase definition, double value)
      {
         if (value < 0)
         {
            //size of the Grid definition could not be less than Zero
            value = 0;
         }
         var columnDefinition = definition as ColumnDefinition;
         if (columnDefinition != null)
         {
            columnDefinition.Width = new GridLength(value, GridUnitType.Star);
         }
         else
         {
            ((RowDefinition)definition).Height = new GridLength(value, GridUnitType.Star);
         }
      }

      private double GetActualLength(DefinitionBase definition)
      {
         var columnDefinition = definition as ColumnDefinition;
         return columnDefinition?.ActualWidth ?? ((RowDefinition)definition).ActualHeight;
      }

      private double GetMinLength(DefinitionBase definition)
      {
         var columnDefinition = definition as ColumnDefinition;
         return columnDefinition?.MinWidth ?? ((RowDefinition)definition).MinHeight;
      }

      private double GetMaxLength(DefinitionBase definition)
      {
         var columnDefinition = definition as ColumnDefinition;
         return columnDefinition?.MaxWidth ?? ((RowDefinition)definition).MaxHeight;
      }

      private bool IsStar(DefinitionBase definition)
      {
         var columnDefinition = definition as ColumnDefinition;
         return columnDefinition?.Width.IsStar ?? ((RowDefinition)definition).Height.IsStar;
      }

      protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
      {
         base.OnAttachedToVisualTree(e);
         _grid = this.GetVisualParent<Grid>();

         if (Orientation == Orientation.Vertical)
         {
            Cursor = new Cursor(StandardCursorType.SizeWestEast);
            var col = GetValue(Grid.ColumnProperty);
            _definitions = _grid.ColumnDefinitions.Cast<DefinitionBase>().ToList();
            _definition1 = _definitions[col - 1];
            _definition2 = _definitions[col + 1];
         }
         else
         {
            Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
            var row = GetValue(Grid.RowProperty);
            _definitions = _grid.RowDefinitions.Cast<DefinitionBase>().ToList();
            _definition1 = _definitions[row - 1];
            _definition2 = _definitions[row + 1];
         }
      }

      private void PrepareGridSplitter()
      {
         _isResizeBehaviorValid = true;
         var behavior = ResizeBehavior;
         _grid = this.GetVisualParent<Grid>();
         if (Orientation == Orientation.Horizontal)
         {
            Cursor = new Cursor(StandardCursorType.SizeWestEast);
            _definitions = _grid.ColumnDefinitions.Cast<DefinitionBase>().ToList();
            var col = GetValue<int>(Grid.ColumnProperty);
            switch (behavior)
            {
               case GridResizeBehavior.PreviousAndNext:
                  if (col <= 0 || col + 1 > _definitions.Count - 1)
                  {
                     _isResizeBehaviorValid = false;
                  }
                  else
                  {
                     _definition1 = _definitions[col - 1];
                     _definition2 = _definitions[col + 1];
                  }
                  break;
               case GridResizeBehavior.PreviousAndCurrent:
                  if (col <= 0)
                  {
                     _isResizeBehaviorValid = false;
                  }
                  else
                  {
                     _definition1 = _definitions[col - 1];
                     _definition2 = _definitions[col];
                  }
                  break;
               case GridResizeBehavior.CurrentAndNext:
                  if (col + 1 > _definitions.Count - 1)
                  {
                     _isResizeBehaviorValid = false;
                  }
                  else
                  {
                     _definition1 = _definitions[col];
                     _definition2 = _definitions[col + 1];
                  }
                  break;
            }
         }
         else
         {
            Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
            _definitions = _grid.RowDefinitions.Cast<DefinitionBase>().ToList();
            var row = GetValue<int>(Grid.RowProperty);
            switch (behavior)
            {
               case GridResizeBehavior.PreviousAndNext:
                  if (row <= 0 || row + 1 > _definitions.Count - 1)
                  {
                     _isResizeBehaviorValid = false;
                  }
                  else
                  {
                     _definition1 = _definitions[row - 1];
                     _definition2 = _definitions[row + 1];
                  }
                  break;
               case GridResizeBehavior.PreviousAndCurrent:
                  if (row <= 0)
                  {
                     _isResizeBehaviorValid = false;
                  }
                  else
                  {
                     _definition1 = _definitions[row - 1];
                     _definition2 = _definitions[row];
                  }
                  break;
               case GridResizeBehavior.CurrentAndNext:
                  if (row + 1 > _definitions.Count - 1)
                  {
                     _isResizeBehaviorValid = false;
                  }
                  else
                  {
                     _definition1 = _definitions[row];
                     _definition2 = _definitions[row + 1];
                  }
                  break;
            }
         }

         if (_isResizeBehaviorValid)
         {
            DefineSplitBehavior();
         }

      }

      private void DefineSplitBehavior()
      {
         bool isStar1 = false;
         bool isStar2 = false;
         if (_definition1 is RowDefinition)
         {
            if (((RowDefinition)_definition1).Height.IsStar)
            {
               isStar1 = true;
            }
         }
         else
         {
            if (((ColumnDefinition)_definition1).Width.IsStar)
            {
               isStar1 = true;
            }
         }

         if (_definition2 is RowDefinition)
         {
            if (((RowDefinition)_definition2).Height.IsStar)
            {
               isStar2 = true;
            }
         }
         else
         {
            if (((ColumnDefinition)_definition2).Width.IsStar)
            {
               isStar2 = true;
            }
         }

         //WPF GridSplitter behaviour
         if (ResizeScheme == GridResizeScheme.WPF)
         {
            if (isStar1 && isStar2)
            {
               _splitBehaviour = SplitBehaviour.ResizeBoth;
            }
            else
            {
               _splitBehaviour = !isStar1 ? SplitBehaviour.ResizeFirst : SplitBehaviour.ResizeSecond;
            }
         }
         else
         {
            if (!isStar1 && isStar2)
            {
               _splitBehaviour = SplitBehaviour.ResizeFirst;
            }
            else if (isStar1 && !isStar2)
            {
               _splitBehaviour = SplitBehaviour.ResizeSecond;
            }
            else if (isStar1 && isStar2)
            {
               _splitBehaviour = SplitBehaviour.ResizeBoth;
            }
            else
            {
               _splitBehaviour = SplitBehaviour.ResizeLeftPlusStar;
            }
         }
      }

      private enum SplitBehaviour
      {
         /// <summary>
         /// This flag means that splitter will resize 2 star definitions
         /// </summary>
         ResizeBoth,

         /// <summary>
         /// This flag means that splitter will resize only first definition if it not star
         /// </summary>
         ResizeFirst,

         /// <summary>
         /// This flag means that splitter will resize only second definition if it not star
         /// </summary>
         ResizeSecond,

         /// <summary>
         /// This flag means that splitter will resize both definitions, but first will be Pixel and Second will be Star
         /// </summary>
         ResizeLeftPlusStar
      }
   }
}

