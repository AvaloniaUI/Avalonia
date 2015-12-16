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
    /// Unlike WPF GridSplitter, Perspex GridSplitter has only one Behavior, GridResizeBehavior.PreviousAndNext
    /// </summary>
    public class GridSplitter : Thumb
    {
        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly PerspexProperty<Orientation?> OrientationProperty =
            PerspexProperty.Register<GridSplitter, Orientation?>(nameof(Orientation));

        protected Grid _grid;

        private IGridColumnsResizer _resizer;

        /// <summary>
        /// Gets or sets the orientation of the GridsSlitter, if null, it's inferred from column/row defenition(should be auto).
        /// </summary>
        public Orientation? Orientation { get { return GetValue(OrientationProperty); } set { SetValue(OrientationProperty, value); } }


        static GridSplitter()
        {
            PseudoClass(OrientationProperty, o => o == Perspex.Controls.Orientation.Vertical, ":vertical");
            PseudoClass(OrientationProperty, o => o == Perspex.Controls.Orientation.Horizontal, ":horizontal");
        }


        protected override void OnDragDelta(VectorEventArgs e)
        {
            _resizer.DragDelta(e);
        }

        /// <summary>
        /// If orientation is not set, method automatically calculates orientation based column/row auto size
        /// </summary>
        /// <returns></returns>
        private void AutoSetOrientation()
        {
            if (Orientation.HasValue)
            {
                return;
            }
            if (_grid.ColumnDefinitions[GetValue(Grid.ColumnProperty)].Width.IsAuto)
            {
                Orientation = Perspex.Controls.Orientation.Vertical;
                return;
            }
            if (_grid.RowDefinitions[GetValue(Grid.RowProperty)].Height.IsAuto)
            {
                Orientation = Perspex.Controls.Orientation.Horizontal;
                return;
            }
            throw new InvalidOperationException("GridSpliter Should have Orientation, width or height set.");
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _grid = this.GetVisualParent<Grid>();
            AutoSetOrientation();
            switch (Orientation)
            {
                case Perspex.Controls.Orientation.Vertical:
                    _resizer = new VerticalColumnsResizer(_grid, GetValue(Grid.ColumnProperty));
                    break;
                case Perspex.Controls.Orientation.Horizontal:
                    _resizer = new HorizontalGridColumnsResizer(_grid, GetValue(Grid.RowProperty));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            base.OnAttachedToVisualTree(e);
        }
    }

    internal interface IGridColumnsResizer
    {
        void DragDelta(VectorEventArgs e);
    }

    internal abstract class GridColumnsResizer<T> : IGridColumnsResizer
        where T : DefinitionBase
    {
        protected PerspexList<T> _definitions;

        protected T _nextDefinition;

        protected T _prevDefinition;

        public abstract Cursor Cursor { get; }

        public void DragDelta(VectorEventArgs e)
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

        protected abstract double GetActualLength(T definition);

        protected abstract double GetMinLength(T definition);

        protected abstract double GetMaxLength(T definition);

        protected abstract bool IsStar(T definition);

        protected abstract void SetLengthInStars(T definition, double value);

        protected abstract double GetDelta(VectorEventArgs vectorEventArgs);

        protected void GetDeltaConstraints(out double min, out double max)
        {
            var _prevDefinitionLen = GetActualLength(_prevDefinition);
            var _prevDefinitionMin = GetMinLength(_prevDefinition);
            var _prevDefinitionMax = GetMaxLength(_prevDefinition);

            var _nextDefinitionLen = GetActualLength(_nextDefinition);
            var _nextDefinitionMin = GetMinLength(_nextDefinition);
            var _nextDefinitionMax = GetMaxLength(_nextDefinition);

            // Determine the minimum and maximum the columns can be resized
            min = -Math.Min(_prevDefinitionLen - _prevDefinitionMin, _nextDefinitionMax - _nextDefinitionLen);
            max = Math.Min(_prevDefinitionMax - _prevDefinitionLen, _nextDefinitionLen - _nextDefinitionMin);
        }
    }

    internal class HorizontalGridColumnsResizer : GridColumnsResizer<RowDefinition>
    {
        public HorizontalGridColumnsResizer(Grid _grid, int splitterRow)
        {
            _definitions = _grid.RowDefinitions;
            _nextDefinition = _definitions[splitterRow + 1];
            _prevDefinition = _definitions[splitterRow - 1];
        }

        public override Cursor Cursor => new Cursor(StandardCursorType.SizeNorthSouth);

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

        protected override void SetLengthInStars(RowDefinition definition, double value)
        {
            definition.Height = new GridLength(value, GridUnitType.Star);
        }
    }

    internal class VerticalColumnsResizer : GridColumnsResizer<ColumnDefinition>
    {
        public VerticalColumnsResizer(Grid _grid, int splitterColumn)
        {
            _definitions = _grid.ColumnDefinitions;
            _nextDefinition = _definitions[splitterColumn + 1];
            _prevDefinition = _definitions[splitterColumn - 1];
        }

        public override Cursor Cursor => new Cursor(StandardCursorType.SizeWestEast);

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
    }
}