using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Controls.Selection;
using Xunit;

namespace Avalonia.Controls.UnitTests.Selection
{
    public class InternalSelectionModelTests
    {
        [Fact]
        public void Selecting_Item_Adds_To_WritableSelectedItems()
        {
            var target = CreateTarget();

            target.Select(0);

            Assert.Equal(new[] { "foo" }, target.WritableSelectedItems);
        }

        [Fact]
        public void Selecting_Duplicate_On_Model_Adds_To_WritableSelectedItems()
        {
            var target = CreateTarget(source: new[] { "foo", "bar", "baz", "foo", "bar", "baz" });

            target.SelectRange(1, 4);

            Assert.Equal(new[] { "bar", "baz", "foo", "bar" }, target.WritableSelectedItems);
        }

        [Fact]
        public void Deselecting_On_Model_Removes_SelectedItem()
        {
            var target = CreateTarget();

            target.SelectRange(1, 2);
            target.Deselect(1);

            Assert.Equal(new[] { "baz" }, target.WritableSelectedItems);
        }

        [Fact]
        public void Deselecting_Duplicate_On_Model_Removes_SelectedItem()
        {
            var target = CreateTarget(source: new[] { "foo", "bar", "baz", "foo", "bar", "baz" });

            target.SelectRange(1, 2);
            target.Select(4);
            target.Deselect(4);

            Assert.Equal(new[] { "baz", "bar" }, target.WritableSelectedItems);
        }

        [Fact]
        public void Adding_To_WritableSelectedItems_Selects_On_Model()
        {
            var target = CreateTarget();

            target.SelectRange(1, 2);
            target.WritableSelectedItems.Add("foo");

            Assert.Equal(new[] { 0, 1, 2 }, target.SelectedIndexes);
            Assert.Equal(new[] { "bar", "baz", "foo" }, target.WritableSelectedItems);
        }

        [Fact]
        public void Removing_From_WritableSelectedItems_Deselects_On_Model()
        {
            var target = CreateTarget();

            target.SelectRange(1, 2);
            target.WritableSelectedItems.Remove("baz");

            Assert.Equal(new[] { 1 }, target.SelectedIndexes);
            Assert.Equal(new[] { "bar" }, target.WritableSelectedItems);
        }

        [Fact]
        public void Replacing_SelectedItem_Updates_Model()
        {
            var target = CreateTarget();

            target.SelectRange(1, 2);
            target.WritableSelectedItems[0] = "foo";

            Assert.Equal(new[] { 0, 2 }, target.SelectedIndexes);
            Assert.Equal(new[] { "foo", "baz" }, target.WritableSelectedItems);
        }

        [Fact]
        public void Clearing_WritableSelectedItems_Updates_Model()
        {
            var target = CreateTarget();

            target.WritableSelectedItems.Clear();

            Assert.Empty(target.SelectedIndexes);
            Assert.Empty(target.WritableSelectedItems);
        }

        [Fact]
        public void Setting_WritableSelectedItems_Updates_Model()
        {
            var target = CreateTarget();
            var oldItems = target.WritableSelectedItems;

            var newItems = new AvaloniaList<string> { "foo", "baz" };
            target.WritableSelectedItems = newItems;

            Assert.Equal(new[] { 0, 2 }, target.SelectedIndexes);
            Assert.Same(newItems, target.WritableSelectedItems);
            Assert.NotSame(oldItems, target.WritableSelectedItems);
            Assert.Equal(new[] { "foo", "baz" }, newItems);
        }

        [Fact]
        public void Setting_Items_To_Null_Clears_Selection()
        {
            var target = CreateTarget();

            target.SelectRange(1, 2);
            target.WritableSelectedItems = null;

            Assert.Empty(target.SelectedIndexes);
            Assert.Empty(target.WritableSelectedItems);
        }

        [Fact]
        public void Setting_Items_To_Null_Creates_Empty_Items()
        {
            var target = CreateTarget();
            var oldItems = target.WritableSelectedItems;

            target.WritableSelectedItems = null;

            Assert.NotNull(target.WritableSelectedItems);
            Assert.NotSame(oldItems, target.WritableSelectedItems);
            Assert.IsType<AvaloniaList<object>>(target.WritableSelectedItems);
        }

        [Fact]
        public void Adds_Null_WritableSelectedItems_When_Source_Is_Null()
        {
            var target = CreateTarget(nullSource: true);

            target.SelectRange(1, 2);
            Assert.Equal(new object[] { null, null }, target.WritableSelectedItems);
        }

