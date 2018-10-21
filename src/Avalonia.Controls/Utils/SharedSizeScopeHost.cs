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
using Avalonia.Layout;
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
                    .Select(d => new MeasurementResult(grid, d))
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
                    var result = Results.Single(mr => ReferenceEquals(mr.Definition, propertyChanged.Item1));
                    var oldName = result.SizeGroup?.Name;
                    var newName = (propertyChanged.Item1 as DefinitionBase).SharedSizeGroup;
                    _groupChanged.OnNext((oldName, newName, result));
                }
            }

            private void DefinitionsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                int offset = 0;
                if (sender is ColumnDefinitions)
                    offset = Grid.RowDefinitions.Count;

                var newItems = e.NewItems?.OfType<DefinitionBase>().Select(db => new MeasurementResult(Grid, db)).ToList() ?? new List<MeasurementResult>();
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
                            .Select(d => new MeasurementResult(Grid, d))
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
                    Results[i].MinLength = rowResult.MinLengths[i];
                }

                for (int i = 0; i < Grid.ColumnDefinitions.Count; i++)
                {
                    Results[i + Grid.RowDefinitions.Count].MeasuredResult = columnResult.LengthList[i];
                    Results[i + Grid.RowDefinitions.Count].MinLength = columnResult.MinLengths[i];
                }
            }

            public void InvalidateMeasure()
            {
                MeasurementState = MeasurementState.Invalidated;
                Results.ForEach(r =>
                {
                    r.MeasuredResult = double.NaN;
                    r.SizeGroup?.Reset();
                });
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

        private class MeasurementResult
        {
            public MeasurementResult(Grid owningGrid, DefinitionBase definition)
            {
                OwningGrid = owningGrid;
                Definition = definition;
                MeasuredResult = double.NaN;
            }

            public DefinitionBase Definition { get; }
            public double MeasuredResult { get; set; }
            public double MinLength { get; set; }
            public Group SizeGroup { get; set; }
            public Grid OwningGrid { get; }

            public (double length, int priority) GetPriorityLength()
            {
                var length = (Definition as ColumnDefinition)?.Width ?? ((RowDefinition)Definition).Height;

                if (length.IsAbsolute)
                    return (MeasuredResult, 1);
                if (length.IsAuto)
                    return (MeasuredResult, 2);
                if (MinLength > 0)
                    return (MinLength, 3);
                return (MeasuredResult, 4);
            }
        }


        private class LentgthGatherer
        {
            public double Length { get; private set; }
            private int gatheredPriority = 6;

            public void Visit(MeasurementResult result)
            {
                var (length, priority) = result.GetPriorityLength();

                if (gatheredPriority < priority)
                    return;

                gatheredPriority = priority;
                if (gatheredPriority == priority)
                {
                    Length = Math.Max(length,Length);
                }
                else
                {
                    Length = length;
                }
            }
        }


        private class Group
        {
            private double? cachedResult;
            private List<MeasurementResult> _results = new List<MeasurementResult>(); 

            public string Name { get; }

            public Group(string name)
            {
                Name = name;
            }

            public bool IsFixed { get; set; }

            public IReadOnlyList<MeasurementResult> Results => _results;

            public double CalculatedLength => (cachedResult ?? (cachedResult = Gather())).Value;

            public void Reset()
            {
                cachedResult = null;
            }

            public void Add(MeasurementResult result)
            {
                if (_results.Contains(result))
                    throw new AvaloniaInternalException(
                        $"SharedSizeScopeHost: Invalid call to Group.Add - The SharedSizeGroup {Name} already contains the passed result");

                result.SizeGroup = this;
                _results.Add(result);
            }

            public void Remove(MeasurementResult result)
            {
                if (!_results.Contains(result))
                    throw new AvaloniaInternalException(
                        $"SharedSizeScopeHost: Invalid call to Group.Remove - The SharedSizeGroup {Name} does not contain the passed result");
                result.SizeGroup = null;
                _results.Remove(result);
            }


            private double Gather()
            {
                var visitor = new LentgthGatherer();

                _results.ForEach(visitor.Visit);

                return visitor.Length;
            }
        }

        private readonly AvaloniaList<MeasurementCache> _measurementCaches;

        private readonly Dictionary<string, Group> _groups = new Dictionary<string, Group>();

        public SharedSizeScopeHost(Control scope)
        {
            _measurementCaches = GetParticipatingGrids(scope);

            foreach (var cache in _measurementCaches)
            {
                cache.Grid.InvalidateMeasure();
                AddGridToScopes(cache);

            }
        }

        void SharedGroupChanged((string oldName, string newName, MeasurementResult result) change)
        {
            RemoveFromGroup(change.oldName, change.result);
            AddToGroup(change.newName, change.result);
        }

        private bool _invalidating;

        internal void InvalidateMeasure(Grid grid)
        {
            // prevent stack overflow
            if (_invalidating)
                return;
            _invalidating = true;

            InvalidateMeasureImpl(grid);

            _invalidating = false;
        }

        private void InvalidateMeasureImpl(Grid grid)
        {
            var cache = _measurementCaches.FirstOrDefault(mc => ReferenceEquals(mc.Grid, grid));

            if (cache == null)
                throw new AvaloniaInternalException(
                    $"SharedSizeScopeHost: InvalidateMeasureImpl - called with a grid not present in the internal cache");

            // already invalidated the cache, early out.
            if (cache.MeasurementState == MeasurementState.Invalidated)
                return;

            cache.InvalidateMeasure();

            // maybe there is a condition to only call arrange on some of the calls?
            grid.InvalidateMeasure();

            // find all the scopes within the invalidated grid
            var scopeNames = cache.Results
                                  .Where(mr => mr.SizeGroup != null)
                                  .Select(mr => mr.SizeGroup.Name)
                                  .Distinct();
            // find all grids related to those scopes
            var otherGrids = scopeNames.SelectMany(sn => _groups[sn].Results)
                                       .Select(r => r.OwningGrid)
                                       .Where(g => g.IsMeasureValid)
                                       .Distinct();

            // invalidate them as well
            foreach (var otherGrid in otherGrids)
            {
                InvalidateMeasureImpl(otherGrid);
            }
        }

        internal void UpdateMeasureStatus(Grid grid, GridLayout.MeasureResult rowResult, GridLayout.MeasureResult columnResult)
        {
            var cache = _measurementCaches.FirstOrDefault(mc => ReferenceEquals(mc.Grid, grid));
            if (cache == null)
                throw new AvaloniaInternalException("SharedSizeScopeHost: Attempted to update measurement status for a grid that wasn't registered!");

            cache.UpdateMeasureResult(rowResult, columnResult);
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

                var length = group.CalculatedLength;

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
                    rowLengths,
                    rowResult.MinLengths),
                new GridLayout.MeasureResult(
                    columnResult.ContainerLength,
                    columnDesiredLength,
                    columnResult.GreedyDesiredLength, //??
                    columnConventions,
                    columnLengths,
                    columnResult.MinLengths)
            );
        }


        private void AddGridToScopes(MeasurementCache cache)
        {
            cache.GroupChanged.Subscribe(SharedGroupChanged);

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
                _groups.Add(scopeName, group = new Group(scopeName));

            group.IsFixed |= IsFixed(result.Definition);

            group.Add(result);
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

            if (!_groups.TryGetValue(scopeName, out var group))
                throw new AvaloniaInternalException($"SharedSizeScopeHost: The scope {scopeName} wasn't found in the shared size scope");

            group.Remove(result);
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
                cache.Dispose();
            }
        }

        internal void RegisterGrid(Grid toAdd)
        {
            if (_measurementCaches.Any(mc => ReferenceEquals(mc.Grid, toAdd)))
                throw new AvaloniaInternalException("SharedSizeScopeHost: tried to register a grid twice!");

            var cache = new MeasurementCache(toAdd);
            _measurementCaches.Add(cache);
            AddGridToScopes(cache);
        }

        internal void UnegisterGrid(Grid toRemove)
        {
            var cache = _measurementCaches.FirstOrDefault(mc => ReferenceEquals(mc.Grid, toRemove));
            if (cache == null)
                throw new AvaloniaInternalException("SharedSizeScopeHost: tried to unregister a grid that wasn't registered before!");

            _measurementCaches.Remove(cache);
            RemoveGridFromScopes(cache);
            cache.Dispose();
        }

        internal bool ParticipatesInScope(Grid toCheck)
        {
            return _measurementCaches.FirstOrDefault(mc => ReferenceEquals(mc.Grid, toCheck))?.Results.Any() ?? false;
        }
    }
}
