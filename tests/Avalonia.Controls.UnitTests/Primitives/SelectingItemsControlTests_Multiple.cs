// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class SelectingItemsControlTests_Multiple
    {
        private MouseTestHelper _helper = new MouseTestHelper();

        [Fact]
        public void Setting_SelectedIndex_Should_Add_To_Selected_Items()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar" },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 1;

            Assert.Equal(new[] { "bar" }, target.Selection.SelectedItems.Cast<object>().ToList());
        }

        [Fact]
        public void Adding_To_Selection_Should_Set_SelectedIndex()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar" },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.Selection.Select(1);

            Assert.Equal(1, target.SelectedIndex);
        }

        [Fact]
        public void Assigning_Null_To_Selection_Should_Create_New_SelectionModel()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar" },
                Template = Template(),
            };

            var oldSelection = target.Selection;

            target.Selection = null;

            Assert.NotNull(target.Selection);
            Assert.NotSame(oldSelection, target.Selection);
        }

        [Fact]
        public void Assigning_SelectionModel_With_Different_Source_To_Selection_Should_Fail()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar" },
                Template = Template(),
            };

            var selection = new SelectionModel { Source = new[] { "baz" } };
            Assert.Throws<ArgumentException>(() => target.Selection = selection);
        }

        [Fact]
        public void Assigning_SelectionModel_With_Null_Source_To_Selection_Should_Set_Source()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar" },
                Template = Template(),
            };

            var selection = new SelectionModel();
            target.Selection = selection;

            Assert.Same(target.Items, selection.Source);
        }

        [Fact]
        public void Assigning_Single_Selected_Item_To_Selection_Should_Set_SelectedIndex()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar" },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            var selection = new SelectionModel { Source = target.Items };
            selection.Select(1);
            target.Selection = selection;

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal(new[] { "bar" }, target.Selection.SelectedItems);
            Assert.Equal(new[] { 1 }, SelectedContainers(target));
        }

        [Fact]
        public void Assigning_Multiple_Selected_Items_To_Selection_Should_Set_SelectedIndex()
        {
            // Note that we don't need SelectionMode = Multiple here. Multiple selections can always
            // be made in code.
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar", "baz" },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            var selection = new SelectionModel { Source = target.Items };
            selection.SelectRange(new IndexPath(0), new IndexPath(2));
            target.Selection = selection;

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(new[] { "foo", "bar", "baz" }, target.Selection.SelectedItems);
            Assert.Equal(new[] { 0, 1, 2 }, SelectedContainers(target));
        }

        [Fact]
        public void Selected_Items_Should_Be_Marked_When_Panel_Created_After_Selected_Items_Are_Set()
        {
            // Issue #2565.
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar", "baz" },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectAll();
            target.Presenter.ApplyTemplate();

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(new[] { "foo", "bar", "baz" }, target.Selection.SelectedItems);
            Assert.Equal(new[] { 0, 1, 2 }, SelectedContainers(target));
        }

        [Fact]
        public void Reassigning_Selection_Should_Clear_Selection()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar" },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.Selection.Select(1);
            target.Selection = new SelectionModel();

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Null(target.SelectedItem);
        }

        [Fact]
        public void Adding_First_Selected_Item_Should_Raise_SelectedIndex_SelectedItem_Changed()
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
            target.Selection.Select(1);

            Assert.True(indexRaised);
            Assert.True(itemRaised);
        }

        [Fact]
        public void Adding_Subsequent_Selected_Items_Should_Not_Raise_SelectedIndex_SelectedItem_Changed()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar" },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.Selection.Select(0);

            bool raised = false;
            target.PropertyChanged += (s, e) =>
                raised |= e.Property.Name == "SelectedIndex" ||
                          e.Property.Name == "SelectedItem";

            target.Selection.Select(1);

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
            target.Selection.Select(0);

            bool raised = false;
            target.PropertyChanged += (s, e) =>
                raised |= e.Property.Name == "SelectedIndex" &&
                          (int)e.OldValue == 0 &&
                          (int)e.NewValue == -1;

            target.Selection.Deselect(0);

            Assert.True(raised);
        }

        [Fact]
        public void Adding_Selected_Items_Should_Set_Item_IsSelected()
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
            target.Selection.Select(0);
            target.Selection.Select(1);

            var foo = target.Presenter.Panel.Children[0];

            Assert.True(items[0].IsSelected);
            Assert.True(items[1].IsSelected);
            Assert.False(items[2].IsSelected);
        }

        [Fact]
        public void Assigning_Selection_Should_Set_Item_IsSelected()
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

            var selection = new SelectionModel { Source = items };
            selection.SelectRange(new IndexPath(0), new IndexPath(1));
            target.Selection = selection;

            Assert.True(items[0].IsSelected);
            Assert.True(items[1].IsSelected);
            Assert.False(items[2].IsSelected);
        }

        [Fact]
        public void Removing_Selected_Items_Should_Clear_Item_IsSelected()
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
            target.Selection.Select(0);
            target.Selection.Select(1);
            target.Selection.Deselect(1);

            Assert.True(items[0].IsSelected);
            Assert.False(items[1].IsSelected);
        }

        [Fact]
        public void Setting_SelectedIndex_Should_Unmark_Previously_Selected_Containers()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar", "baz" },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            target.Selection.Select(0);
            target.Selection.Select(1);

            Assert.Equal(new[] { 0, 1 }, SelectedContainers(target));

            target.SelectedIndex = 2;

            Assert.Equal(new[] { 2 }, SelectedContainers(target));
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

            Assert.Equal(new[] { "bar", "baz", "qux" }, target.Selection.SelectedItems.Cast<object>().ToList());
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

            Assert.Equal(new[] { "bar", "baz", "qux" }, target.Selection.SelectedItems.Cast<object>().ToList());
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

            Assert.Equal(new[] { "baz", "qux", "qiz" }, target.Selection.SelectedItems.Cast<object>().ToList());
        }

        [Fact]
        public void Setting_SelectedIndex_After_Range_Should_Unmark_Previously_Selected_Containers()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar", "baz", "qux" },
                Template = Template(),
                SelectedIndex = 0,
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            target.SelectRange(2);

            Assert.Equal(new[] { 0, 1, 2 }, SelectedContainers(target));

            target.SelectedIndex = 3;

            Assert.Equal(new[] { 3 }, SelectedContainers(target));
        }

        [Fact]
        public void Toggling_Selection_After_Range_Should_Work()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar", "baz", "foo", "bar", "baz" },
                Template = Template(),
                SelectedIndex = 0,
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            target.SelectRange(3);

            Assert.Equal(new[] { 0, 1, 2, 3 }, SelectedContainers(target));

            target.Toggle(4);

            Assert.Equal(new[] { 0, 1, 2, 3, 4 }, SelectedContainers(target));
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

            var selection = new SelectionModel { Source = target.Items };
            selection.SelectRange(new IndexPath(1), new IndexPath(2));
            target.Selection = selection;
            target.SelectedItem = "foo";

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(new[] { -1, 1, 0 }, selectedIndexes);
        }

        [Fact]
        public void Can_Set_SelectedIndex_To_Another_Selected_Item()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar", "baz" },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            target.Selection.Select(0);
            target.Selection.Select(1);

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(new[] { "foo", "bar" }, target.Selection.SelectedItems);
            Assert.Equal(new[] { 0, 1 }, SelectedContainers(target));

            var raised = false;
            target.SelectionChanged += (s, e) =>
            {
                raised = true;
                Assert.Empty(e.AddedItems);
                Assert.Equal(new[] { "foo" }, e.RemovedItems);
            };

            target.SelectedIndex = 1;

            Assert.True(raised);
            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal(new[] { "bar" }, target.Selection.SelectedItems);
            Assert.Equal(new[] { 1 }, SelectedContainers(target));
        }

        /// <summary>
        /// Tests a problem discovered with ListBox with selection.
        /// </summary>
        /// <remarks>
        /// - Items is bound to DataContext first, followed by say SelectedIndex
        /// - When the ListBox is removed from the tree, DataContext becomes null (as it's inherited)
        /// - This changes Items and Selection to null
        /// - However, the news that DataContext is now null reaches the Selection after the change to
        ///   Items, so the SelectionModel.Source is set to null
        /// 
        /// This is a similar problem to that tested by XamlBindingTest.Should_Not_Write_To_Old_DataContext.
        /// However, that tests a general property binding problem: here we are writing directly
        /// to the SelectionModel - not via a binding - so it's something that the binding system cannot
        /// solve. Instead we solve it by not clearing SelectedItems when DataContext is in the process of
        /// changing.
        /// </remarks>
        [Fact]
        public void Should_Not_Reset_Selection_Source_When_DataContext_Changes()
        {
            var vm = new OldDataContextViewModel();
            var target = new TestSelector();

            var itemsBinding = new Binding
            {
                Path = "Items",
                Mode = BindingMode.OneWay,
            };

            var selectionBinding = new Binding
            {
                Path = "Selection",
                Mode = BindingMode.OneWay,
            };

            // Bind Items and SelectedItems to the VM.
            target.Bind(TestSelector.ItemsProperty, itemsBinding);
            target.Bind(TestSelector.SelectionProperty, selectionBinding);

            // Set DataContext and SelectedIndex
            target.DataContext = vm;
            target.SelectedIndex = 1;

            // Make sure selection is written back to VM.
            Assert.Same(vm.Selection, target.Selection);
            Assert.Equal(new[] { "bar" }, vm.Selection.SelectedItems);

            vm.Selection.SelectionChanged += (s, e) => { };

            // Clear DataContext and ensure that selection is still set in the VM.
            target.DataContext = null;
            Assert.Same(vm.Items, vm.Selection.Source);
            Assert.NotSame(vm.Selection, target.Selection);
            Assert.Equal(new[] { "bar" }, vm.Selection.SelectedItems);

            // Ensure target's selection is now clear.
            Assert.Empty(target.Selection.SelectedItems);
        }

        [Fact]
        public void Unbound_Selected_Items_Should_Be_Cleared_When_DataContext_Cleared()
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

            target.Selection.Select(1);
            target.DataContext = null;

            Assert.Empty(target.Selection.SelectedItems);
        }

        [Fact]
        public void Adding_Selected_Items_Should_Raise_SelectionChanged()
        {
            var items = new[] { "foo", "bar", "baz" };

            var target = new TestSelector
            {
                DataContext = items,
                Template = Template(),
                Items = items,
            };

            var called = false;

            target.SelectionChanged += (s, e) =>
            {
                Assert.Equal(new[] { "bar" }, e.AddedItems.Cast<object>().ToList());
                Assert.Empty(e.RemovedItems);
                called = true;
            };

            target.Selection.Select(1);

            Assert.True(called);
        }

        [Fact]
        public void Removing_Selected_Items_Should_Raise_SelectionChanged()
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

            target.Selection.Deselect(1);

            Assert.True(called);
        }

        [Fact]
        public void Assigning_Selection_Should_Raise_SelectionChanged()
        {
            var items = new[] { "foo", "bar", "baz" };

            var target = new TestSelector
            {
                Items = items,
                Template = Template(),
                SelectedItem = "bar",
            };

            var raised = 0;

            target.SelectionChanged += (s, e) =>
            {
                if (raised == 0)
                {
                    Assert.Empty(e.AddedItems.Cast<object>());
                    Assert.Equal(new[] { "bar" }, e.RemovedItems.Cast<object>());
                }
                else
                {
                    Assert.Equal(new[] { "foo", "baz" }, e.AddedItems.Cast<object>());
                    Assert.Empty(e.RemovedItems.Cast<object>());
                }

                ++raised;
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            var selection = new SelectionModel { Source = items };
            selection.Select(0);
            selection.Select(2);
            target.Selection = selection;

            Assert.Equal(2, raised);
        }

        [Fact]
        public void Shift_Selecting_From_No_Selection_Selects_From_Start()
        {
            var target = new ListBox
            {
                Template = Template(),
                Items = new[] { "Foo", "Bar", "Baz" },
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            _helper.Click((Interactive)target.Presenter.Panel.Children[2], modifiers: InputModifiers.Shift);

            var panel = target.Presenter.Panel;

            Assert.Equal(new[] { "Foo", "Bar", "Baz" }, target.Selection.SelectedItems);
            Assert.Equal(new[] { 0, 1, 2 }, SelectedContainers(target));
        }

        [Fact]
        public void Ctrl_Selecting_Raises_SelectionChanged_Events()
        {
            var target = new ListBox
            {
                Template = Template(),
                Items = new[] { "Foo", "Bar", "Baz", "Qux" },
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            SelectionChangedEventArgs receivedArgs = null;

            target.SelectionChanged += (_, args) => receivedArgs = args;

            void VerifyAdded(string selection)
            {
                Assert.NotNull(receivedArgs);
                Assert.Equal(new[] { selection }, receivedArgs.AddedItems);
                Assert.Empty(receivedArgs.RemovedItems);
            }

            void VerifyRemoved(string selection)
            {
                Assert.NotNull(receivedArgs);
                Assert.Equal(new[] { selection }, receivedArgs.RemovedItems);
                Assert.Empty(receivedArgs.AddedItems);
            }

            _helper.Click((Interactive)target.Presenter.Panel.Children[1]);

            VerifyAdded("Bar");

            receivedArgs = null;
            _helper.Click((Interactive)target.Presenter.Panel.Children[2], modifiers: InputModifiers.Control);

            VerifyAdded("Baz");

            receivedArgs = null;
            _helper.Click((Interactive)target.Presenter.Panel.Children[3], modifiers: InputModifiers.Control);

            VerifyAdded("Qux");

            receivedArgs = null;
            _helper.Click((Interactive)target.Presenter.Panel.Children[1], modifiers: InputModifiers.Control);

            VerifyRemoved("Bar");
        }

        [Fact]
        public void Ctrl_Selecting_SelectedItem_With_Multiple_Selection_Active_Sets_SelectedItem_To_Next_Selection()
        {
            var target = new ListBox
            {
                Template = Template(),
                Items = new[] { "Foo", "Bar", "Baz", "Qux" },
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            _helper.Click((Interactive)target.Presenter.Panel.Children[1]);
            _helper.Click((Interactive)target.Presenter.Panel.Children[2], modifiers: InputModifiers.Control);
            _helper.Click((Interactive)target.Presenter.Panel.Children[3], modifiers: InputModifiers.Control);

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal("Bar", target.SelectedItem);
            Assert.Equal(new[] { "Bar", "Baz", "Qux" }, target.Selection.SelectedItems);

            _helper.Click((Interactive)target.Presenter.Panel.Children[1], modifiers: InputModifiers.Control);

            Assert.Equal(2, target.SelectedIndex);
            Assert.Equal("Baz", target.SelectedItem);
            Assert.Equal(new[] { "Baz", "Qux" }, target.Selection.SelectedItems);
        }

        [Fact]
        public void Ctrl_Selecting_Non_SelectedItem_With_Multiple_Selection_Active_Leaves_SelectedItem_The_Same()
        {
            var target = new ListBox
            {
                Template = Template(),
                Items = new[] { "Foo", "Bar", "Baz" },
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            _helper.Click((Interactive)target.Presenter.Panel.Children[1]);
            _helper.Click((Interactive)target.Presenter.Panel.Children[2], modifiers: InputModifiers.Control);

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal("Bar", target.SelectedItem);

            _helper.Click((Interactive)target.Presenter.Panel.Children[2], modifiers: InputModifiers.Control);

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal("Bar", target.SelectedItem);
        }

        [Fact]
        public void Should_Ctrl_Select_Correct_Item_When_Duplicate_Items_Are_Present()
        {
            var target = new ListBox
            {
                Template = Template(),
                Items = new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" },
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            _helper.Click((Interactive)target.Presenter.Panel.Children[3]);
            _helper.Click((Interactive)target.Presenter.Panel.Children[4], modifiers: InputModifiers.Control);

            var panel = target.Presenter.Panel;

            Assert.Equal(new[] { "Foo", "Bar" }, target.Selection.SelectedItems);
            Assert.Equal(new[] { 3, 4 }, SelectedContainers(target));
        }

        [Fact]
        public void Should_Shift_Select_Correct_Item_When_Duplicates_Are_Present()
        {
            var target = new ListBox
            {
                Template = Template(),
                Items = new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" },
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            _helper.Click((Interactive)target.Presenter.Panel.Children[3]);
            _helper.Click((Interactive)target.Presenter.Panel.Children[5], modifiers: InputModifiers.Shift);

            var panel = target.Presenter.Panel;

            Assert.Equal(new[] { "Foo", "Bar", "Baz" }, target.Selection.SelectedItems);
            Assert.Equal(new[] { 3, 4, 5 }, SelectedContainers(target));
        }

        [Fact]
        public void Can_Shift_Select_All_Items_When_Duplicates_Are_Present()
        {
            var target = new ListBox
            {
                Template = Template(),
                Items = new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" },
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            _helper.Click((Interactive)target.Presenter.Panel.Children[0]);
            _helper.Click((Interactive)target.Presenter.Panel.Children[5], modifiers: InputModifiers.Shift);

            var panel = target.Presenter.Panel;

            Assert.Equal(new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" }, target.Selection.SelectedItems);
            Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, SelectedContainers(target));
        }

        [Fact]
        public void Shift_Selecting_Raises_SelectionChanged_Events()
        {
            var target = new ListBox
            {
                Template = Template(),
                Items = new[] { "Foo", "Bar", "Baz", "Qux" },
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            SelectionChangedEventArgs receivedArgs = null;

            target.SelectionChanged += (_, args) => receivedArgs = args;

            void VerifyAdded(params string[] selection)
            {
                Assert.NotNull(receivedArgs);
                Assert.Equal(selection, receivedArgs.AddedItems);
                Assert.Empty(receivedArgs.RemovedItems);
            }

            void VerifyRemoved(string selection)
            {
                Assert.NotNull(receivedArgs);
                Assert.Equal(new[] { selection }, receivedArgs.RemovedItems);
                Assert.Empty(receivedArgs.AddedItems);
            }

            _helper.Click((Interactive)target.Presenter.Panel.Children[1]);

            VerifyAdded("Bar");

            receivedArgs = null;
            _helper.Click((Interactive)target.Presenter.Panel.Children[3], modifiers: InputModifiers.Shift);

            VerifyAdded("Baz" ,"Qux");

            receivedArgs = null;
            _helper.Click((Interactive)target.Presenter.Panel.Children[2], modifiers: InputModifiers.Shift);

            
            VerifyRemoved("Qux");
        }

        [Fact]
        public void Duplicate_Items_Are_Added_To_SelectedItems_In_Order()
        {
            var target = new ListBox
            {
                Template = Template(),
                Items = new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" },
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            _helper.Click((Interactive)target.Presenter.Panel.Children[0]);

            Assert.Equal(new[] { "Foo" }, target.Selection.SelectedItems);

            _helper.Click((Interactive)target.Presenter.Panel.Children[4], modifiers: InputModifiers.Control);

            Assert.Equal(new[] { "Foo", "Bar" }, target.Selection.SelectedItems);

            _helper.Click((Interactive)target.Presenter.Panel.Children[3], modifiers: InputModifiers.Control);

            Assert.Equal(new[] { "Foo", "Foo", "Bar" }, target.Selection.SelectedItems);

            _helper.Click((Interactive)target.Presenter.Panel.Children[1], modifiers: InputModifiers.Control);

            Assert.Equal(new[] { "Foo", "Bar", "Foo", "Bar" }, target.Selection.SelectedItems);
        }

        [Fact]
        public void SelectAll_Sets_SelectedIndex_And_SelectedItem()
        {
            var target = new TestSelector
            {
                Template = Template(),
                Items = new[] { "Foo", "Bar", "Baz" },
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            target.SelectAll();

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("Foo", target.SelectedItem);
        }

        [Fact]
        public void SelectAll_Raises_SelectionChanged_Event()
        {
            var target = new TestSelector
            {
                Template = Template(),
                Items = new[] { "Foo", "Bar", "Baz" },
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            SelectionChangedEventArgs receivedArgs = null;

            target.SelectionChanged += (_, args) => receivedArgs = args;

            target.SelectAll();

            Assert.NotNull(receivedArgs);
            Assert.Equal(target.Items, receivedArgs.AddedItems);
            Assert.Empty(receivedArgs.RemovedItems);
        }

        [Fact]
        public void UnselectAll_Clears_SelectedIndex_And_SelectedItem()
        {
            var target = new TestSelector
            {
                Template = Template(),
                Items = new[] { "Foo", "Bar", "Baz" },
                SelectionMode = SelectionMode.Multiple,
                SelectedIndex = 0,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            target.UnselectAll();

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Equal(null, target.SelectedItem);
        }

        [Fact]
        public void SelectAll_Handles_Duplicate_Items()
        {
            var target = new TestSelector
            {
                Template = Template(),
                Items = new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" },
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            target.SelectAll();

            Assert.Equal(new[] { "Foo", "Bar", "Baz", "Foo", "Bar", "Baz" }, target.Selection.SelectedItems);
        }

        [Fact]
        public void Adding_Item_Before_SelectedItems_Should_Update_Selection()
        {
            var items = new ObservableCollection<string>
            {
               "Foo",
               "Bar",
               "Baz"
            };

            var target = new ListBox
            {
                Template = Template(),
                Items = items,
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            target.SelectAll();
            items.Insert(0, "Qux");

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal("Foo", target.SelectedItem);
            Assert.Equal(new[] { "Foo", "Bar", "Baz" }, target.Selection.SelectedItems);
            Assert.Equal(new[] { 1, 2, 3 }, SelectedContainers(target));
        }

        [Fact]
        public void Removing_Item_Before_SelectedItem_Should_Update_Selection()
        {
            var items = new ObservableCollection<string>
            {
               "Foo",
               "Bar",
               "Baz"
            };

            var target = new TestSelector
            {
                Template = Template(),
                Items = items,
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            target.SelectedIndex = 1;
            target.SelectRange(2);

            Assert.Equal(new[] { "Bar", "Baz" }, target.Selection.SelectedItems);

            items.RemoveAt(0);

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("Bar", target.SelectedItem);
            Assert.Equal(new[] { "Bar", "Baz" }, target.Selection.SelectedItems);
            Assert.Equal(new[] { 0, 1 }, SelectedContainers(target));
        }

        [Fact]
        public void Removing_SelectedItem_With_Multiple_Selection_Active_Should_Update_Selection()
        {
            var items = new ObservableCollection<string>
            {
               "Foo",
               "Bar",
               "Baz"
            };

            var target = new ListBox
            {
                Template = Template(),
                Items = items,
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            target.SelectAll();
            items.RemoveAt(0);

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("Bar", target.SelectedItem);
            Assert.Equal(new[] { "Bar", "Baz" }, target.Selection.SelectedItems);
            Assert.Equal(new[] { 0, 1 }, SelectedContainers(target));
        }

        [Fact]
        public void Replacing_Selected_Item_Should_Update_SelectedItems()
        {
            var items = new ObservableCollection<string>
            {
               "Foo",
               "Bar",
               "Baz"
            };

            var target = new ListBox
            {
                Template = Template(),
                Items = items,
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            target.SelectAll();
            items[1] = "Qux";

            Assert.Equal(new[] { "Foo", "Baz" }, target.Selection.SelectedItems);
        }

        [Fact]
        public void Left_Click_On_SelectedItem_Should_Clear_Existing_Selection()
        {
            var target = new ListBox
            {
                Template = Template(),
                Items = new[] { "Foo", "Bar", "Baz" },
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            target.SelectAll();

            Assert.Equal(3, target.Selection.SelectedItems.Count);

            _helper.Click((Interactive)target.Presenter.Panel.Children[0]);

            Assert.Equal(1, target.Selection.SelectedItems.Count);
            Assert.Equal(new[] { "Foo", }, target.Selection.SelectedItems);
            Assert.Equal(new[] { 0 }, SelectedContainers(target));
        }

        [Fact]
        public void Right_Click_On_SelectedItem_Should_Not_Clear_Existing_Selection()
        {
            var target = new ListBox
            {
                Template = Template(),
                Items = new[] { "Foo", "Bar", "Baz" },
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            target.SelectAll();

            Assert.Equal(3, target.Selection.SelectedItems.Count);

            _helper.Click((Interactive)target.Presenter.Panel.Children[0], MouseButton.Right);

            Assert.Equal(3, target.Selection.SelectedItems.Count);
        }

        [Fact]
        public void Right_Click_On_UnselectedItem_Should_Clear_Existing_Selection()
        {
            var target = new ListBox
            {
                Template = Template(),
                Items = new[] { "Foo", "Bar", "Baz" },
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            _helper.Click((Interactive)target.Presenter.Panel.Children[0]);
            _helper.Click((Interactive)target.Presenter.Panel.Children[1], modifiers: InputModifiers.Shift);

            Assert.Equal(2, target.Selection.SelectedItems.Count);

            _helper.Click((Interactive)target.Presenter.Panel.Children[2], MouseButton.Right);

            Assert.Equal(1, target.Selection.SelectedItems.Count);
        }

        [Fact]
        public void Adding_Selected_ItemContainers_Should_Update_Selection()
        {
            var items = new AvaloniaList<ItemContainer>(new[]
            {
                new ItemContainer(),
                new ItemContainer(),
            });

            var target = new TestSelector
            {
                Items = items,
                SelectionMode = SelectionMode.Multiple,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            items.Add(new ItemContainer { IsSelected = true });
            items.Add(new ItemContainer { IsSelected = true });

            Assert.Equal(2, target.SelectedIndex);
            Assert.Equal(items[2], target.SelectedItem);
            Assert.Equal(new[] { items[2], items[3] }, target.Selection.SelectedItems);
        }

        [Fact]
        public void Shift_Right_Click_Should_Not_Select_Multiple()
        {
            var target = new ListBox
            {
                Template = Template(),
                Items = new[] { "Foo", "Bar", "Baz" },
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            _helper.Click((Interactive)target.Presenter.Panel.Children[0]);
            _helper.Click((Interactive)target.Presenter.Panel.Children[2], MouseButton.Right, modifiers: InputModifiers.Shift);

            Assert.Equal(1, target.Selection.SelectedItems.Count);
        }

        [Fact]
        public void Ctrl_Right_Click_Should_Not_Select_Multiple()
        {
            var target = new ListBox
            {
                Template = Template(),
                Items = new[] { "Foo", "Bar", "Baz" },
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                SelectionMode = SelectionMode.Multiple,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            _helper.Click((Interactive)target.Presenter.Panel.Children[0]);
            _helper.Click((Interactive)target.Presenter.Panel.Children[2], MouseButton.Right, modifiers: InputModifiers.Control);

            Assert.Equal(1, target.Selection.SelectedItems.Count);
        }

        [Fact]
        public void Ctrl_Left_Click_With_Bound_SelectedIndex_Should_Not_Clear_Selection()
        {
            var target = new ListBox
            {
                DataContext = new SelectionViewModel(),
                Template = Template(),
                Items = new[] { "Foo", "Bar", "Baz" },
                ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Width = 20, Height = 10 }),
                SelectionMode = SelectionMode.Multiple,
                [!ListBox.SelectedIndexProperty] = new Binding(nameof(SelectionViewModel.SelectedIndex)),
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            target.Selection.Select(1);

            _helper.Click((Interactive)target.Presenter.Panel.Children[0], modifiers: InputModifiers.Control);

            Assert.Equal(new[] { "Foo", "Bar" }, target.Selection.SelectedItems);
        }

        private IEnumerable<int> SelectedContainers(SelectingItemsControl target)
        {
            return target.Presenter.Panel.Children
                .Select((x, i) => x.Classes.Contains(":selected") ? i : -1)
                .Where(x => x != -1);
        }

        private FuncControlTemplate Template()
        {
            return new FuncControlTemplate<SelectingItemsControl>((control, scope) =>
                new ItemsPresenter
                {
                    Name = "PART_ItemsPresenter",
                    [~ItemsPresenter.ItemsProperty] = control[~ItemsControl.ItemsProperty],
                    [~ItemsPresenter.ItemsPanelProperty] = control[~ItemsControl.ItemsPanelProperty],
                }.RegisterInNameScope(scope));
        }

        private class TestSelector : SelectingItemsControl
        {
            public TestSelector()
            {
                SelectionMode = SelectionMode.Multiple;
            }

            public new ISelectionModel Selection
            {
                get => base.Selection;
                set => base.Selection = value;
            }

            public new SelectionMode SelectionMode
            {
                get { return base.SelectionMode; }
                set { base.SelectionMode = value; }
            }

            public void SelectAll() => Selection.SelectAll();
            public void UnselectAll() => Selection.ClearSelection();
            public void SelectRange(int index) => UpdateSelection(index, true, true);
            public void Toggle(int index) => UpdateSelection(index, true, false, true);
        }

        private class SelectionViewModel : NotifyingBase
        {
            private int _selectedIndex = -1;

            public int SelectedIndex
            {
                get => _selectedIndex;
                set
                {
                    if (_selectedIndex != value)
                    {
                        _selectedIndex = value;
                        RaisePropertyChanged(nameof(SelectedIndex));
                    }
                }
            }
        }

        private class OldDataContextViewModel
        {
            public OldDataContextViewModel()
            {
                Items = new List<string> { "foo", "bar" };
                Selection = new SelectionModel { Source = Items };
            }

            public List<string> Items { get; } 
            public SelectionModel Selection { get; }
        }

        private class ItemContainer : Control, ISelectable
        {
            public string Value { get; set; }
            public bool IsSelected { get; set; }
        }
    }
}
