using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Collections;
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
            var items = target.GetOrCreateItems();

            Assert.Equal(new[] { "bar", "baz" }, items);
        }

        [Fact]
        public void Selecting_On_Model_Adds_Item()
        {
            var target = CreateTarget();
            var items = target.GetOrCreateItems();

            target.Model.Select(0);

            Assert.Equal(new[] { "bar", "baz", "foo" }, items);
        }

        [Fact]
        public void Selecting_Duplicate_On_Model_Adds_Item()
        {
            var target = CreateTarget(new[] { "foo", "bar", "baz", "foo", "bar", "baz" });
            var items = target.GetOrCreateItems();

            target.Model.Select(4);

            Assert.Equal(new[] { "bar", "baz", "bar" }, items);
        }

        [Fact]
        public void Deselecting_On_Model_Removes_Item()
        {
            var target = CreateTarget();
            var items = target.GetOrCreateItems();

            target.Model.Deselect(1);

            Assert.Equal(new[] { "baz" }, items);
        }

        [Fact]
        public void Deselecting_Duplicate_On_Model_Removes_Item()
        {
            var target = CreateTarget(new[] { "foo", "bar", "baz", "foo", "bar", "baz" });
            var items = target.GetOrCreateItems();

            target.Model.Select(4);
            target.Model.Deselect(4);

            Assert.Equal(new[] { "baz", "bar" }, items);
        }

        [Fact]
        public void Reassigning_Model_Resets_Items()
        {
            var target = CreateTarget();
            var items = target.GetOrCreateItems();

            var newModel = new SelectionModel { Source = target.Model.Source };
            newModel.Select(0);
            newModel.Select(1);

            target.SetModel(newModel);

            Assert.Equal(new[] { "foo", "bar" }, items);
        }

        [Fact]
        public void Reassigning_Model_Tracks_New_Model()
        {
            var target = CreateTarget();
            var items = target.GetOrCreateItems();

            var newModel = new SelectionModel { Source = target.Model.Source };
            target.SetModel(newModel);

            newModel.Select(0);
            newModel.Select(1);

            Assert.Equal(new[] { "foo", "bar" }, items);
        }

        [Fact]
        public void Adding_To_Items_Selects_On_Model()
        {
            var target = CreateTarget();
            var items = target.GetOrCreateItems();

            items.Add("foo");

            Assert.Equal(
                new[] { new IndexPath(0), new IndexPath(1), new IndexPath(2) },
                target.Model.SelectedIndices);
            Assert.Equal(new[] { "bar", "baz", "foo" }, items);
        }

        [Fact]
        public void Removing_From_Items_Deselects_On_Model()
        {
            var target = CreateTarget();
            var items = target.GetOrCreateItems();

            items.Remove("baz");

            Assert.Equal(new[] { new IndexPath(1) }, target.Model.SelectedIndices);
            Assert.Equal(new[] { "bar" }, items);
        }

        [Fact]
        public void Replacing_Item_Updates_Model()
        {
            var target = CreateTarget();
            var items = target.GetOrCreateItems();

            items[0] = "foo";

            Assert.Equal(new[] { new IndexPath(0), new IndexPath(2) }, target.Model.SelectedIndices);
            Assert.Equal(new[] { "foo", "baz" }, items);
        }

        [Fact]
        public void Clearing_Items_Updates_Model()
        {
            var target = CreateTarget();
            var items = target.GetOrCreateItems();

            items.Clear();

            Assert.Empty(target.Model.SelectedIndices);
        }

        [Fact]
        public void Setting_Items_Updates_Model()
        {
            var target = CreateTarget();
            var oldItems = target.GetOrCreateItems();

            var newItems = new AvaloniaList<string> { "foo", "baz" };
            target.SetItems(newItems);

            Assert.Equal(new[] { new IndexPath(0), new IndexPath(2) }, target.Model.SelectedIndices);
            Assert.Same(newItems, target.GetOrCreateItems());
            Assert.NotSame(oldItems, target.GetOrCreateItems());
            Assert.Equal(new[] { "foo", "baz" }, newItems);
        }

        [Fact]
        public void Setting_Items_Subscribes_To_Model()
        {
            var target = CreateTarget();
            var items = new AvaloniaList<string> { "foo", "baz" };

            target.SetItems(items);
            target.Model.Select(1);

            Assert.Equal(new[] { "foo", "baz", "bar" }, items);
        }

        [Fact]
        public void Setting_Items_To_Null_Creates_Empty_Items()
        {
            var target = CreateTarget();
            var oldItems = target.GetOrCreateItems();

            target.SetItems(null);

            var newItems = Assert.IsType<AvaloniaList<object>>(target.GetOrCreateItems());

            Assert.NotSame(oldItems, newItems);
        }

        [Fact]
        public void Handles_Null_Model_Source()
        {
            var model = new SelectionModel();
            model.Select(1);

            var target = new SelectedItemsSync(model);
            var items = target.GetOrCreateItems();

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
                target.SetItems(new[] { "foo", "bar", "baz" }));
        }

        [Fact]
        public void Selected_Items_Can_Be_Set_Before_SelectionModel_Source()
        {
            var model = new SelectionModel();
            var target = new SelectedItemsSync(model);
            var items = new AvaloniaList<string> { "foo", "bar", "baz" };
            var selectedItems = new AvaloniaList<string> { "bar" };

            target.SetItems(selectedItems);
            model.Source = items;

            Assert.Equal(new IndexPath(1), model.SelectedIndex);
        }

        private static SelectedItemsSync CreateTarget(
            IEnumerable<string> items = null)
        {
            items ??= new[] { "foo", "bar", "baz" };

            var model = new SelectionModel { Source = items };
            model.SelectRange(new IndexPath(1), new IndexPath(2));

            var target = new SelectedItemsSync(model);
            return target;
        }
    }
}
