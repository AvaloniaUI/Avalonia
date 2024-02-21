﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Diagnostics;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ItemsSourceViewTests
    {
        [Fact]
        public void Only_Subscribes_To_Source_CollectionChanged_When_CollectionChanged_Subscribed()
        {
            var source = new AvaloniaList<string>();
            var target = ItemsSourceView.GetOrCreate(source);
            var debug = (INotifyCollectionChangedDebug)source;

            Assert.Null(debug.GetCollectionChangedSubscribers());

            void Handler(object sender, NotifyCollectionChangedEventArgs e) { }
            target.CollectionChanged += Handler;

            Assert.NotNull(debug.GetCollectionChangedSubscribers());
            Assert.Equal(1, debug.GetCollectionChangedSubscribers().Length);

            target.CollectionChanged -= Handler;

            Assert.Null(debug.GetCollectionChangedSubscribers());
        }

        [Fact]
        public void Cannot_Create_ItemsSourceView_With_Collection_That_Implements_INCC_But_Not_List()
        {
            var source = new InvalidCollection();
            Assert.Throws<ArgumentException>(() => ItemsSourceView.GetOrCreate(source));
        }

        [Fact]
        public void Reassigning_Source_Unsubscribes_From_Previous_Source()
        {
            var source = new AvaloniaList<string>();
            var target = new ReassignableItemsSourceView(source);
            var debug = (INotifyCollectionChangedDebug)source;

            target.CollectionChanged += (s, e) => { };

            Assert.Equal(1, debug.GetCollectionChangedSubscribers().Length);

            target.SetSource(new string[0]);

            Assert.Null(debug.GetCollectionChangedSubscribers());
        }

        [Fact]
        public void Reassigning_Source_Subscribes_To_New_Source()
        {
            var source = new AvaloniaList<string>();
            var target = new ReassignableItemsSourceView(new string[0]);
            var debug = (INotifyCollectionChangedDebug)source;

            target.CollectionChanged += (s, e) => { };
            target.SetSource(source);

            Assert.Equal(1, debug.GetCollectionChangedSubscribers().Length);
        }

        [Fact]
        public void Filtered_View_Adds_New_Items()
        {
            var source = new AvaloniaList<string>() { "foo", "bar" };
            var target = ItemsSourceView.GetOrCreate(source);

            var collectionChangeEvents = new List<NotifyCollectionChangedEventArgs>();

            target.CollectionChanged += (s, e) => collectionChangeEvents.Add(e);

            target.Filters.Add(new FunctionItemFilter { Filter = (s, e) => e.Accept = !Equals(bool.FalseString, e.Item) });
            
            Assert.Equal(1, collectionChangeEvents.Count);
            Assert.Equal(NotifyCollectionChangedAction.Reset, collectionChangeEvents[0].Action);
            collectionChangeEvents.Clear();

            source.Add(bool.FalseString);

            Assert.Empty(collectionChangeEvents);

            Assert.Equal(new int[] { 0, 1, -1 }, ItemsSourceView.GetDiagnosticItemMap(target));

            source.InsertRange(1, new[] { bool.TrueString, bool.TrueString });

            Assert.Equal(new int[] { 0, 1, 2, 3, -1 }, ItemsSourceView.GetDiagnosticItemMap(target));

            Assert.Equal(1, collectionChangeEvents.Count);
            Assert.Equal(NotifyCollectionChangedAction.Add, collectionChangeEvents[0].Action);
            Assert.Equal(1, collectionChangeEvents[0].NewStartingIndex);
            Assert.Equal(2, collectionChangeEvents[0].NewItems.Count);
            Assert.Equal(bool.TrueString, collectionChangeEvents[0].NewItems[0]);

            Assert.Equal(4, target.Count);
            Assert.Equal(bool.TrueString, target[1]);
            Assert.Equal(bool.TrueString, target[2]);

            source.Add(bool.TrueString);

            Assert.Equal(new int[] { 0, 1, 2, 3, -1, 4 }, ItemsSourceView.GetDiagnosticItemMap(target));

            Assert.Equal(5, target.Count);
            Assert.Equal(bool.TrueString, target[^1]);
        }

        [Fact]
        public void Filtered_View_Removes_Old_Items()
        {            
            var source = new AvaloniaList<string>() { "foo", "bar", bool.TrueString, bool.FalseString, bool.TrueString, "end" };
            var target = ItemsSourceView.GetOrCreate(source);

            var collectionChangeEvents = new List<NotifyCollectionChangedEventArgs>();

            target.CollectionChanged += (s, e) => collectionChangeEvents.Add(e);

            target.Filters.Add(new FunctionItemFilter { Filter = (s, e) => e.Accept = !Equals(bool.FalseString, e.Item) });

            Assert.Equal(1, collectionChangeEvents.Count);
            Assert.Equal(NotifyCollectionChangedAction.Reset, collectionChangeEvents[0].Action);
            collectionChangeEvents.Clear();

            Assert.Equal(new int[] { 0, 1, 2, -1, 3, 4 }, ItemsSourceView.GetDiagnosticItemMap(target));

            source.RemoveAt(4);

            Assert.Equal(4, target.Count);
            Assert.Equal(new int[] { 0, 1, 2, -1, 3 }, ItemsSourceView.GetDiagnosticItemMap(target));

            Assert.Equal(1, collectionChangeEvents.Count);
            Assert.Equal(NotifyCollectionChangedAction.Remove, collectionChangeEvents[0].Action);
            Assert.Equal(3, collectionChangeEvents[0].OldStartingIndex);
            Assert.Equal(1, collectionChangeEvents[0].OldItems.Count);
            Assert.Equal(bool.TrueString, collectionChangeEvents[0].OldItems[0]);
            collectionChangeEvents.Clear();

            source.RemoveAt(3);
            Assert.Empty(collectionChangeEvents);
            Assert.Equal(4, target.Count);

            Assert.Equal(new int[] { 0, 1, 2, 3 }, ItemsSourceView.GetDiagnosticItemMap(target));
        }

        [Fact]
        public void Filtered_View_Resets_When_Source_Cleared()
        {            
            var source = new AvaloniaList<string>() { "foo", "bar" };
            var target = ItemsSourceView.GetOrCreate(source);

            var collectionChangeEvents = new List<NotifyCollectionChangedEventArgs>();

            target.CollectionChanged += (s, e) => collectionChangeEvents.Add(e);

            target.Filters.Add(new FunctionItemFilter { Filter = (s, e) => e.Accept = !Equals(bool.FalseString, e.Item) });
            
            Assert.Equal(1, collectionChangeEvents.Count);
            Assert.Equal(NotifyCollectionChangedAction.Reset, collectionChangeEvents[0].Action);
            collectionChangeEvents.Clear();

            source.Clear();

            Assert.Equal(0, target.Count);
            Assert.Equal(Array.Empty<int>(), ItemsSourceView.GetDiagnosticItemMap(target));
            Assert.Equal(1, collectionChangeEvents.Count);
            Assert.Equal(NotifyCollectionChangedAction.Reset, collectionChangeEvents[0].Action);
        }

        [Fact]
        public void ComparableSorter_Sorts_Integers()
        {
            var random = new Random();
            var source = new AvaloniaList<int>(Enumerable.Repeat(0, 100).Select(i => random.Next(int.MinValue + 1, int.MaxValue)));
            var target = ItemsSourceView.GetOrCreate(source);

            var collectionChangeEvents = new List<NotifyCollectionChangedEventArgs>();

            target.CollectionChanged += (s, e) => collectionChangeEvents.Add(e);

            target.Sorters.Add(new ComparableSorter());
            
            Assert.Equal(1, collectionChangeEvents.Count);
            Assert.Equal(NotifyCollectionChangedAction.Reset, collectionChangeEvents[0].Action);
            collectionChangeEvents.Clear();

            Assert.Equal(target.Cast<int>().OrderBy(i => i), target);

            source.Add(int.MinValue);

            Assert.Equal(target[0], int.MinValue);

            source.Insert(0, int.MaxValue);

            Assert.Equal(target[target.Count - 1], int.MaxValue);
        }

        [Fact]
        public void Disabled_Layers_Update_View_When_Activated()
        {
            var random = new Random();
            var source = new AvaloniaList<int>(Enumerable.Repeat(0, 100));
            var target = ItemsSourceView.GetOrCreate(source);

            var collectionChangeEvents = new List<NotifyCollectionChangedEventArgs>();

            target.CollectionChanged += (s, e) => collectionChangeEvents.Add(e);

            var filter = new FunctionItemFilter { IsActive = false, Filter = (s, e) => e.Accept = (int)e.Item % 2 == 0 };
            target.Filters.Add(filter);

            var sorter = new ComparableSorter() { IsActive = false, SortDirection = ListSortDirection.Descending };
            target.Sorters.Add(sorter);
            
            Assert.Equal(0, collectionChangeEvents.Count);
            
            Assert.Equal(source, target);

            sorter.IsActive = true;

            Assert.Equal(1, collectionChangeEvents.Count);
            Assert.Equal(NotifyCollectionChangedAction.Reset, collectionChangeEvents[0].Action);
            collectionChangeEvents.Clear();

            Assert.Equal(source.Reverse(), target);

            filter.IsActive = true;

            Assert.Equal(1, collectionChangeEvents.Count);
            Assert.Equal(NotifyCollectionChangedAction.Reset, collectionChangeEvents[0].Action);
            collectionChangeEvents.Clear();

            Assert.Equal(source.Reverse().Where((i, _) => i % 2 == 0), target);

            sorter.IsActive = false;

            Assert.Equal(1, collectionChangeEvents.Count);
            Assert.Equal(NotifyCollectionChangedAction.Reset, collectionChangeEvents[0].Action);
            collectionChangeEvents.Clear();

            Assert.Equal(source.Where((i, _) => i % 2 == 0), target);
        }

        [Fact]
        public void Layers_Refreshed_When_InvalidationProperty_Changes()
        {
            var source = new AvaloniaList<ViewModel>(Enumerable.Repeat(0, 5).Select(i => new ViewModel()));
            var target = ItemsSourceView.GetOrCreate(source);
            var collectionChangeEvents = new List<NotifyCollectionChangedEventArgs>();

            target.CollectionChanged += (s, e) => collectionChangeEvents.Add(e);

            target.Filters.Add(new FunctionItemFilter
            {
                Filter = (s, e) => e.Accept = ((ViewModel)e.Item).PassesFilter,
                InvalidationPropertyNames = new() { nameof(ViewModel.PassesFilter) },
            });

            target.Sorters.Add(new ComparableSorter
            {
                ComparableSelector = (s, e) => e.Comparable = ((ViewModel)e.Item).LastModified,
                InvalidationPropertyNames = new() { nameof(ViewModel.LastModified) },
            });

            foreach (var vm in source)
                Assert.Equal(1, vm.PropertyChangedSubscriberCount); // One event subscription should be shared between all layers

            Assert.Equal(2, collectionChangeEvents.Count);
            Assert.Equal(NotifyCollectionChangedAction.Reset, collectionChangeEvents[0].Action);
            Assert.Equal(NotifyCollectionChangedAction.Reset, collectionChangeEvents[1].Action);
            collectionChangeEvents.Clear();

            source[3].PassesFilter = true;

            Assert.Equal(1, collectionChangeEvents.Count);
            Assert.Equal(NotifyCollectionChangedAction.Add, collectionChangeEvents[0].Action);
            Assert.Equal(0, collectionChangeEvents[0].NewStartingIndex);
            Assert.Equal(new[] { source[3] }, collectionChangeEvents[0].NewItems);
            Assert.Equal(source[3], target[0]);
            collectionChangeEvents.Clear();

            source[0].PassesFilter = true;

            Assert.Equal(new[] { source[3], source[0] }, target); // source[0] comes last because it was modified more recently
        }

        private class ViewModel : INotifyPropertyChanged
        {
            private bool _passesFilter;
            public bool PassesFilter
            {
                get => _passesFilter;
                set
                {
                    _passesFilter = value;
                    PropertyChanged?.Invoke(this, new(nameof(PassesFilter)));

                    LastModified = DateTimeOffset.Now;
                    PropertyChanged?.Invoke(this, new(nameof(LastModified)));
                }
            }

            public DateTimeOffset? LastModified { get; private set; }

            public event PropertyChangedEventHandler PropertyChanged;

            public int PropertyChangedSubscriberCount => PropertyChanged?.GetInvocationList().Length ?? 0;
        }

        private class InvalidCollection : INotifyCollectionChanged, IEnumerable<string>
        {
            public event NotifyCollectionChangedEventHandler CollectionChanged { add { } remove { } }

            public IEnumerator<string> GetEnumerator()
            {
                yield break;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                yield break;
            }
        }

        private class ReassignableItemsSourceView : ItemsSourceView
        {
            public ReassignableItemsSourceView(IEnumerable source)
                : base(null, source)
            {
            }

            public new void SetSource(IEnumerable source) => base.SetSource(source);
        }
    }
}
