// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

using Perspex.Collections;
using Perspex.Controls.Primitives;
using Perspex.Input;
using Perspex.VisualTree;

namespace Perspex.Controls
{
    /// <summary>
    /// Unlike WPF GridSplitter, Perspex GridSplitter has only one Behavior. It's GridResizeBehavior.PreviousAndNext
    /// </summary>
    public abstract class GridSplitterBase<T> : Thumb
        where T : DefinitionBase
    {
        protected PerspexList<T> _definitions;

        protected Grid _grid;

        protected T _nextDefinition;

        protected T _prevDefinition;

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

        protected abstract double GetActualLength(T definition);

        protected abstract double GetMinLength(T definition);

        protected abstract double GetMaxLength(T definition);

        protected abstract bool IsStar(T definition);

        protected abstract void SetLengthInStars(T definition, double value);

        private void GetDeltaConstraints(out double min, out double max)
        {
            var prevDefinitionLen = GetActualLength(_prevDefinition);
            var prevDefinitionMin = GetMinLength(_prevDefinition);
            var prevDefinitionMax = GetMaxLength(_prevDefinition);

            var nextDefinitionLen = GetActualLength(_nextDefinition);
            var nextDefinitionMin = GetMinLength(_nextDefinition);
            var nextDefinitionMax = GetMaxLength(_nextDefinition);

            // Determine the minimum and maximum the columns can be resized
            min = -Math.Min(prevDefinitionLen - prevDefinitionMin, nextDefinitionMax - nextDefinitionLen);
            max = Math.Min(prevDefinitionMax - prevDefinitionLen, nextDefinitionLen - nextDefinitionMin);
        }

        protected override void OnDragDelta(VectorEventArgs e)
        {
            var delta = GetDelta(e);

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
                else if (IsStar(definition))
                {
                    SetLengthInStars(definition, GetActualLength(definition)); // same size but in stars
                }
            }
        }

        protected abstract double GetDelta(VectorEventArgs vectorEventArgs);

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _grid = this.GetVisualParent<Grid>();
        }
    }

    public class HorizontalGridSplitter : GridSplitterBase<RowDefinition>
    {
        public HorizontalGridSplitter()
        {
            Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
        }

        protected override double GetActualLength(RowDefinition definition)
        {
            return definition.ActualHeight;
        }

        protected override double GetMaxLength(RowDefinition definition)
        {
            return definition.MaxHeight;
        }

        protected override double GetMinLength(RowDefinition definition)
        {
            return definition.MinHeight;
        }

        protected override bool IsStar(RowDefinition definition)
        {
            return definition.Height.IsStar;
        }

        protected override double GetDelta(VectorEventArgs vectorEventArgs)
        {
            return vectorEventArgs.Vector.Y;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            var row = GetValue(Grid.RowProperty);
            _definitions = _grid.RowDefinitions;
            _prevDefinition = _definitions[row - 1];
            _nextDefinition = _definitions[row + 1];
        }

        protected override void SetLengthInStars(RowDefinition definition, double value)
        {
            definition.Height = new GridLength(value, GridUnitType.Star);
        }
    }

    public class VerticalGridSplitter : GridSplitterBase<ColumnDefinition>
    {



        public VerticalGridSplitter()
        {
            Cursor = new Cursor(StandardCursorType.SizeWestEast);
        }

        protected override double GetActualLength(ColumnDefinition definition)
        {
            return definition.ActualWidth;
        }

        protected override double GetMaxLength(ColumnDefinition definition)
        {
            return definition.MaxWidth;
        }

        protected override double GetMinLength(ColumnDefinition definition)
        {
            return definition.MinWidth;
        }

        protected override bool IsStar(ColumnDefinition definition)
        {
            return definition.Width.IsStar;
        }
        protected override double GetDelta(VectorEventArgs vectorEventArgs)
        {
            return vectorEventArgs.Vector.X;
        }

        protected override void SetLengthInStars(ColumnDefinition definition, double value)
        {
            definition.Width = new GridLength(value, GridUnitType.Star);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            var col = GetValue(Grid.ColumnProperty);
            _definitions = _grid.ColumnDefinitions;
            _prevDefinition = _definitions[col - 1];
            _nextDefinition = _definitions[col + 1];
        }
    }
}