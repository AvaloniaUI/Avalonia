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
    /// <summary>
    /// Shared size scope implementation.
    /// Shares the size information between participating grids.
    /// An instance of this class is attached to every <see cref="Control"/> that has its
    /// IsSharedSizeScope property set to true.
    /// </summary>
    internal sealed class SharedSizeScopeHost : IDisposable
    {
        private enum MeasurementState
        {
            Invalidated,
            Measuring,
            Cached
        }

        /// <summary>
        /// Class containing the measured rows/columns for a single grid.
        /// Monitors changes to the row/column collections as well as the SharedSizeGroup changes
        /// for the individual items in those collections.
        /// Notifies the <see cref="SharedSizeScopeHost"/> of SharedSizeGroup changes.
        /// </summary>
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

            // method to be hooked up once RowDefinitions/ColumnDefinitions collections can be replaced on a grid
            private void DefinitionsChanged(object sender, AvaloniaPropertyChangedEventArgs e)
            {
                // route to collection changed as a Reset.
                DefinitionsCollectionChanged(null, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
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
                var oldItems = e.OldStartingIndex >= 0 
                                    ? Results.GetRange(e.OldStartingIndex + offset, e.OldItems.Count) 
                                    : new List<MeasurementResult>();

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


            /// <summary>
            /// Updates the Results collection with Grid Measure results. 
            /// </summary>
            /// <param name="rowResult">Result of the GridLayout.Measure method for the RowDefinitions in the grid.</param>
            /// <param name="columnResult">Result of the GridLayout.Measure method for the ColumnDefinitions in the grid.</param>
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

            /// <summary>
            /// Clears the measurement cache, in preparation for the Measure pass.
            /// </summary>
            public void InvalidateMeasure()
            {
                var newItems = new List<MeasurementResult>();
                var oldItems = new List<MeasurementResult>();

                MeasurementState = MeasurementState.Invalidated;

                Results.ForEach(r =>
                {
                    r.MeasuredResult = double.NaN;
                    r.SizeGroup?.Reset();
                });
            }

            /// <summary>
            /// Clears the <see cref="IObservable{T}"/> subscriptions.
            /// </summary>
            public void Dispose()
            {
                _subscriptions.Dispose();
                _groupChanged.OnCompleted();
            }

            /// <summary>
            /// Gets the <see cref="Grid"/> for which this cache has been created.
            /// </summary>
            public Grid Grid { get; }
            
            /// <summary>
            /// Gets the <see cref="MeasurementState"/> of this cache.
            /// </summary>
            public MeasurementState MeasurementState { get; private set; }

            /// <summary>
            /// Gets the list of <see cref="MeasurementResult"/> instances.
            /// </summary>
            /// <remarks>
            /// The list is a 1-1 map of the concatenation of RowDefinitions and ColumnDefinitions
            /// </remarks>
            public List<MeasurementResult> Results { get; private set; }
        }


        /// <summary>
        /// Class containing the Measure result for a single Row/Column in a grid.
        /// </summary>
        private class MeasurementResult
        {
            public MeasurementResult(Grid owningGrid, DefinitionBase definition)
            {
                OwningGrid = owningGrid;
                Definition = definition;
                MeasuredResult = double.NaN;
            }

            /// <summary>
            /// Gets the <see cref="RowDefinition"/>/<see cref="ColumnDefinition"/> related to this <see cref="MeasurementResult"/>
            /// </summary>
            public DefinitionBase Definition { get; }
            
            /// <summary>
            /// Gets or sets the actual result of the Measure operation for this column.
            /// </summary>
            public double MeasuredResult { get; set; }
            
            /// <summary>
            /// Gets or sets the Minimum constraint for a Row/Column - relevant for star Rows/Columns in unconstrained grids.
            /// </summary>
            public double MinLength { get; set; }
            
            /// <summary>
            /// Gets or sets the <see cref="Group"/> that this result belongs to.
            /// </summary>
            public Group SizeGroup { get; set; }
            
            /// <summary>
            /// Gets the Grid that is the parent of the Row/Column
            /// </summary>
            public Grid OwningGrid { get; }

            /// <summary>
            /// Calculates the effective length that this Row/Column wishes to enforce in the SharedSizeGroup.
            /// </summary>
            /// <returns>A tuple of length and the priority in the shared size group.</returns>
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

        /// <summary>
        /// Visitor class used to gather the final length for a given SharedSizeGroup.
        /// </summary>
        /// <remarks>
        /// The values are applied according to priorities defined in <see cref="MeasurementResult.GetPriorityLength"/>.
        /// </remarks>
        private class LentgthGatherer
        {
            /// <summary>
            /// Gets the final Length to be applied to every Row/Column in a SharedSizeGroup
            /// </summary>
            public double Length { get; private set; }
            private int gatheredPriority = 6;

            /// <summary>
            /// Visits the <paramref name="result"/> applying the result of <see cref="MeasurementResult.GetPriorityLength"/> to its internal cache.
            /// </summary>
            /// <param name="result">The <see cref="MeasurementResult"/> instance to visit.</param>
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

        /// <summary>
        /// Representation of a SharedSizeGroup, containing Rows/Columns with the same SharedSizeGroup property value.
        /// </summary>
        private class Group
        {
            private double? cachedResult;
            private List<MeasurementResult> _results = new List<MeasurementResult>(); 

            /// <summary>
            /// Gets the name of the SharedSizeGroup.
            /// </summary>
            public string Name { get; }

            public Group(string name)
            {
                Name = name;
            }

            /// <summary>
            /// Gets the collection of the <see cref="MeasurementResult"/> instances.
            /// </summary>
            public IReadOnlyList<MeasurementResult> Results => _results;

            /// <summary>
            /// Gets the final, calculated length for all Rows/Columns in the SharedSizeGroup.
            /// </summary>
            public double CalculatedLength => (cachedResult ?? (cachedResult = Gather())).Value;

            /// <summary>
            /// Clears the previously cached result in preparation for measurement.
            /// </summary>
            public void Reset()
            {
                cachedResult = null;
            }

            /// <summary>
            /// Ads a measurement result to this group and sets it's <see cref="MeasurementResult.SizeGroup"/> property
            /// to this instance.
            /// </summary>
            /// <param name="result">The <see cref="MeasurementResult"/> to include in this group.</param>
            public void Add(MeasurementResult result)
            {
                if (_results.Contains(result))
                    throw new AvaloniaInternalException(
                        $"SharedSizeScopeHost: Invalid call to Group.Add - The SharedSizeGroup {Name} already contains the passed result");

                result.SizeGroup = this;
                _results.Add(result);
            }

            /// <summary>
            /// Removes the measurement result from this group and clears its <see cref="MeasurementResult.SizeGroup"/> value.
            /// </summary>
            /// <param name="result">The <see cref="MeasurementResult"/> to clear.</param>
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

        private readonly AvaloniaList<MeasurementCache> _measurementCaches = new AvaloniaList<MeasurementCache>();
        private readonly Dictionary<string, Group> _groups = new Dictionary<string, Group>();
        private bool _invalidating;

        /// <summary>
        /// Removes the SharedSizeScope and notifies all affected grids of the change.
        /// </summary>
        public void Dispose()
        {
            while (_measurementCaches.Any())
                _measurementCaches[0].Grid.SharedScopeChanged();
        }

        /// <summary>
        /// Registers the grid in this SharedSizeScope, to be called when the grid is added to the visual tree. 
        /// </summary>
        /// <param name="toAdd">The <see cref="Grid"/> to add to this scope.</param>
        internal void RegisterGrid(Grid toAdd)
        {
            if (_measurementCaches.Any(mc => ReferenceEquals(mc.Grid, toAdd)))
                throw new AvaloniaInternalException("SharedSizeScopeHost: tried to register a grid twice!");

            var cache = new MeasurementCache(toAdd);
            _measurementCaches.Add(cache);
            AddGridToScopes(cache);
        }

        /// <summary>
        /// Removes the registration for a grid in this SharedSizeScope.
        /// </summary>
        /// <param name="toRemove">The <see cref="Grid"/> to remove.</param>
        internal void UnegisterGrid(Grid toRemove)
        {
            var cache = _measurementCaches.FirstOrDefault(mc => ReferenceEquals(mc.Grid, toRemove));
            if (cache == null)
                throw new AvaloniaInternalException("SharedSizeScopeHost: tried to unregister a grid that wasn't registered before!");

            _measurementCaches.Remove(cache);
            RemoveGridFromScopes(cache);
            cache.Dispose();
        }

        /// <summary>
        /// Helper method to check if a grid needs to forward its Mesure results to, and requrest Arrange results from this scope.
        /// </summary>
        /// <param name="toCheck">The <see cref="Grid"/> that should be checked.</param>
        /// <returns>True if the grid should forward its calls.</returns>
        internal bool ParticipatesInScope(Grid toCheck)
        {
            return _measurementCaches.FirstOrDefault(mc => ReferenceEquals(mc.Grid, toCheck))
                                    ?.Results.Any(r => r.SizeGroup != null) ?? false;
        }

        /// <summary>
        /// Notifies the SharedSizeScope that a grid had requested its measurement to be invalidated.
        /// Forwards the same call to all affected grids in this scope.
        /// </summary>
        /// <param name="grid">The <see cref="Grid"/> that had it's Measure invalidated.</param>
        internal void InvalidateMeasure(Grid grid)
        {
            // prevent stack overflow
            if (_invalidating)
                return;
            _invalidating = true;

            InvalidateMeasureImpl(grid);

            _invalidating = false;
        }

        /// <summary>
        /// Updates the measurement cache with the results of the <paramref name="grid"/> measurement pass.
        /// </summary>
        /// <param name="grid">The <see cref="Grid"/> that has been measured.</param>
        /// <param name="rowResult">Measurement result for the grid's <see cref="RowDefinitions"/></param>
        /// <param name="columnResult">Measurement result for the grid's <see cref="ColumnDefinitions"/></param>
        internal void UpdateMeasureStatus(Grid grid, GridLayout.MeasureResult rowResult, GridLayout.MeasureResult columnResult)
        {
            var cache = _measurementCaches.FirstOrDefault(mc => ReferenceEquals(mc.Grid, grid));
            if (cache == null)
                throw new AvaloniaInternalException("SharedSizeScopeHost: Attempted to update measurement status for a grid that wasn't registered!");

            cache.UpdateMeasureResult(rowResult, columnResult);
        }

        /// <summary>
        /// Calculates the measurement result including the impact of any SharedSizeGroups that might affect this grid.
        /// </summary>
        /// <param name="grid">The <see cref="Grid"/> that is being Arranged</param>
        /// <param name="rowResult">The <paramref name="grid"/>'s cached measurement result.</param>
        /// <param name="columnResult">The <paramref name="grid"/>'s cached measurement result.</param>
        /// <returns>Row and column measurement result updated with the SharedSizeScope constraints.</returns>
        internal (GridLayout.MeasureResult, GridLayout.MeasureResult) HandleArrange(Grid grid, GridLayout.MeasureResult rowResult, GridLayout.MeasureResult columnResult)
        {
            return (
                 Arrange(grid.RowDefinitions, rowResult),
                 Arrange(grid.ColumnDefinitions, columnResult)
                 );
        }

        /// <summary>
        /// Invalidates the measure of all grids affected by the SharedSizeGroups contained within.
        /// </summary>
        /// <param name="grid">The <see cref="Grid"/> that is being invalidated.</param>
        private void InvalidateMeasureImpl(Grid grid)
        {
            var cache = _measurementCaches.FirstOrDefault(mc => ReferenceEquals(mc.Grid, grid));

            if (cache == null)
                throw new AvaloniaInternalException(
                    $"SharedSizeScopeHost: InvalidateMeasureImpl - called with a grid not present in the internal cache");

            // already invalidated the cache, early out.
            if (cache.MeasurementState == MeasurementState.Invalidated)
                return;

            // we won't calculate, so we should not invalidate.
            if (!ParticipatesInScope(grid))
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

        /// <summary>
        /// <see cref="IObserver{T}"/> callback notifying the scope that a <see cref="MeasurementResult"/> has changed its
        /// SharedSizeGroup
        /// </summary>
        /// <param name="change">Old and New name (either can be null) of the SharedSizeGroup, as well as the result.</param>
        private void SharedGroupChanged((string oldName, string newName, MeasurementResult result) change)
        {
            RemoveFromGroup(change.oldName, change.result);
            AddToGroup(change.newName, change.result);
        }

        /// <summary>
        /// Handles the impact of SharedSizeGroups on the Arrange of <see cref="RowDefinitions"/>/<see cref="ColumnDefinitions"/>
        /// </summary>
        /// <param name="definitions">Rows/Columns that were measured</param>
        /// <param name="measureResult">The initial measurement result.</param>
        /// <returns>Modified measure result</returns>
        private GridLayout.MeasureResult Arrange(IReadOnlyList<DefinitionBase> definitions, GridLayout.MeasureResult measureResult)
        {
            var conventions = measureResult.LeanLengthList.ToList();
            var lengths = measureResult.LengthList.ToList();
            var desiredLength = 0.0;
            for (int i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];

                // for empty SharedSizeGroups pass on unmodified result.
                if (string.IsNullOrEmpty(definition.SharedSizeGroup))
                {
                    desiredLength += measureResult.LengthList[i];
                    continue;
                }

                var group = _groups[definition.SharedSizeGroup];
                // Length calculated over all Definitions participating in a SharedSizeGroup.
                var length = group.CalculatedLength;

                conventions[i] = new GridLayout.LengthConvention(
                    new GridLength(length),
                    measureResult.LeanLengthList[i].MinLength,
                    measureResult.LeanLengthList[i].MaxLength
                );
                lengths[i] = length;
                desiredLength += length;
            }

            return new GridLayout.MeasureResult(
                    measureResult.ContainerLength,
                    desiredLength,
                    measureResult.GreedyDesiredLength,//??
                    conventions,
                    lengths,
                    measureResult.MinLengths);
        }

        /// <summary>
        /// Adds all measurement results for a grid to their repsective scopes.
        /// </summary>
        /// <param name="cache">The <see cref="MeasurementCache"/> for a grid to be added.</param>
        private void AddGridToScopes(MeasurementCache cache)
        {
            cache.GroupChanged.Subscribe(SharedGroupChanged);

            foreach (var result in cache.Results)
            {
                var scopeName = result.Definition.SharedSizeGroup;
                AddToGroup(scopeName, result);
            }
        }

        /// <summary>
        /// Handles adding the <see cref="MeasurementResult"/> to a SharedSizeGroup.
        /// Does nothing for empty SharedSizeGroups.
        /// </summary>
        /// <param name="scopeName">The name (can be null or empty) of the group to add the <paramref name="result"/> to.</param>
        /// <param name="result">The <see cref="MeasurementResult"/> to add to a scope.</param>
        private void AddToGroup(string scopeName, MeasurementResult result)
        {
            if (string.IsNullOrEmpty(scopeName))
                return;

            if (!_groups.TryGetValue(scopeName, out var group))
                _groups.Add(scopeName, group = new Group(scopeName));

            group.Add(result);
        }

        /// <summary>
        /// Removes all measurement results for a grid from their respective scopes.
        /// </summary>
        /// <param name="cache">The <see cref="MeasurementCache"/> for a grid to be removed.</param>
        private void RemoveGridFromScopes(MeasurementCache cache)
        {
            foreach (var result in cache.Results)
            {
                var scopeName = result.Definition.SharedSizeGroup;
                RemoveFromGroup(scopeName, result);
            }
        }

        /// <summary>
        /// Handles removing the <see cref="MeasurementResult"/> from a SharedSizeGroup.
        /// Does nothing for empty SharedSizeGroups.
        /// </summary>
        /// <param name="scopeName">The name (can be null or empty) of the group to remove the <paramref name="result"/> from.</param>
        /// <param name="result">The <see cref="MeasurementResult"/> to remove from a scope.</param>
        private void RemoveFromGroup(string scopeName, MeasurementResult result)
        {
            if (string.IsNullOrEmpty(scopeName))
                return;

            if (!_groups.TryGetValue(scopeName, out var group))
                throw new AvaloniaInternalException($"SharedSizeScopeHost: The scope {scopeName} wasn't found in the shared size scope");

            group.Remove(result);
            if (!group.Results.Any())
                _groups.Remove(scopeName);
        }
    }
}
