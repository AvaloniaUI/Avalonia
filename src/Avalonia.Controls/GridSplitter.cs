// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    ///     Represents the control that redistributes space between columns or rows of a Grid control.
    /// </summary>
    /// <remarks>
    ///     Unlike WPF GridSplitter, Avalonia GridSplitter has only one Behavior, GridResizeBehavior.PreviousAndNext.
    /// </remarks>
    public class GridSplitter : Thumb
    {
        private List<DefinitionBase> _definitions;

        private Grid _grid;

        private DefinitionBase _nextDefinition;

        private Orientation _orientation;

        private DefinitionBase _prevDefinition;

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
            var delta = _orientation == Orientation.Vertical ? e.Vector.X : e.Vector.Y;
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
                    SetLengthInStars(definition, GetActualLength(definition)); // same size but in stars.
                }
            }
        }

        private double GetActualLength(DefinitionBase definition)
        {
            if (definition == null)
                return 0;
            var columnDefinition = definition as ColumnDefinition;
            return columnDefinition?.ActualWidth ?? ((RowDefinition) definition).ActualHeight;
        }

        private double GetMinLength(DefinitionBase definition)
        {
            if (definition == null)
                return 0;
            var columnDefinition = definition as ColumnDefinition;
            return columnDefinition?.MinWidth ?? ((RowDefinition) definition).MinHeight;
        }

        private double GetMaxLength(DefinitionBase definition)
        {
            if (definition == null)
                return 0;
            var columnDefinition = definition as ColumnDefinition;
            return columnDefinition?.MaxWidth ?? ((RowDefinition) definition).MaxHeight;
        }

        private bool IsStar(DefinitionBase definition)
        {
            var columnDefinition = definition as ColumnDefinition;
            return columnDefinition?.Width.IsStar ?? ((RowDefinition) definition).Height.IsStar;
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
                ((RowDefinition) definition).Height = new GridLength(value, GridUnitType.Star);
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _grid = this.GetVisualParent<Grid>();

            _orientation = DetectOrientation();

            int defenitionIndex; //row or col
            if (_orientation == Orientation.Vertical)
            {
                Cursor = new Cursor(StandardCursorType.SizeWestEast);
                _definitions = _grid.ColumnDefinitions.Cast<DefinitionBase>().ToList();
                defenitionIndex = GetValue(Grid.ColumnProperty);
                PseudoClasses.Add(":vertical");
            }
            else
            {
                Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
                defenitionIndex = GetValue(Grid.RowProperty);
                _definitions = _grid.RowDefinitions.Cast<DefinitionBase>().ToList();
                PseudoClasses.Add(":horizontal");
            }

            if (defenitionIndex > 0)
                _prevDefinition = _definitions[defenitionIndex - 1];

            if (defenitionIndex < _definitions.Count - 1)
                _nextDefinition = _definitions[defenitionIndex + 1];
        }

        private Orientation DetectOrientation()
        {
            if (!_grid.ColumnDefinitions.Any())
                return Orientation.Horizontal;
            if (!_grid.RowDefinitions.Any())
                return Orientation.Vertical;

            var col = GetValue(Grid.ColumnProperty);
            var row = GetValue(Grid.RowProperty);
            var width = _grid.ColumnDefinitions[col].Width;
            var height = _grid.RowDefinitions[row].Height;
            if (width.IsAuto && !height.IsAuto)
            {
                return Orientation.Vertical;
            }
            if (!width.IsAuto && height.IsAuto)
            {
                return Orientation.Horizontal;
            }
            if (_grid.Children.OfType<Control>() // Decision based on other controls in the same column
                .Where(c => Grid.GetColumn(c) == col)
                .Any(c => c.GetType() != typeof (GridSplitter)))
            {
                return Orientation.Horizontal;
            }
            return Orientation.Vertical;
        }
    }
}