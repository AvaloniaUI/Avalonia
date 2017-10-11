// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Data;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class SelectingItemsControlTests_Multiple
    {
        [Fact]
        public void Setting_SelectedIndex_Should_Add_To_SelectedItems()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar" },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 1;

            Assert.Equal(new[] { "bar" }, target.SelectedItems.Cast<object>().ToList());
        }

        [Fact]
        public void Adding_SelectedItems_Should_Set_SelectedIndex()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar" },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedItems.Add("bar");

            Assert.Equal(1, target.SelectedIndex);
        }

        [Fact]
        public void Assigning_SelectedItems_Should_Set_SelectedIndex()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar" },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedItems = new AvaloniaList<object>("bar");

            Assert.Equal(1, target.SelectedIndex);
        }

        [Fact]
        public void Reassigning_SelectedItems_Should_Clear_Selection()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar" },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedItems.Add("bar");
            target.SelectedItems = new AvaloniaList<object>();

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Null(target.SelectedItem);
        }

        [Fact]
        public void Adding_First_SelectedItem_Should_Raise_SelectedIndex_SelectedItem_Changed()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar" },
                Template = Template(),
            };

            bool indexRaised = false;
            bool itemRaised = false;
            target.PropertyChanged += (s, e) =>
            {
                indexRaised |= e.Property.Name == "SelectedIndex" &&
                    (int)e.OldValue == -1 &&
                    (int)e.NewValue == 1;
                itemRaised |= e.Property.Name == "SelectedItem" &&
                    (string)e.OldValue == null &&
                    (string)e.NewValue == "bar";
            };

            target.ApplyTemplate();
            target.SelectedItems.Add("bar");

            Assert.True(indexRaised);
            Assert.True(itemRaised);
        }

        [Fact]
        public void Adding_Subsequent_SelectedItems_Should_Not_Raise_SelectedIndex_SelectedItem_Changed()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar" },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedItems.Add("foo");

            bool raised = false;
            target.PropertyChanged += (s, e) => 
                raised |= e.Property.Name == "SelectedIndex" ||
                          e.Property.Name == "SelectedItem";

            target.SelectedItems.Add("bar");

            Assert.False(raised);
        }

        [Fact]
        public void Removing_Last_SelectedItem_Should_Raise_SelectedIndex_Changed()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar" },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedItems.Add("foo");

            bool raised = false;
            target.PropertyChanged += (s, e) => 
                raised |= e.Property.Name == "SelectedIndex" && 
                          (int)e.OldValue == 0 && 
                          (int)e.NewValue == -1;

            target.SelectedItems.RemoveAt(0);

            Assert.True(raised);
        }

        [Fact]
        public void Adding_SelectedItems_Should_Set_Item_IsSelected()
        {
            var items = new[]
            {
                new ListBoxItem(),
                new ListBoxItem(),
                new ListBoxItem(),
            };

            var target = new TestSelector
            {
                Items = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            target.SelectedItems.Add(items[0]);
            target.SelectedItems.Add(items[1]);

            var foo = target.Presenter.Panel.Children[0];

            Assert.True(items[0].IsSelected);
            Assert.True(items[1].IsSelected);
            Assert.False(items[2].IsSelected);
        }

        [Fact]
        public void Assigning_SelectedItems_Should_Set_Item_IsSelected()
        {
            var items = new[]
            {
                new ListBoxItem(),
                new ListBoxItem(),
                new ListBoxItem(),
            };

            var target = new TestSelector
            {
                Items = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            target.SelectedItems = new AvaloniaList<object> { items[0], items[1] };

            Assert.True(items[0].IsSelected);
            Assert.True(items[1].IsSelected);
            Assert.False(items[2].IsSelected);
        }

        [Fact]
        public void Removing_SelectedItems_Should_Clear_Item_IsSelected()
        {
            var items = new[]
            {
                new ListBoxItem(),
                new ListBoxItem(),
                new ListBoxItem(),
            };

            var target = new TestSelector
            {
                Items = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            target.SelectedItems.Add(items[0]);
            target.SelectedItems.Add(items[1]);
            target.SelectedItems.Remove(items[1]);

            Assert.True(items[0].IsSelected);
            Assert.False(items[1].IsSelected);
        }

        [Fact]
        public void Reassigning_SelectedItems_Should_Clear_Item_IsSelected()
        {
            var items = new[]
            {
                new ListBoxItem(),
                new ListBoxItem(),
                new ListBoxItem(),
            };

            var target = new TestSelector
            {
                Items = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedItems.Add(items[0]);
            target.SelectedItems.Add(items[1]);

            target.SelectedItems = new AvaloniaList<object> { items[0], items[1] };

            Assert.False(items[0].IsSelected);
            Assert.False(items[1].IsSelected);
        }

        [Fact]
        public void Replacing_First_SelectedItem_Should_Update_SelectedItem_SelectedIndex()
        {
            var items = new[]
            {
                new ListBoxItem(),
                new ListBoxItem(),
                new ListBoxItem(),
            };

            var target = new TestSelector
            {
                Items = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            target.SelectedIndex = 1;
            target.SelectedItems[0] = items[2];

            Assert.Equal(2, target.SelectedIndex);
            Assert.Equal(items[2], target.SelectedItem);
            Assert.False(items[0].IsSelected);
            Assert.False(items[1].IsSelected);
            Assert.True(items[2].IsSelected);
        }

        [Fact]
        public void Range_Select_Should_Select_Range()
        {
            var target = new TestSelector
            {
                Items = new[]
                {
                    "foo",
                    "bar",
                    "baz",
                    "qux",
                    "qiz",
                    "lol",
                },
                SelectionMode = SelectionMode.Multiple,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 1;
            target.SelectRange(3);

            Assert.Equal(new[] { "bar", "baz", "qux" }, target.SelectedItems.Cast<object>().ToList());
        }

        [Fact]
        public void Range_Select_Backwards_Should_Select_Range()
        {
            var target = new TestSelector
            {
                Items = new[]
                {
                    "foo",
                    "bar",
                    "baz",
                    "qux",
                    "qiz",
                    "lol",
                },
                SelectionMode = SelectionMode.Multiple,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 3;
            target.SelectRange(1);

            Assert.Equal(new[] { "qux", "baz", "bar" }, target.SelectedItems.Cast<object>().ToList());
        }

        [Fact]
        public void Second_Range_Select_Backwards_Should_Select_From_Original_Selection()
        {
            var target = new TestSelector
            {
                Items = new[]
                {
                    "foo",
                    "bar",
                    "baz",
                    "qux",
                    "qiz",
                    "lol",
                },
                SelectionMode = SelectionMode.Multiple,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 2;
            target.SelectRange(5);
            target.SelectRange(4);

            Assert.Equal(new[] { "baz", "qux", "qiz" }, target.SelectedItems.Cast<object>().ToList());
        }

        [Fact]
        public void Suprious_SelectedIndex_Changes_Should_Not_Be_Triggered()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar", "baz" },
                Template = Template(),
            };

            target.ApplyTemplate();

            var selectedIndexes = new List<int>();
            target.GetObservable(TestSelector.SelectedIndexProperty).Subscribe(x => selectedIndexes.Add(x));

            target.SelectedItems = new AvaloniaList<object> { "bar", "baz" };
            target.SelectedItem = "foo";

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(new[] { -1, 1, 0 }, selectedIndexes);
        }

        /// <summary>
        /// Tests a problem discovered with ListBox with selection.
        /// </summary>
        /// <remarks>
        /// - Items is bound to DataContext first, followed by say SelectedIndex
        /// - When the ListBox is removed from the visual tree, DataContext becomes null (as it's
        ///   inherited)
        /// - This changes Items to null, which changes SelectedIndex to null as there are no
        ///   longer any items
        /// - However, the news that DataContext is now null hasn't yet reached the SelectedItems
        ///   binding and so the unselection is sent back to the ViewModel
        /// 
        /// This is a similar problem to that tested by XamlBindingTest.Should_Not_Write_To_Old_DataContext.
        /// However, that tests a general property binding problem: here we are writing directly
        /// to the SelectedItems collection - not via a binding - so it's something that the 
        /// binding system cannot solve. Instead we solve it by not clearing SelectedItems when
        /// DataContext is in the process of changing.
        /// </remarks>
        [Fact]
        public void Should_Not_Write_To_Old_DataContext()
        {
            var vm = new OldDataContextViewModel();
            var target = new TestSelector();

            var itemsBinding = new Binding
            {
                Path = "Items",
                Mode = BindingMode.OneWay,
            };

            var selectedItemsBinding = new Binding
            {
                Path = "SelectedItems",
                Mode = BindingMode.OneWay,
            };

            // Bind Items and SelectedItems to the VM.
            target.Bind(TestSelector.ItemsProperty, itemsBinding);
            target.Bind(TestSelector.SelectedItemsProperty, selectedItemsBinding);

            // Set DataContext and SelectedIndex
            target.DataContext = vm;
            target.SelectedIndex = 1;

            // Make sure SelectedItems are written back to VM.
            Assert.Equal(new[] { "bar" }, vm.SelectedItems);

            // Clear DataContext and ensure that SelectedItems is still set in the VM.
            target.DataContext = null;
            Assert.Equal(new[] { "bar" }, vm.SelectedItems);

            // Ensure target's SelectedItems is now clear.
            Assert.Empty(target.SelectedItems);
        }

        [Fact]
        public void Unbound_SelectedItems_Should_Be_Cleared_When_DataContext_Cleared()
        {
            var data = new
            {
                Items = new[] { "foo", "bar", "baz" },
            };

            var target = new TestSelector
            {
                DataContext = data,
                Template = Template(),
            };

            var itemsBinding = new Binding { Path = "Items" };
            target.Bind(TestSelector.ItemsProperty, itemsBinding);

            Assert.Same(data.Items, target.Items);

            target.SelectedItems.Add("bar");
            target.DataContext = null;

            Assert.Empty(target.SelectedItems);
        }

        [Fact]
        public void Adding_To_SelectedItems_Should_Raise_SelectionChanged()
        {
            var items = new[] { "foo", "bar", "baz" };

            var target = new TestSelector
            {
                DataContext = items,
                Template = Template(),
            };

            var called = false;

            target.SelectionChanged += (s, e) =>
            {
                Assert.Equal(new[] { "bar" }, e.AddedItems.Cast<object>().ToList());
                Assert.Empty(e.RemovedItems);
                called = true;
            };

            target.SelectedItems.Add("bar");

            Assert.True(called);
        }

        [Fact]
        public void Removing_From_SelectedItems_Should_Raise_SelectionChanged()
        {
            var items = new[] { "foo", "bar", "baz" };

            var target = new TestSelector
            {
                Items = items,
                Template = Template(),
                SelectedItem = "bar",
            };

            var called = false;

            target.SelectionChanged += (s, e) =>
            {
                Assert.Equal(new[] { "bar" }, e.RemovedItems.Cast<object>().ToList());
                Assert.Empty(e.AddedItems);
                called = true;
            };

            target.SelectedItems.Remove("bar");

            Assert.True(called);
        }

        [Fact]
        public void Assigning_SelectedItems_Should_Raise_SelectionChanged()
        {
            var items = new[] { "foo", "bar", "baz" };

            var target = new TestSelector
            {
                Items = items,
                Template = Template(),
                SelectedItem = "bar",
            };

            var called = false;

            target.SelectionChanged += (s, e) =>
            {
                Assert.Equal(new[] { "foo", "baz" }, e.AddedItems.Cast<object>());
                Assert.Equal(new[] { "bar" }, e.RemovedItems.Cast<object>());
                called = true;
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            target.SelectedItems = new AvaloniaList<object>("foo", "baz");

            Assert.True(called);
        }

        private FuncControlTemplate Template()
        {
            return new FuncControlTemplate<SelectingItemsControl>(control =>
                new ItemsPresenter
                {
                    Name = "PART_ItemsPresenter",
                    [~ItemsPresenter.ItemsProperty] = control[~ItemsControl.ItemsProperty],
                    [~ItemsPresenter.ItemsPanelProperty] = control[~ItemsControl.ItemsPanelProperty],
                });
        }

        private class TestSelector : SelectingItemsControl
        {
            public static readonly new AvaloniaProperty<IList> SelectedItemsProperty = 
                SelectingItemsControl.SelectedItemsProperty;

            public new IList SelectedItems
            {
                get { return base.SelectedItems; }
                set { base.SelectedItems = value; }
            }

            public new SelectionMode SelectionMode
            {
                get { return base.SelectionMode; }
                set { base.SelectionMode = value; }
            }

            public void SelectRange(int index)
            {
                UpdateSelection(index, true, true);
            }
        }

        private class OldDataContextViewModel
        {
            public OldDataContextViewModel()
            {
                Items = new List<string> { "foo", "bar" };
                SelectedItems = new List<string>();
            }

            public List<string> Items { get; } 
            public List<string> SelectedItems { get; }
        }
    }
}
