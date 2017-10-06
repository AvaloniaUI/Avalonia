// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Data;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class SelectingItemsControlTests
    {
        [Fact]
        public void SelectedIndex_Should_Initially_Be_Minus_1()
        {
            var items = new[]
            {
                new Item(),
                new Item(),
            };

            var target = new SelectingItemsControl
            {
                Items = items,
                Template = Template(),
            };

            Assert.Equal(-1, target.SelectedIndex);
        }

        [Fact]
        public void Item_IsSelected_Should_Initially_Be_False()
        {
            var items = new[]
            {
                new Item(),
                new Item(),
            };

            var target = new SelectingItemsControl
            {
                Items = items,
                Template = Template(),
            };

            target.ApplyTemplate();

            Assert.False(items[0].IsSelected);
            Assert.False(items[1].IsSelected);
        }

        [Fact]
        public void Setting_SelectedItem_Should_Set_Item_IsSelected_True()
        {
            var items = new[]
            {
                new Item(),
                new Item(),
            };

            var target = new SelectingItemsControl
            {
                Items = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            target.SelectedItem = items[1];

            Assert.False(items[0].IsSelected);
            Assert.True(items[1].IsSelected);
        }

        [Fact]
        public void Setting_SelectedItem_Before_ApplyTemplate_Should_Set_Item_IsSelected_True()
        {
            var items = new[]
            {
                new Item(),
                new Item(),
            };

            var target = new SelectingItemsControl
            {
                Items = items,
                Template = Template(),
            };

            target.SelectedItem = items[1];
            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            Assert.False(items[0].IsSelected);
            Assert.True(items[1].IsSelected);
        }

        [Fact]
        public void Setting_SelectedIndex_Before_ApplyTemplate_Should_Set_Item_IsSelected_True()
        {
            var items = new[]
            {
                new Item(),
                new Item(),
            };

            var target = new SelectingItemsControl
            {
                Items = items,
                Template = Template(),
            };

            target.SelectedIndex = 1;
            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            Assert.False(items[0].IsSelected);
            Assert.True(items[1].IsSelected);
        }

        [Fact]
        public void Setting_SelectedItem_Should_Set_SelectedIndex()
        {
            var items = new[]
            {
                new Item(),
                new Item(),
            };

            var target = new SelectingItemsControl
            {
                Items = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedItem = items[1];

            Assert.Equal(items[1], target.SelectedItem);
            Assert.Equal(1, target.SelectedIndex);
        }

        [Fact]
        public void Setting_SelectedItem_To_Not_Present_Item_Should_Clear_Selection()
        {
            var items = new[]
            {
                new Item(),
                new Item(),
            };

            var target = new SelectingItemsControl
            {
                Items = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedItem = items[1];

            Assert.Equal(items[1], target.SelectedItem);
            Assert.Equal(1, target.SelectedIndex);

            target.SelectedItem = new Item();

            Assert.Null(target.SelectedItem);
            Assert.Equal(-1, target.SelectedIndex);
        }

        [Fact]
        public void Setting_SelectedIndex_Should_Set_SelectedItem()
        {
            var items = new[]
            {
                new Item(),
                new Item(),
            };

            var target = new SelectingItemsControl
            {
                Items = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 1;

            Assert.Equal(items[1], target.SelectedItem);
        }

        [Fact]
        public void Setting_SelectedIndex_Out_Of_Bounds_Should_Clear_Selection()
        {
            var items = new[]
            {
                new Item(),
                new Item(),
            };

            var target = new SelectingItemsControl
            {
                Items = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 2;

            Assert.Equal(-1, target.SelectedIndex);
        }

        [Fact]
        public void Setting_SelectedItem_To_Non_Existent_Item_Should_Clear_Selection()
        {
            var target = new SelectingItemsControl
            {
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedItem = new Item();

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Null(target.SelectedItem);
        }

        [Fact]
        public void Adding_Selected_Item_Should_Update_Selection()
        {
            var items = new AvaloniaList<Item>(new[]
            {
                new Item(),
                new Item(),
            });

            var target = new SelectingItemsControl
            {
                Items = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            items.Add(new Item { IsSelected = true });

            Assert.Equal(2, target.SelectedIndex);
            Assert.Equal(items[2], target.SelectedItem);
        }

        [Fact]
        public void Setting_Items_To_Null_Should_Clear_Selection()
        {
            var items = new AvaloniaList<Item>
            {
                new Item(),
                new Item(),
            };

            var target = new SelectingItemsControl
            {
                Items = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 1;

            Assert.Equal(items[1], target.SelectedItem);
            Assert.Equal(1, target.SelectedIndex);

            target.Items = null;

            Assert.Null(target.SelectedItem);
            Assert.Equal(-1, target.SelectedIndex);
        }

        [Fact]
        public void Removing_Selected_Item_Should_Clear_Selection()
        {
            var items = new AvaloniaList<Item>
            {
                new Item(),
                new Item(),
            };

            var target = new SelectingItemsControl
            {
                Items = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 1;

            Assert.Equal(items[1], target.SelectedItem);
            Assert.Equal(1, target.SelectedIndex);

            items.RemoveAt(1);

            Assert.Null(target.SelectedItem);
            Assert.Equal(-1, target.SelectedIndex);
        }

        [Fact]
        public void Resetting_Items_Collection_Should_Clear_Selection()
        {
            // Need to use ObservableCollection here as AvaloniaList signals a Clear as an
            // add + remove.
            var items = new ObservableCollection<Item>
            {
                new Item(),
                new Item(),
            };

            var target = new SelectingItemsControl
            {
                Items = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 1;

            Assert.Equal(items[1], target.SelectedItem);
            Assert.Equal(1, target.SelectedIndex);

            items.Clear();

            Assert.Null(target.SelectedItem);
            Assert.Equal(-1, target.SelectedIndex);
        }

        [Fact]
        public void Raising_IsSelectedChanged_On_Item_Should_Update_Selection()
        {
            var items = new[]
            {
                new Item(),
                new Item(),
            };

            var target = new SelectingItemsControl
            {
                Items = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            target.SelectedItem = items[1];

            Assert.False(items[0].IsSelected);
            Assert.True(items[1].IsSelected);

            items[0].IsSelected = true;
            items[0].RaiseEvent(new RoutedEventArgs(SelectingItemsControl.IsSelectedChangedEvent));

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(items[0], target.SelectedItem);
            Assert.True(items[0].IsSelected);
            Assert.False(items[1].IsSelected);
        }

        [Fact]
        public void Clearing_IsSelected_And_Raising_IsSelectedChanged_On_Item_Should_Update_Selection()
        {
            var items = new[]
            {
                new Item(),
                new Item(),
            };

            var target = new SelectingItemsControl
            {
                Items = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            target.SelectedItem = items[1];

            Assert.False(items[0].IsSelected);
            Assert.True(items[1].IsSelected);

            items[1].IsSelected = false;
            items[1].RaiseEvent(new RoutedEventArgs(SelectingItemsControl.IsSelectedChangedEvent));

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Null(target.SelectedItem);
        }

        [Fact]
        public void Raising_IsSelectedChanged_On_Someone_Elses_Item_Should_Not_Update_Selection()
        {
            var items = new[]
            {
                new Item(),
                new Item(),
            };

            var target = new SelectingItemsControl
            {
                Items = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedItem = items[1];

            var notChild = new Item
            {
                IsSelected = true,
            };

            target.RaiseEvent(new RoutedEventArgs
            {
                RoutedEvent = SelectingItemsControl.IsSelectedChangedEvent,
                Source = notChild,
            });

            Assert.Equal(target.SelectedItem, items[1]);
        }

        [Fact]
        public void Setting_SelectedIndex_Should_Raise_SelectionChanged_Event()
        {
            var items = new[]
            {
                new Item(),
                new Item(),
            };

            var target = new SelectingItemsControl
            {
                Items = items,
                Template = Template(),
            };

            var called = false;

            target.SelectionChanged += (s, e) =>
            {
                Assert.Same(items[1], e.AddedItems.Cast<object>().Single());
                Assert.Empty(e.RemovedItems);
                called = true;
            };

            target.SelectedIndex = 1;

            Assert.True(called);
        }

        [Fact]
        public void Clearing_SelectedIndex_Should_Raise_SelectionChanged_Event()
        {
            var items = new[]
            {
                new Item(),
                new Item(),
            };

            var target = new SelectingItemsControl
            {
                Items = items,
                Template = Template(),
                SelectedIndex = 1,
            };

            var called = false;

            target.SelectionChanged += (s, e) =>
            {
                Assert.Same(items[1], e.RemovedItems.Cast<object>().Single());
                Assert.Empty(e.AddedItems);
                called = true;
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            target.SelectedIndex = -1;

            Assert.True(called);
        }

        [Fact]
        public void Order_Of_Setting_Items_And_SelectedIndex_During_Initialization_Should_Not_Matter()
        {
            var items = new[] { "Foo", "Bar" };
            var target = new SelectingItemsControl();

            ((ISupportInitialize)target).BeginInit();
            target.SelectedIndex = 1;
            target.Items = items;
            ((ISupportInitialize)target).EndInit();

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal("Bar", target.SelectedItem);
        }

        [Fact]
        public void Order_Of_Setting_Items_And_SelectedItem_During_Initialization_Should_Not_Matter()
        {
            var items = new[] { "Foo", "Bar" };
            var target = new SelectingItemsControl();

            ((ISupportInitialize)target).BeginInit();
            target.SelectedItem = "Bar";
            target.Items = items;
            ((ISupportInitialize)target).EndInit();

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal("Bar", target.SelectedItem);
        }

        [Fact]
        public void Changing_DataContext_Should_Not_Clear_Nested_ViewModel_SelectedItem()
        {
            var items = new[]
            {
                new Item(),
                new Item(),
            };

            var vm = new MasterViewModel
            {
                Child = new ChildViewModel
                {
                    Items = items,
                    SelectedItem = items[1],
                }
            };

            var target = new SelectingItemsControl { DataContext = vm };
            var itemsBinding = new Binding("Child.Items");
            var selectedBinding = new Binding("Child.SelectedItem");

            target.Bind(SelectingItemsControl.ItemsProperty, itemsBinding);
            target.Bind(SelectingItemsControl.SelectedItemProperty, selectedBinding);

            Assert.Equal(1, target.SelectedIndex);
            Assert.Same(vm.Child.SelectedItem, target.SelectedItem);

            items = new[]
            {
                new Item { Value = "Item1" },
                new Item { Value = "Item2" },
                new Item { Value = "Item3" },
            };

            vm = new MasterViewModel
            {
                Child = new ChildViewModel
                {
                    Items = items,
                    SelectedItem = items[2],
                }
            };

            target.DataContext = vm;

            Assert.Equal(2, target.SelectedIndex);
            Assert.Same(vm.Child.SelectedItem, target.SelectedItem);
        }

        [Fact]
        public void Nested_ListBox_Does_Not_Change_Parent_SelectedIndex()
        {
            SelectingItemsControl nested;

            var root = new SelectingItemsControl
            {
                Template = Template(),
                Items = new IControl[]
                {
                    new Border(),
                    nested = new ListBox
                    {
                        Template = Template(),
                        Items = new[] { "foo", "bar" },
                        SelectedIndex = 1,
                    }
                },
                SelectedIndex = 0,
            };

            root.ApplyTemplate();
            root.Presenter.ApplyTemplate();
            nested.ApplyTemplate();
            nested.Presenter.ApplyTemplate();

            Assert.Equal(0, root.SelectedIndex);
            Assert.Equal(1, nested.SelectedIndex);

            nested.SelectedIndex = 0;

            Assert.Equal(0, root.SelectedIndex);
        }

        [Fact]
        public void Setting_SelectedItem_With_Pointer_Should_Set_TabOnceActiveElement()
        {
            var target = new ListBox
            {
                Template = Template(),
                Items = new[] { "Foo", "Bar", "Baz " },
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            target.Presenter.Panel.Children[1].RaiseEvent(new PointerPressedEventArgs
            {
                RoutedEvent = InputElement.PointerPressedEvent,
                MouseButton = MouseButton.Left,
            });

            var panel = target.Presenter.Panel;

            Assert.Equal(
                KeyboardNavigation.GetTabOnceActiveElement((InputElement)panel),
                panel.Children[1]);
        }

        [Fact]
        public void Removing_SelectedItem_Should_Clear_TabOnceActiveElement()
        {
            var items = new ObservableCollection<string>(new[] { "Foo", "Bar", "Baz " });

            var target = new ListBox
            {
                Template = Template(),
                Items = items,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            target.Presenter.Panel.Children[1].RaiseEvent(new PointerPressedEventArgs
            {
                RoutedEvent = InputElement.PointerPressedEvent,
                MouseButton = MouseButton.Left,
            });

            items.RemoveAt(1);

            var panel = target.Presenter.Panel;

            Assert.Null(KeyboardNavigation.GetTabOnceActiveElement((InputElement)panel));
        }

        private FuncControlTemplate Template()
        {
            return new FuncControlTemplate<SelectingItemsControl>(control =>
                new ItemsPresenter
                {
                    Name = "itemsPresenter",
                    [~ItemsPresenter.ItemsProperty] = control[~ItemsControl.ItemsProperty],
                    [~ItemsPresenter.ItemsPanelProperty] = control[~ItemsControl.ItemsPanelProperty],
                });
        }

        private class Item : Control, ISelectable
        {
            public string Value { get; set; }
            public bool IsSelected { get; set; }
        }

        private class MasterViewModel : NotifyingBase
        {
            private ChildViewModel _child;

            public ChildViewModel Child
            {
                get { return _child; }
                set
                {
                    _child = value;
                    RaisePropertyChanged();
                }
            }
        }

        private class ChildViewModel : NotifyingBase
        {
            public IList<Item> Items { get; set; }
            public Item SelectedItem { get; set; }
        }
    }
}
