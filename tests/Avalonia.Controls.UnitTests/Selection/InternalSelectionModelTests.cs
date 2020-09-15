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
        public void Selecting_Item_Adds_To_SelectedItems()
        {
            var target = CreateTarget();

            target.Select(0);

            Assert.Equal(new[] { "foo" }, target.SelectedItems);
        }

        [Fact]
        public void Selecting_Duplicate_On_Model_Adds_To_SelectedItems()
        {
            var target = CreateTarget(source: new[] { "foo", "bar", "baz", "foo", "bar", "baz" });

            target.SelectRange(1, 4);

            Assert.Equal(new[] { "bar", "baz", "foo", "bar" }, target.SelectedItems);
        }

        [Fact]
        public void Deselecting_On_Model_Removes_SelectedItem()
        {
            var target = CreateTarget();

            target.SelectRange(1, 2);
            target.Deselect(1);

            Assert.Equal(new[] { "baz" }, target.SelectedItems);
        }

        [Fact]
        public void Deselecting_Duplicate_On_Model_Removes_SelectedItem()
        {
            var target = CreateTarget(source: new[] { "foo", "bar", "baz", "foo", "bar", "baz" });

            target.SelectRange(1, 2);
            target.Select(4);
            target.Deselect(4);

            Assert.Equal(new[] { "baz", "bar" }, target.SelectedItems);
        }

        [Fact]
        public void Adding_To_SelectedItems_Selects_On_Model()
        {
            var target = CreateTarget();

            target.SelectRange(1, 2);
            target.SelectedItems.Add("foo");

            Assert.Equal(new[] { 0, 1, 2 }, target.SelectedIndexes);
            Assert.Equal(new[] { "bar", "baz", "foo" }, target.SelectedItems);
        }

        [Fact]
        public void Removing_From_SelectedItems_Deselects_On_Model()
        {
            var target = CreateTarget();

            target.SelectRange(1, 2);
            target.SelectedItems.Remove("baz");

            Assert.Equal(new[] { 1 }, target.SelectedIndexes);
            Assert.Equal(new[] { "bar" }, target.SelectedItems);
        }

        [Fact]
        public void Replacing_SelectedItem_Updates_Model()
        {
            var target = CreateTarget();

            target.SelectRange(1, 2);
            target.SelectedItems[0] = "foo";

            Assert.Equal(new[] { 0, 2 }, target.SelectedIndexes);
            Assert.Equal(new[] { "foo", "baz" }, target.SelectedItems);
        }

        [Fact]
        public void Clearing_SelectedItems_Updates_Model()
        {
            var target = CreateTarget();

            target.SelectedItems.Clear();

            Assert.Empty(target.SelectedIndexes);
        }

        [Fact]
        public void Setting_SelectedItems_Updates_Model()
        {
            var target = CreateTarget();
            var oldItems = target.SelectedItems;

            var newItems = new AvaloniaList<string> { "foo", "baz" };
            target.SelectedItems = newItems;

            Assert.Equal(new[] { 0, 2 }, target.SelectedIndexes);
            Assert.Same(newItems, target.SelectedItems);
            Assert.NotSame(oldItems, target.SelectedItems);
            Assert.Equal(new[] { "foo", "baz" }, newItems);
        }

        [Fact]
        public void Setting_Items_To_Null_Clears_Selection()
        {
            var target = CreateTarget();

            target.SelectRange(1, 2);
            target.SelectedItems = null;

            Assert.Empty(target.SelectedIndexes);
        }

        [Fact]
        public void Setting_Items_To_Null_Creates_Empty_Items()
        {
            var target = CreateTarget();
            var oldItems = target.SelectedItems;

            target.SelectedItems = null;

            Assert.NotNull(target.SelectedItems);
            Assert.NotSame(oldItems, target.SelectedItems);
            Assert.IsType<AvaloniaList<object>>(target.SelectedItems);
        }

        [Fact]
        public void Adds_Null_SelectedItems_When_Source_Is_Null()
        {
            var target = CreateTarget(nullSource: true);

            target.SelectRange(1, 2);
            Assert.Equal(new object[] { null, null }, target.SelectedItems);
        }

        [Fact]
        public void Updates_SelectedItems_When_Source_Changes_From_Null()
        {
            var target = CreateTarget(nullSource: true);

            target.SelectRange(1, 2);
            Assert.Equal(new object[] { null, null }, target.SelectedItems);

            target.Source = new[] { "foo", "bar", "baz" };
            Assert.Equal(new[] { "bar", "baz" }, target.SelectedItems);
        }

        [Fact]
        public void Updates_SelectedItems_When_Source_Changes_To_Null()
        {
            var target = CreateTarget();

            target.SelectRange(1, 2);
            Assert.Equal(new[] { "bar", "baz" }, target.SelectedItems);

            target.Source = null;
            Assert.Equal(new object[] { null, null }, target.SelectedItems);
        }

        [Fact]
        public void SelectedItems_Can_Be_Set_Before_Source()
        {
            var target = CreateTarget(nullSource: true);
            var items = new AvaloniaList<string> { "foo", "bar", "baz" };
            var selectedItems = new AvaloniaList<string> { "bar" };

            target.SelectedItems = selectedItems;
            target.Source = items;

            Assert.Equal(1, target.SelectedIndex);
        }

        [Fact]
        public void Does_Not_Accept_Fixed_Size_Items()
        {
            var target = CreateTarget();

            Assert.Throws<NotSupportedException>(() =>
                target.SelectedItems = new[] { "foo", "bar", "baz" });
        }

        [Fact]
        public void Restores_Selection_On_Items_Reset()
        {
            var items = new ResettingCollection(new[] { "foo", "bar", "baz" });
            var target = CreateTarget(source: items);

            target.SelectedIndex = 1;
            items.Reset(new[] { "baz", "foo", "bar" });

            Assert.Equal(2, target.SelectedIndex);
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