        [Fact]
        public void Updates_WritableSelectedItems_When_Source_Changes_From_Null()
        {
            var target = CreateTarget(nullSource: true);

            target.SelectRange(1, 2);
            Assert.Equal(new object[] { null, null }, target.WritableSelectedItems);

            target.Source = new[] { "foo", "bar", "baz" };
            Assert.Equal(new[] { "bar", "baz" }, target.WritableSelectedItems);
        }

        [Fact]
        public void Updates_WritableSelectedItems_When_Source_Changes_To_Null()
        {
            var target = CreateTarget();

            target.SelectRange(1, 2);
            Assert.Equal(new[] { "bar", "baz" }, target.WritableSelectedItems);

            target.Source = null;
            Assert.Equal(new object[] { null, null }, target.WritableSelectedItems);
        }

        [Fact]
        public void WritableSelectedItems_Can_Be_Set_Before_Source()
        {
            var target = CreateTarget(nullSource: true);
            var items = new AvaloniaList<string> { "foo", "bar", "baz" };
            var WritableSelectedItems = new AvaloniaList<string> { "bar" };

            target.WritableSelectedItems = WritableSelectedItems;
            target.Source = items;

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal(new[] { "bar" }, target.WritableSelectedItems);
        }

        [Fact]
        public void Does_Not_Accept_Fixed_Size_Items()
        {
            var target = CreateTarget();

            Assert.Throws<NotSupportedException>(() =>
                target.WritableSelectedItems = new[] { "foo", "bar", "baz" });
        }

        [Fact]
        public void Restores_Selection_On_Items_Reset()
        {
            var items = new ResettingCollection(new[] { "foo", "bar", "baz" });
            var target = CreateTarget(source: items);

            target.SelectedIndex = 1;
            items.Reset(new[] { "baz", "foo", "bar" });

            Assert.Equal(2, target.SelectedIndex);
            Assert.Equal(new[] { "bar" }, target.WritableSelectedItems);
        }

        [Fact]
        public void Raises_Selection_Changed_On_Items_Reset()
        {
            var items = new ResettingCollection(new[] { "foo", "bar", "baz" });
            var target = CreateTarget(source: items);

            target.SelectedIndex = 1;

            var changed = new List<string>();

            target.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            var oldSelectedIndex = target.SelectedIndex;
            var oldSelectedItem = target.SelectedItem;

            items.Reset(new string[0]);

            Assert.NotEqual(oldSelectedIndex, target.SelectedIndex);
            Assert.NotEqual(oldSelectedItem, target.SelectedItem);

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Equal(null, target.SelectedItem);
            Assert.Empty(target.WritableSelectedItems);

            Assert.Contains(nameof(target.SelectedIndex), changed);
            Assert.Contains(nameof(target.SelectedItem), changed);
        }

        [Fact]
        public void Preserves_SelectedItem_On_Items_Reset()
        {
            var items = new ResettingCollection(new[] { "foo", "bar", "baz" });
            var target = CreateTarget(source: items);

            target.SelectedItem = "foo";

            Assert.Equal(0, target.SelectedIndex);

            items.Reset(new string[] { "baz", "foo", "bar" });

            Assert.Equal("foo", target.SelectedItem);
            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal(new[] { "foo" }, target.WritableSelectedItems);
        }

        [Fact]
        public void Preserves_Selection_On_Source_Changed()
        {
            var target = CreateTarget();

            target.SelectedIndex = 1;
            target.Source = new[] { "baz", "foo", "bar" };

            Assert.Equal(2, target.SelectedIndex);
            Assert.Equal(new[] { "bar" }, target.WritableSelectedItems);
        }

        private static InternalSelectionModel CreateTarget(
            bool singleSelect = false,
            IList source = null,
            bool nullSource = false)
        {
            source ??= !nullSource ? new[] { "foo", "bar", "baz" } : null;

            var result = new InternalSelectionModel
            {
                SingleSelect = singleSelect,
            };

            ((ISelectionModel)result).Source = source;
            return result;
        }

        private class ResettingCollection : List<string>, INotifyCollectionChanged
        {
            public ResettingCollection(IEnumerable<string> items)
            {
                AddRange(items);
            }

            public void Reset(IEnumerable<string> items)
            {
                Clear();
                AddRange(items);
                CollectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            public event NotifyCollectionChangedEventHandler CollectionChanged;
        }
    }
}
