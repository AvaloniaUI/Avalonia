// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Data.Core;
using Xunit;

namespace Avalonia.Base.UnitTests.Collections
{
    public class AvaloniaDictionaryTests
    {
        [Fact]
        public void Adding_Item_Should_Raise_CollectionChanged()
        {
            var target = new AvaloniaDictionary<string, string>();
            var tracker = new CollectionChangedTracker(target);

            target.Add("foo", "bar");

            Assert.NotNull(tracker.Args);
            Assert.Equal(NotifyCollectionChangedAction.Add, tracker.Args.Action);
            Assert.Equal(-1, tracker.Args.NewStartingIndex);
            Assert.Equal(1, tracker.Args.NewItems.Count);
            Assert.Equal(new KeyValuePair<string, string>("foo", "bar"), tracker.Args.NewItems[0]);
        }

        [Fact]
        public void Adding_Item_Should_Raise_PropertyChanged()
        {
            var target = new AvaloniaDictionary<string, string>();
            var tracker = new PropertyChangedTracker(target);

            target.Add("foo", "bar");

            Assert.Equal(new[] { "Count", "Item[foo]" }, tracker.Names);
        }

        [Fact]
        public void Assigning_Item_Should_Raise_CollectionChanged_Add()
        {
            var target = new AvaloniaDictionary<string, string>();
            var tracker = new CollectionChangedTracker(target);

            target["foo"] = "bar";

            Assert.NotNull(tracker.Args);
            Assert.Equal(NotifyCollectionChangedAction.Add, tracker.Args.Action);
            Assert.Equal(-1, tracker.Args.NewStartingIndex);
            Assert.Equal(1, tracker.Args.NewItems.Count);
            Assert.Equal(new KeyValuePair<string, string>("foo", "bar"), tracker.Args.NewItems[0]);
        }

        [Fact]
        public void Assigning_Item_Should_Raise_CollectionChanged_Replace()
        {
            var target = new AvaloniaDictionary<string, string>();

            target["foo"] = "baz";
            var tracker = new CollectionChangedTracker(target);
            target["foo"] = "bar";

            Assert.NotNull(tracker.Args);
            Assert.Equal(NotifyCollectionChangedAction.Replace, tracker.Args.Action);
            Assert.Equal(-1, tracker.Args.NewStartingIndex);
            Assert.Equal(1, tracker.Args.NewItems.Count);
            Assert.Equal(new KeyValuePair<string, string>("foo", "bar"), tracker.Args.NewItems[0]);
        }

        [Fact]
        public void Assigning_Item_Should_Raise_PropertyChanged_Add()
        {
            var target = new AvaloniaDictionary<string, string>();
            var tracker = new PropertyChangedTracker(target);

            target["foo"] = "bar";

            Assert.Equal(new[] { "Count", "Item[foo]" }, tracker.Names);
        }

        [Fact]
        public void Assigning_Item_Should_Raise_PropertyChanged_Replace()
        {
            var target = new AvaloniaDictionary<string, string>();

            target["foo"] = "baz";
            var tracker = new PropertyChangedTracker(target);
            target["foo"] = "bar";

            Assert.Equal(new[] { "Item[foo]" }, tracker.Names);
        }

        [Fact]
        public void Removing_Item_Should_Raise_CollectionChanged()
        {
            var target = new AvaloniaDictionary<string, string>();

            target["foo"] = "bar";
            var tracker = new CollectionChangedTracker(target);
            target.Remove("foo");

            Assert.NotNull(tracker.Args);
            Assert.Equal(NotifyCollectionChangedAction.Remove, tracker.Args.Action);
            Assert.Equal(-1, tracker.Args.OldStartingIndex);
            Assert.Equal(1, tracker.Args.OldItems.Count);
            Assert.Equal(new KeyValuePair<string, string>("foo", "bar"), tracker.Args.OldItems[0]);
        }

        [Fact]
        public void Removing_Item_Should_Raise_PropertyChanged()
        {
            var target = new AvaloniaDictionary<string, string>();

            target["foo"] = "bar";
            var tracker = new PropertyChangedTracker(target);
            target.Remove("foo");

            Assert.Equal(new[] { "Count", "Item[foo]" }, tracker.Names);
        }

        [Fact]
        public void Clearing_Collection_Should_Raise_CollectionChanged()
        {
            var target = new AvaloniaDictionary<string, string>();

            target["foo"] = "bar";
            target["baz"] = "qux";
            var tracker = new CollectionChangedTracker(target);
            target.Clear();

            Assert.NotNull(tracker.Args);
            Assert.Equal(NotifyCollectionChangedAction.Remove, tracker.Args.Action);
            Assert.Equal(-1, tracker.Args.OldStartingIndex);
            Assert.Equal(2, tracker.Args.OldItems.Count);
            Assert.Equal(new KeyValuePair<string, string>("foo", "bar"), tracker.Args.OldItems[0]);
        }

        [Fact]
        public void Clearing_Collection_Should_Raise_PropertyChanged()
        {
            var target = new AvaloniaDictionary<string, string>();

            target["foo"] = "bar";
            target["baz"] = "qux";
            var tracker = new PropertyChangedTracker(target);
            target.Clear();

            Assert.Equal(new[] { "Count", CommonPropertyNames.IndexerName }, tracker.Names);
        }
    }
}
