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
    /// <summary>
    /// Unlike WPF GridSplitter, Perspex GridSplitter has only one Behavior. It's GridResizeBehavior.PreviousAndNext
    /// </summary>
    public class GridSplitter : Thumb
    {
        private Grid _grid;

        private DefinitionBase _prevDefinition;

        private DefinitionBase _nextDefinition;

        private bool _isResizingColumns;

        private List<DefinitionBase> _definitions;

        /// <summary>
        /// Decide depending on set size
        /// </summary>
        /// <returns></returns>
        private bool IsResizingColumns()
        {
            if (!double.IsNaN(Width))
            {
                return true;
            }
            if (!double.IsNaN(Height))
            {
                return false;
            }
            throw new InvalidOperationException("GridSpliter Should have width or height set. It affects whether columns or rows it resizes");
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

        private void SetLengthInStars(DefinitionBase definition, double value)
        {
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




        private void GetDeltaConstraints(out double min, out double max)
        {
            double prevDefinitionLen = GetActualLength(_prevDefinition);
            double prevDefinitionMin = GetMinLength(_prevDefinition);
            double prevDefinitionMax = GetMaxLength(_prevDefinition);

            double nextDefinitionLen = GetActualLength(_nextDefinition);
            double nextDefinitionMin = GetMinLength(_nextDefinition);
            double nextDefinitionMax = GetMaxLength(_nextDefinition);


            // Determine the minimum and maximum the columns can be resized
            min = -Math.Min(prevDefinitionLen - prevDefinitionMin, nextDefinitionMax - nextDefinitionLen);
            max = Math.Min(prevDefinitionMax - prevDefinitionLen, nextDefinitionLen - nextDefinitionMin);
        }


        protected override void OnDragDelta(VectorEventArgs e)
        {
            var delta = _isResizingColumns  ? e.Vector.X : e.Vector.Y;

            double max;
            double min;
            GetDeltaConstraints(out min, out max);
            delta = Math.Min(Math.Max(delta, min), max);


            foreach (var definition in _definitions)
            {
                if (definition == _prevDefinition)
                {
                    SetLengthInStars(_prevDefinition, GetActualLength(_prevDefinition) + delta);
                }
                else if (definition == _nextDefinition)
                {
                    SetLengthInStars(_nextDefinition, GetActualLength(_nextDefinition) - delta);
                }
                else if(IsStar(definition))
                {
                    SetLengthInStars(definition, GetActualLength(definition)); // same size but in stars
                }
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            //new Range
            base.OnAttachedToVisualTree(e);
            _grid = this.GetVisualParent<Grid>();
            _isResizingColumns = IsResizingColumns();
            if (_isResizingColumns)
            {
                Cursor = new Cursor(StandardCursorType.SizeWestEast);
                var col = GetValue(Grid.ColumnProperty);
                _definitions = _grid.ColumnDefinitions.Cast<DefinitionBase>().ToList();
                _prevDefinition = _definitions[col - 1];
                _nextDefinition = _definitions[col + 1];
            }
            else
            {
                Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
                var row = GetValue(Grid.RowProperty);
                _definitions = _grid.RowDefinitions.Cast<DefinitionBase>().ToList();
                _prevDefinition = _definitions[row - 1];
                _nextDefinition = _definitions[row + 1];
            }
        }
    }
}
