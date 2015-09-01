// -----------------------------------------------------------------------
// <copyright file="PerspexDictionaryTests.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Base.UnitTests.Collections
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using Perspex.Collections;
    using Xunit;

    public class PerspexDictionaryTests
    {
        [Fact]
        public void Adding_Item_Should_Raise_CollectionChanged()
        {
            var target = new PerspexDictionary<string, string>();
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
            var target = new PerspexDictionary<string, string>();
            var tracker = new PropertyChangedTracker(target);

            target.Add("foo", "bar");

            Assert.Equal(new[] { "Count", "Item[foo]" }, tracker.Names);
        }

        [Fact]
        public void Assigning_Item_Should_Raise_CollectionChanged_Add()
        {
            var target = new PerspexDictionary<string, string>();
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
            var target = new PerspexDictionary<string, string>();

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
            var target = new PerspexDictionary<string, string>();
            var tracker = new PropertyChangedTracker(target);

            target["foo"] = "bar";

            Assert.Equal(new[] { "Count", "Item[foo]" }, tracker.Names);
        }

        [Fact]
        public void Assigning_Item_Should_Raise_PropertyChanged_Replace()
        {
            var target = new PerspexDictionary<string, string>();

            target["foo"] = "baz";
            var tracker = new PropertyChangedTracker(target);
            target["foo"] = "bar";

            Assert.Equal(new[] { "Item[foo]" }, tracker.Names);
        }

        [Fact]
        public void Removing_Item_Should_Raise_CollectionChanged()
        {
            var target = new PerspexDictionary<string, string>();

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
            var target = new PerspexDictionary<string, string>();

            target["foo"] = "bar";
            var tracker = new PropertyChangedTracker(target);
            target.Remove("foo");

            Assert.Equal(new[] { "Count", "Item[foo]" }, tracker.Names);
        }

        [Fact]
        public void Clearing_Collection_Should_Raise_CollectionChanged()
        {
            var target = new PerspexDictionary<string, string>();

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
            var target = new PerspexDictionary<string, string>();

            target["foo"] = "bar";
            target["baz"] = "qux";
            var tracker = new PropertyChangedTracker(target);
            target.Clear();

            Assert.Equal(new[] { "Count", "Item[]" }, tracker.Names);
        }
    }
}
