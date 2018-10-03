// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Collections;
using Avalonia.Controls.Utils;
using Avalonia.VisualTree;
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

        public static readonly AttachedProperty<bool> IsSharedSizeScopeProperty =
            AvaloniaProperty.RegisterAttached<Grid, Control, bool>("IsSharedSizeScope", false);

        private sealed class SharedSizeScopeHost : IDisposable
        {
            private enum MeasurementState
            {
                Invalidated,
                Measuring,
                Cached
            }

            private class MeasurementCache
            {
                public MeasurementCache(Grid grid)
                {
                    Grid = grid;
                    Results = grid.RowDefinitions.Cast<DefinitionBase>()
                                 .Concat(grid.ColumnDefinitions)
                                 .Select(d => new MeasurementResult(d))
                                 .ToList();
                }

                public void UpdateMeasureResult(GridLayout.MeasureResult rowResult, GridLayout.MeasureResult columnResult)
                {
                    RowResult = rowResult;
                    ColumnResult = columnResult;
                    MeasurementState = MeasurementState.Cached;
                    for (int i = 0; i < rowResult.LengthList.Count; i++)
                    {
                        Results[i].MeasuredResult = rowResult.LengthList[i];
                    }

                    for (int i = 0; i < columnResult.LengthList.Count; i++)
                    {
                        Results[i + rowResult.LengthList.Count].MeasuredResult = columnResult.LengthList[i];
                    }
                }

                public void InvalidateMeasure()
                {
                    MeasurementState = MeasurementState.Invalidated;
                    Results.ForEach(r => r.MeasuredResult = double.NaN);
                }

                public Grid Grid { get; }
                public GridLayout.MeasureResult RowResult { get; private set; }
                public GridLayout.MeasureResult ColumnResult { get; private set; }
                public MeasurementState MeasurementState { get; private set; }

                public List<MeasurementResult> Results { get; }
            }

            private readonly AvaloniaList<MeasurementCache> _measurementCaches;

            private class MeasurementResult
            {
                public MeasurementResult(DefinitionBase @base)
                {
                    Definition = @base;
                    MeasuredResult = double.NaN;
                }

                public DefinitionBase Definition { get; }
                public double MeasuredResult { get; set; }
            }

            private enum ScopeType
            {
                Auto,
                Fixed
            }

            private class Group
            {
                public bool IsFixed { get; set; }

                public List<MeasurementResult> Results { get; }

                public double CalculatedLength { get; }
            }

            private Dictionary<string, Group> _groups = new Dictionary<string, Group>();


            public SharedSizeScopeHost(Control scope)
            {
                _measurementCaches = GetParticipatingGrids(scope);
                
                foreach (var cache in _measurementCaches)
                {
                    cache.Grid.InvalidateMeasure();
                    AddGridToScopes(cache);
                }
            }

            internal void InvalidateMeasure(Grid grid)
            {
                var cache = _measurementCaches.FirstOrDefault(mc => ReferenceEquals(mc.Grid, grid));
                Debug.Assert(cache != null);

                cache.InvalidateMeasure();
            }

            internal void UpdateMeasureStatus(Grid grid, GridLayout.MeasureResult rowResult, GridLayout.MeasureResult columnResult)
            {
                var cache = _measurementCaches.FirstOrDefault(mc => ReferenceEquals(mc.Grid, grid));
                Debug.Assert(cache != null);

                cache.UpdateMeasureResult(rowResult, columnResult);
            }

            internal (GridLayout.MeasureResult, GridLayout.MeasureResult) HandleArrange(Grid grid, GridLayout.MeasureResult rowResult, GridLayout.MeasureResult columnResult)
            {
                var rowConventions = rowResult.LeanLengthList.ToList();
                var rowLengths = rowResult.LengthList.ToList();
                var rowDesiredLength = 0.0;
                for (int i = 0; i < grid.RowDefinitions.Count; i++)
                {
                    var definition = grid.RowDefinitions[i];
                    if (string.IsNullOrEmpty(definition.SharedSizeGroup))
                    {
                        rowDesiredLength += rowResult.LengthList[i];
                        continue;
                    }

                    var group = _groups[definition.SharedSizeGroup];

                    var length = group.Results.Max(g => g.MeasuredResult);
                    rowConventions[i] = new GridLayout.LengthConvention(
                        new GridLength(length),
                        rowResult.LeanLengthList[i].MinLength,
                        rowResult.LeanLengthList[i].MaxLength
                        );
                    rowLengths[i] = length;
                    rowDesiredLength += length;

                }

                var columnConventions = columnResult.LeanLengthList.ToList();
                var columnLengths = columnResult.LengthList.ToList();
                var columnDesiredLength = 0.0;
                for (int i = 0; i < grid.ColumnDefinitions.Count; i++)
                {
                    var definition = grid.ColumnDefinitions[i];
                    if (string.IsNullOrEmpty(definition.SharedSizeGroup))
                    {
                        columnDesiredLength += rowResult.LengthList[i];
                        continue;
                    }

                    var group = _groups[definition.SharedSizeGroup];

                    var length = group.Results.Max(g => g.MeasuredResult);
                    columnConventions[i] = new GridLayout.LengthConvention(
                        new GridLength(length),
                        columnResult.LeanLengthList[i].MinLength,
                        columnResult.LeanLengthList[i].MaxLength
                        );
                    columnLengths[i] = length;
                    columnDesiredLength += length;
                }

                return (
                    new GridLayout.MeasureResult(
                        rowResult.ContainerLength,
                        rowDesiredLength,
                        rowResult.GreedyDesiredLength,//??
                        rowConventions,
                        rowLengths),
                    new GridLayout.MeasureResult(
                        columnResult.ContainerLength,
                        columnDesiredLength,
                        columnResult.GreedyDesiredLength, //??
                        columnConventions,
                        columnLengths)
                    );
            }


            private void AddGridToScopes(MeasurementCache cache)
            {
                foreach (var result in cache.Results)
                {
                    var scopeName = result.Definition.SharedSizeGroup;
                    if (!_groups.TryGetValue(scopeName, out var group))
                        _groups.Add(scopeName, group = new Group());

                    group.IsFixed |= IsFixed(result.Definition);

                    group.Results.Add(result);
                }
            }

            private bool IsFixed(DefinitionBase definition)
            {
                return ((definition as ColumnDefinition)?.Width ?? ((RowDefinition)definition).Height).IsAbsolute;
            }

            private void RemoveGridFromScopes(MeasurementCache cache)
            {
                foreach (var result in cache.Results)
                {
                    var scopeName = result.Definition.SharedSizeGroup;
                    Debug.Assert(_groups.TryGetValue(scopeName, out var group));

                    group.Results.Remove(result);
                    if (!group.Results.Any())
                        _groups.Remove(scopeName);
                    else
                    {
                        group.IsFixed = group.Results.Select(r => r.Definition).Any(IsFixed);
                    }
                }
            }

            private static AvaloniaList<MeasurementCache> GetParticipatingGrids(Control scope)
            {
                var result = scope.GetVisualDescendants().OfType<Grid>();

                return new AvaloniaList<MeasurementCache>(
                    result.Where(g => g.HasSharedSizeGroups())
                          .Select(g => new MeasurementCache(g)));
            }

            public void Dispose()
            {
                foreach (var cache in _measurementCaches)
                {
                    cache.Grid.SharedScopeChanged();
                }
            }

            internal void RegisterGrid(Grid toAdd)
            {
                Debug.Assert(!_measurementCaches.Any(mc => ReferenceEquals(mc.Grid,toAdd)));
                var cache = new MeasurementCache(toAdd);
                _measurementCaches.Add(cache);
                AddGridToScopes(cache);
            }

            internal void UnegisterGrid(Grid toRemove)
            {
                var cache = _measurementCaches.FirstOrDefault(mc => ReferenceEquals(mc.Grid, toRemove));

                Debug.Assert(cache != null);
                _measurementCaches.Remove(cache);
                RemoveGridFromScopes(cache);
            }
        }

        protected override void OnMeasureInvalidated()
        {
            base.OnMeasureInvalidated();
            _sharedSizeHost?.InvalidateMeasure(this);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            var scope = this.GetVisualAncestors().OfType<Control>()
                .FirstOrDefault(c => c.GetValue(IsSharedSizeScopeProperty));

            Debug.Assert(_sharedSizeHost == null);

            if (scope != null)
            {
                _sharedSizeHost = scope.GetValue(s_sharedSizeScopeHostProperty);
                _sharedSizeHost.RegisterGrid(this);
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            _sharedSizeHost?.UnegisterGrid(this);
            _sharedSizeHost = null;
        }

        private SharedSizeScopeHost _sharedSizeHost;

        private static readonly AttachedProperty<SharedSizeScopeHost> s_sharedSizeScopeHostProperty =
            AvaloniaProperty.RegisterAttached<Grid, Control, SharedSizeScopeHost>("&&SharedSizeScopeHost", null);

        private ColumnDefinitions _columnDefinitions;

        private RowDefinitions _rowDefinitions;

        static Grid()
        {
            AffectsParentMeasure<Grid>(ColumnProperty, ColumnSpanProperty, RowProperty, RowSpanProperty);
            IsSharedSizeScopeProperty.Changed.AddClassHandler<Control>(IsSharedSizeScopeChanged);
        }

        private static void IsSharedSizeScopeChanged(Control source, AvaloniaPropertyChangedEventArgs arg2) 
        {
            if ((bool)arg2.NewValue)
            {
                Debug.Assert(source.GetValue(s_sharedSizeScopeHostProperty) == null);
                source.SetValue(IsSharedSizeScopeProperty, new SharedSizeScopeHost(source));
            }
            else
            {
                var host = source.GetValue(s_sharedSizeScopeHostProperty) as SharedSizeScopeHost;
                Debug.Assert(host != null);
                host.Dispose();
                source.SetValue(IsSharedSizeScopeProperty, null);
            }
        }

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
        /// Gets the result of the last column measurement.
        /// Use this result to reduce the arrange calculation.
        /// </summary>
        private GridLayout.MeasureResult _columnMeasureCache;

        /// <summary>
        /// Gets the result of the last row measurement.
        /// Use this result to reduce the arrange calculation.
        /// </summary>
        private GridLayout.MeasureResult _rowMeasureCache;

        /// <summary>
        /// Gets the row layout as of the last measure.
        /// </summary>
        private GridLayout _rowLayoutCache;

        /// <summary>
        /// Gets the column layout as of the last measure.
        /// </summary>
        private GridLayout _columnLayoutCache;

        /// <summary>
        /// Measures the grid.
        /// </summary>
        /// <param name="constraint">The available size.</param>
        /// <returns>The desired size of the control.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            // Situation 1/2:
            // If the grid doesn't have any column/row definitions, it behaves like a normal panel.
            // GridLayout supports this situation but we handle this separately for performance.

            if (ColumnDefinitions.Count == 0 && RowDefinitions.Count == 0)
            {
                var maxWidth = 0.0;
                var maxHeight = 0.0;
                foreach (var child in Children.OfType<Control>())
                {
                    child.Measure(constraint);
                    maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
                    maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
                }

                maxWidth = Math.Min(maxWidth, constraint.Width);
                maxHeight = Math.Min(maxHeight, constraint.Height);
                return new Size(maxWidth, maxHeight);
            }

            // Situation 2/2:
            // If the grid defines some columns or rows.
            // Debug Tip:
            //     - GridLayout doesn't hold any state, so you can drag the debugger execution
            //       arrow back to any statements and re-run them without any side-effect.

            var measureCache = new Dictionary<Control, Size>();
            var (safeColumns, safeRows) = GetSafeColumnRows();
            var columnLayout = new GridLayout(ColumnDefinitions);
            var rowLayout = new GridLayout(RowDefinitions);
            // Note: If a child stays in a * or Auto column/row, use constraint to measure it.
            columnLayout.AppendMeasureConventions(safeColumns, child => MeasureOnce(child, constraint).Width);
            rowLayout.AppendMeasureConventions(safeRows, child => MeasureOnce(child, constraint).Height);

            // Calculate measurement.
            var columnResult = columnLayout.Measure(constraint.Width);
            var rowResult = rowLayout.Measure(constraint.Height);

            // Use the results of the measurement to measure the rest of the children.
            foreach (var child in Children.OfType<Control>())
            {
                var (column, columnSpan) = safeColumns[child];
                var (row, rowSpan) = safeRows[child];
                var width = Enumerable.Range(column, columnSpan).Select(x => columnResult.LengthList[x]).Sum();
                var height = Enumerable.Range(row, rowSpan).Select(x => rowResult.LengthList[x]).Sum();

                MeasureOnce(child, new Size(width, height));
            }

            // Cache the measure result and return the desired size.
            _columnMeasureCache = columnResult;
            _rowMeasureCache = rowResult;
            _rowLayoutCache = rowLayout;
            _columnLayoutCache = columnLayout;

            _sharedSizeHost?.UpdateMeasureStatus(this, rowResult, columnResult);

            return new Size(columnResult.DesiredLength, rowResult.DesiredLength);

            // Measure each child only once.
            // If a child has been measured, it will just return the desired size.
            Size MeasureOnce(Control child, Size size)
            {
                if (measureCache.TryGetValue(child, out var desiredSize))
                {
                    return desiredSize;
                }

                child.Measure(size);
                desiredSize = child.DesiredSize;
                measureCache[child] = desiredSize;
                return desiredSize;
            }
        }

        /// <summary>
        /// Arranges the grid's children.
        /// </summary>
        /// <param name="finalSize">The size allocated to the control.</param>
        /// <returns>The space taken.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            // Situation 1/2:
            // If the grid doesn't have any column/row definitions, it behaves like a normal panel.
            // GridLayout supports this situation but we handle this separately for performance.

            if (ColumnDefinitions.Count == 0 && RowDefinitions.Count == 0)
            {
                foreach (var child in Children.OfType<Control>())
                {
                    child.Arrange(new Rect(finalSize));
                }

                return finalSize;
            }

            // Situation 2/2:
            // If the grid defines some columns or rows.
            // Debug Tip:
            //     - GridLayout doesn't hold any state, so you can drag the debugger execution
            //       arrow back to any statements and re-run them without any side-effect.

            var (safeColumns, safeRows) = GetSafeColumnRows();
            var columnLayout = _columnLayoutCache;
            var rowLayout = _rowLayoutCache;

            var (rowCache, columnCache) = _sharedSizeHost?.HandleArrange(this, _rowMeasureCache, _columnMeasureCache) ?? (_rowMeasureCache, _columnMeasureCache);

            // Calculate for arrange result.
            var columnResult = columnLayout.Arrange(finalSize.Width, rowCache);
            var rowResult = rowLayout.Arrange(finalSize.Height, columnCache);
            // Arrange the children.
            foreach (var child in Children.OfType<Control>())
            {
                var (column, columnSpan) = safeColumns[child];
                var (row, rowSpan) = safeRows[child];
                var x = Enumerable.Range(0, column).Sum(c => columnResult.LengthList[c]);
                var y = Enumerable.Range(0, row).Sum(r => rowResult.LengthList[r]);
                var width = Enumerable.Range(column, columnSpan).Sum(c => columnResult.LengthList[c]);
                var height = Enumerable.Range(row, rowSpan).Sum(r => rowResult.LengthList[r]);
                child.Arrange(new Rect(x, y, width, height));
            }

            // Assign the actual width.
            for (var i = 0; i < ColumnDefinitions.Count; i++)
            {
                ColumnDefinitions[i].ActualWidth = columnResult.LengthList[i];
            }

            // Assign the actual height.
            for (var i = 0; i < RowDefinitions.Count; i++)
            {
                RowDefinitions[i].ActualHeight = rowResult.LengthList[i];
            }

            // Return the render size.
            return finalSize;
        }

        /// <summary>
        /// Get the safe column/columnspan and safe row/rowspan.
        /// This method ensures that none of the children has a column/row outside the bounds of the definitions.
        /// </summary>
        [Pure]
        private (Dictionary<Control, (int index, int span)> safeColumns,
            Dictionary<Control, (int index, int span)> safeRows) GetSafeColumnRows()
        {
            var columnCount = ColumnDefinitions.Count;
            var rowCount = RowDefinitions.Count;
            columnCount = columnCount == 0 ? 1 : columnCount;
            rowCount = rowCount == 0 ? 1 : rowCount;
            var safeColumns = Children.OfType<Control>().ToDictionary(child => child,
                child => GetSafeSpan(columnCount, GetColumn(child), GetColumnSpan(child)));
            var safeRows = Children.OfType<Control>().ToDictionary(child => child,
                child => GetSafeSpan(rowCount, GetRow(child), GetRowSpan(child)));
            return (safeColumns, safeRows);
        }

        /// <summary>
        /// Gets the safe row/column and rowspan/columnspan for a specified range.
        /// The user may assign row/column properties outside the bounds of the row/column count, this method coerces them inside.
        /// </summary>
        /// <param name="length">The row or column count.</param>
        /// <param name="userIndex">The row or column that the user assigned.</param>
        /// <param name="userSpan">The rowspan or columnspan that the user assigned.</param>
        /// <returns>The safe row/column and rowspan/columnspan.</returns>
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (int index, int span) GetSafeSpan(int length, int userIndex, int userSpan)
        {
            var index = userIndex;
            var span = userSpan;

            if (index < 0)
            {
                span = index + span;
                index = 0;
            }

            if (span <= 0)
            {
                span = 1;
            }

            if (userIndex >= length)
            {
                index = length - 1;
                span = 1;
            }
            else if (userIndex + userSpan > length)
            {
                span = length - userIndex;
            }

            return (index, span);
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

        internal bool HasSharedSizeGroups()
        {
            return ColumnDefinitions.Any(cd => !string.IsNullOrEmpty(cd.SharedSizeGroup)) ||
                   RowDefinitions.Any(rd => !string.IsNullOrEmpty(rd.SharedSizeGroup));
        }

        internal void SharedScopeChanged()
        {
            _sharedSizeHost = null;
            var scope = this.GetVisualAncestors().OfType<Control>()
                .FirstOrDefault(c => c.GetValue(IsSharedSizeScopeProperty));

            if (scope != null)
            {
                _sharedSizeHost = scope.GetValue(s_sharedSizeScopeHostProperty);
                _sharedSizeHost.RegisterGrid(this);
            }

            InvalidateMeasure();
        }
    }
}
