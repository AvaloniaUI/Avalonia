using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Utils;
using Xunit;

namespace Avalonia.Controls.UnitTests.Utils
{
    public class SelectedItemsSyncTests
    {
        [Fact]
        public void Initial_Items_Are_From_Model()
        {
            var target = CreateTarget();
            var items = target.SelectedItems;

            Assert.Equal(new[] { "bar", "baz" }, items);
        }

        [Fact]
        public void Selecting_On_Model_Adds_Item()
        {
            var target = CreateTarget();
            var items = target.SelectedItems;

            target.SelectionModel.Select(0);

            Assert.Equal(new[] { "bar", "baz", "foo" }, items);
        }

        [Fact]
        public void Selecting_Duplicate_On_Model_Adds_Item()
        {
            var target = CreateTarget(new[] { "foo", "bar", "baz", "foo", "bar", "baz" });
            var items = target.SelectedItems;

            target.SelectionModel.Select(4);

            Assert.Equal(new[] { "bar", "baz", "bar" }, items);
        }

        [Fact]
        public void Deselecting_On_Model_Removes_Item()
        {
            var target = CreateTarget();
            var items = target.SelectedItems;

            target.SelectionModel.Deselect(1);

            Assert.Equal(new[] { "baz" }, items);
        }

        [Fact]
        public void Deselecting_Duplicate_On_Model_Removes_Item()
        {
            var target = CreateTarget(new[] { "foo", "bar", "baz", "foo", "bar", "baz" });
            var items = target.SelectedItems;

            target.SelectionModel.Select(4);
            target.SelectionModel.Deselect(4);

            Assert.Equal(new[] { "baz", "bar" }, items);
        }

        [Fact]
        public void Reassigning_Model_Resets_Items()
        {
            var target = CreateTarget();
            var items = target.SelectedItems;

            var newModel = new SelectionModel<string> 
            { 
                Source = (string[])target.SelectionModel.Source,
                SingleSelect = false 
            };

            newModel.Select(0);
            newModel.Select(1);

            target.SelectionModel = newModel;

            Assert.Equal(new[] { "foo", "bar" }, items);
        }

        [Fact]
        public void Reassigning_Model_Tracks_New_Model()
        {
            var target = CreateTarget();
            var items = target.SelectedItems;

            var newModel = new SelectionModel<string>
            {
                Source = (string[])target.SelectionModel.Source,
                SingleSelect = false
            };

            target.SelectionModel = newModel;

            newModel.Select(0);
            newModel.Select(1);

            Assert.Equal(new[] { "foo", "bar" }, items);
        }

        [Fact]
        public void Adding_To_Items_Selects_On_Model()
        {
            var target = CreateTarget();
            var items = target.SelectedItems;

            items.Add("foo");

            Assert.Equal(new[] { 0, 1, 2 }, target.SelectionModel.SelectedIndexes);
            Assert.Equal(new[] { "bar", "baz", "foo" }, items);
        }

        [Fact]
        public void Removing_From_Items_Deselects_On_Model()
        {
            var target = CreateTarget();
            var items = target.SelectedItems;

            items.Remove("baz");

            Assert.Equal(new[] { 1 }, target.SelectionModel.SelectedIndexes);
            Assert.Equal(new[] { "bar" }, items);
        }

        [Fact]
        public void Replacing_Item_Updates_Model()
        {
            var target = CreateTarget();
            var items = target.SelectedItems;

            items[0] = "foo";

            Assert.Equal(new[] { 0, 2 }, target.SelectionModel.SelectedIndexes);
            Assert.Equal(new[] { "foo", "baz" }, items);
        }

        [Fact]
        public void Clearing_Items_Updates_Model()
        {
            var target = CreateTarget();
            var items = target.SelectedItems;

            items.Clear();

            Assert.Empty(target.SelectionModel.SelectedIndexes);
        }

        [Fact]
        public void Setting_Items_Updates_Model()
        {
            var target = CreateTarget();
            var oldItems = target.SelectedItems;

            var newItems = new AvaloniaList<string> { "foo", "baz" };
            target.SelectedItems = newItems;

            Assert.Equal(new[] { 0, 2 }, target.SelectionModel.SelectedIndexes);
            Assert.Same(newItems, target.SelectedItems);
            Assert.NotSame(oldItems, target.SelectedItems);
            Assert.Equal(new[] { "foo", "baz" }, newItems);
        }

        [Fact]
        public void Setting_Items_Subscribes_To_Model()
        {
            var target = CreateTarget();
            var items = new AvaloniaList<string> { "foo", "baz" };

            target.SelectedItems = items;
            target.SelectionModel.Select(1);

            Assert.Equal(new[] { "foo", "baz", "bar" }, items);
        }

        [Fact]
        public void Setting_Items_To_Null_Creates_Empty_Items()
        {
            var target = CreateTarget();
            var oldItems = target.SelectedItems;

            target.SelectedItems = null;

            var newItems = Assert.IsType<AvaloniaList<object>>(target.SelectedItems);

            Assert.NotSame(oldItems, newItems);
        }

        [Fact]
        public void Handles_Null_Model_Source()
        {
            var model = new SelectionModel<string> { SingleSelect = false };
            model.Select(1);

            var target = new SelectedItemsSync(model);
            var items = target.SelectedItems;

            Assert.Empty(items);

            model.Select(2);
            model.Source = new[] { "foo", "bar", "baz" };

            Assert.Equal(new[] { "bar", "baz" }, items);
        }

        [Fact]
        public void Does_Not_Accept_Fixed_Size_Items()
        {
            var target = CreateTarget();

            Assert.Throws<NotSupportedException>(() =>
                target.SelectedItems = new[] { "foo", "bar", "baz" });
        }

        [Fact]
        public void Selected_Items_Can_Be_Set_Before_SelectionModel_Source()
        {
            var model = new SelectionModel<string>();
            var target = new SelectedItemsSync(model);
            var items = new AvaloniaList<string> { "foo", "bar", "baz" };
            var selectedItems = new AvaloniaList<string> { "bar" };

            target.SelectedItems = selectedItems;
            model.Source = items;

            Assert.Equal(1, model.SelectedIndex);
        }

        [Fact]
        public void Restores_Selection_On_Items_Reset()
        {
            var items = new ResettingCollection(new[] { "foo", "bar", "baz" });
            var model = new SelectionModel<string> { Source = items };
            var target = new SelectedItemsSync(model);

            model.SelectedIndex = 1;
            items.Reset(new[] { "baz", "foo", "bar" });

            Assert.Equal(2, model.SelectedIndex);
        }

        private static SelectedItemsSync CreateTarget(
            IEnumerable<string> items = null)
        {
            items ??= new[] { "foo", "bar", "baz" };

            var model = new SelectionModel<string> { Source = items, SingleSelect = false };
            model.SelectRange(1, 2);

            var target = new SelectedItemsSync(model);
            return target;
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
