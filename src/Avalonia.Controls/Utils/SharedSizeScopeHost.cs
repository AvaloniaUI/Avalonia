using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Avalonia.Collections;
using Avalonia.Controls.Utils;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    internal sealed class SharedSizeScopeHost : IDisposable
    {
        private enum MeasurementState
        {
            Invalidated,
            Measuring,
            Cached
        }

        private sealed class MeasurementCache : IDisposable
        {
            readonly CompositeDisposable _subscriptions;
            readonly Subject<(string, string, MeasurementResult)> _groupChanged = new Subject<(string, string, MeasurementResult)>();

            public ISubject<(string oldName, string newName, MeasurementResult result)> GroupChanged => _groupChanged;

            public MeasurementCache(Grid grid)
            {
                Grid = grid;
                Results = grid.RowDefinitions.Cast<DefinitionBase>()
                    .Concat(grid.ColumnDefinitions)
                    .Select(d => new MeasurementResult(d))
                    .ToList();

                grid.RowDefinitions.CollectionChanged += DefinitionsCollectionChanged;
                grid.ColumnDefinitions.CollectionChanged += DefinitionsCollectionChanged;

                _subscriptions = new CompositeDisposable(
                    Disposable.Create(() => grid.RowDefinitions.CollectionChanged -= DefinitionsCollectionChanged),
                    Disposable.Create(() => grid.ColumnDefinitions.CollectionChanged -= DefinitionsCollectionChanged),
                    grid.RowDefinitions.TrackItemPropertyChanged(DefinitionPropertyChanged),
                    grid.ColumnDefinitions.TrackItemPropertyChanged(DefinitionPropertyChanged));

            }

            private void DefinitionPropertyChanged(Tuple<object, PropertyChangedEventArgs> propertyChanged)
            {
                if (propertyChanged.Item2.PropertyName == nameof(DefinitionBase.SharedSizeGroup))
                {
                    var oldName = string.Empty; // TODO: find how to determine the old name
                    var newName = (propertyChanged.Item1 as DefinitionBase).SharedSizeGroup;
                    var result = Results.Single(mr => ReferenceEquals(mr.Definition, propertyChanged.Item1));
                    _groupChanged.OnNext((oldName, newName, result));
                }
            }

            private void DefinitionsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                int offset = 0;
                if (sender is ColumnDefinitions)
                    offset = Grid.RowDefinitions.Count;

                var newItems = e.NewItems?.OfType<DefinitionBase>().Select(db => new MeasurementResult(db)).ToList() ?? new List<MeasurementResult>();
                var oldItems = Results.GetRange(e.OldStartingIndex + offset, e.OldItems?.Count ?? 0);

                void NotifyNewItems()
                {
                    foreach (var item in newItems)
                    {
                        if (string.IsNullOrEmpty(item.Definition.SharedSizeGroup))
                            continue;

                        _groupChanged.OnNext((null, item.Definition.SharedSizeGroup, item));
                    }
                }

                void NotifyOldItems()
                {
                    foreach (var item in oldItems)
                    {
                        if (string.IsNullOrEmpty(item.Definition.SharedSizeGroup))
                            continue;

                        _groupChanged.OnNext((item.Definition.SharedSizeGroup, null, item));
                    }
                }

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        Results.InsertRange(e.NewStartingIndex + offset, newItems);
                        NotifyNewItems();
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        Results.RemoveRange(e.OldStartingIndex + offset, oldItems.Count);
                        NotifyOldItems();
                        break;

                    case NotifyCollectionChangedAction.Move:
                        Results.RemoveRange(e.OldStartingIndex + offset, oldItems.Count);
                        Results.InsertRange(e.NewStartingIndex + offset, oldItems);
                        break;

                    case NotifyCollectionChangedAction.Replace:
                        Results.RemoveRange(e.OldStartingIndex + offset, oldItems.Count);
                        Results.InsertRange(e.NewStartingIndex + offset, newItems);

                        NotifyOldItems();
                        NotifyNewItems();

                        break;

                    case NotifyCollectionChangedAction.Reset:
                        oldItems = Results;
                        newItems = Results = Grid.RowDefinitions.Cast<DefinitionBase>()
                            .Concat(Grid.ColumnDefinitions)
                            .Select(d => new MeasurementResult(d))
                            .ToList();
                        NotifyOldItems();
                        NotifyNewItems();

                        break;
                }
            }

            public void UpdateMeasureResult(GridLayout.MeasureResult rowResult, GridLayout.MeasureResult columnResult)
            {
                MeasurementState = MeasurementState.Cached;
                for (int i = 0; i < Grid.RowDefinitions.Count; i++)
                {
                    Results[i].MeasuredResult = rowResult.LengthList[i];
                }

                for (int i = 0; i < Grid.ColumnDefinitions.Count; i++)
                {
                    Results[i + rowResult.LengthList.Count].MeasuredResult = columnResult.LengthList[i];
                }
            }

            public void InvalidateMeasure()
            {
                MeasurementState = MeasurementState.Invalidated;
                Results.ForEach(r => r.MeasuredResult = double.NaN);
            }

            public void Dispose()
            {
                _subscriptions.Dispose();
                _groupChanged.OnCompleted();
            }

            public Grid Grid { get; }
            public MeasurementState MeasurementState { get; private set; }

            public List<MeasurementResult> Results { get; private set; }
        }

        private readonly AvaloniaList<MeasurementCache> _measurementCaches;

        private class MeasurementResult
        {
            public MeasurementResult(DefinitionBase definition)
            {
                Definition = definition;
                MeasuredResult = double.NaN;
            }

            public DefinitionBase Definition { get; }
            public double MeasuredResult { get; set; }
        }

        private class Group
        {
            public bool IsFixed { get; set; }

            public List<MeasurementResult> Results { get; } = new List<MeasurementResult>();

            public double CalculatedLength { get; }
        }

        private readonly Dictionary<string, Group> _groups = new Dictionary<string, Group>();


        public SharedSizeScopeHost(Control scope)
        {
            _measurementCaches = GetParticipatingGrids(scope);

            foreach (var cache in _measurementCaches)
            {
                cache.Grid.InvalidateMeasure();
                AddGridToScopes(cache);

                cache.GroupChanged.Subscribe(SharedGroupChanged);
            }
        }

        void SharedGroupChanged((string oldName, string newName, MeasurementResult result) change)
        {
            RemoveFromGroup(change.oldName, change.result);
            AddToGroup(change.newName, change.result);
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

        private double Gather(IEnumerable<MeasurementResult> measurements)
        {
            var result = 0.0d;

            bool onlyFixed = false;

            foreach (var measurement in measurements)
            {
                if (measurement.Definition is ColumnDefinition column)
                {
                    if (!onlyFixed && column.Width.IsAbsolute)
                    {
                        onlyFixed = true;
                        result = measurement.MeasuredResult;
                    }
                    else if (onlyFixed == column.Width.IsAbsolute)
                        result = Math.Max(result, measurement.MeasuredResult);

                    result = Math.Max(result, column.MinWidth);
                }
                if (measurement.Definition is RowDefinition row)
                {
                    if (!onlyFixed && row.Height.IsAbsolute)
                    {
                        onlyFixed = true;
                        result = measurement.MeasuredResult;
                    }
                    else if (onlyFixed == row.Height.IsAbsolute)
                        result = Math.Max(result, measurement.MeasuredResult);

                    result = Math.Max(result, row.MinHeight);
                }
            }

            return result;
        }


        (List<GridLayout.LengthConvention>, List<double>, double) Arrange(IReadOnlyList<DefinitionBase> definitions, GridLayout.MeasureResult measureResult)
        {
            var conventions = measureResult.LeanLengthList.ToList();
            var lengths = measureResult.LengthList.ToList();
            var desiredLength = 0.0;
            for (int i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (string.IsNullOrEmpty(definition.SharedSizeGroup))
                {
                    desiredLength += measureResult.LengthList[i];
                    continue;
                }

                var group = _groups[definition.SharedSizeGroup];

                var length = Gather(group.Results);

                conventions[i] = new GridLayout.LengthConvention(
                    new GridLength(length),
                    measureResult.LeanLengthList[i].MinLength,
                    measureResult.LeanLengthList[i].MaxLength
                );
                lengths[i] = length;
                desiredLength += length;
            }

            return (conventions, lengths, desiredLength);
        }

        internal (GridLayout.MeasureResult, GridLayout.MeasureResult) HandleArrange(Grid grid, GridLayout.MeasureResult rowResult, GridLayout.MeasureResult columnResult)
        {
            var (rowConventions, rowLengths, rowDesiredLength) = Arrange(grid.RowDefinitions, rowResult);
            var (columnConventions, columnLengths, columnDesiredLength) = Arrange(grid.ColumnDefinitions, columnResult);

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
                AddToGroup(scopeName, result);
            }
        }

        private void AddToGroup(string scopeName, MeasurementResult result)
        {
            if (string.IsNullOrEmpty(scopeName))
                return;

            if (!_groups.TryGetValue(scopeName, out var group))
                _groups.Add(scopeName, group = new Group());

            group.IsFixed |= IsFixed(result.Definition);

            group.Results.Add(result);
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
                RemoveFromGroup(scopeName, result);
            }
        }

        private void RemoveFromGroup(string scopeName, MeasurementResult result)
        {
            if (string.IsNullOrEmpty(scopeName))
                return;

            Debug.Assert(_groups.TryGetValue(scopeName, out var group));

            group.Results.Remove(result);
            if (!group.Results.Any())
                _groups.Remove(scopeName);
            else
            {
                group.IsFixed = group.Results.Select(r => r.Definition).Any(IsFixed);
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
            Debug.Assert(!_measurementCaches.Any(mc => ReferenceEquals(mc.Grid, toAdd)));
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
}
