// -----------------------------------------------------------------------
// <copyright file="SelectingItemsControlTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests.Primitives
{
    using System.Collections.ObjectModel;
    using Perspex.Collections;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Templates;
    using Perspex.Input;
    using Perspex.Interactivity;
    using Xunit;

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
                Template = this.Template(),
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
                Template = this.Template(),
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
                Template = this.Template(),
            };

            target.ApplyTemplate();
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
                Template = this.Template(),
            };

            target.SelectedItem = items[1];
            target.ApplyTemplate();

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
                Template = this.Template(),
            };

            target.SelectedIndex = 1;
            target.ApplyTemplate();

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
                Template = this.Template(),
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
                Template = this.Template(),
            };

            target.ApplyTemplate();
            target.SelectedItem = items[1];

            Assert.Equal(items[1], target.SelectedItem);
            Assert.Equal(1, target.SelectedIndex);

            target.SelectedItem = new Item();

            Assert.Equal(null, target.SelectedItem);
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
                Template = this.Template(),
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
                Template = this.Template(),
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
                Template = this.Template(),
            };

            target.ApplyTemplate();
            target.SelectedItem = new Item();

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Null(target.SelectedItem);
        }

        [Fact]
        public void Adding_Selected_Item_Should_Update_Selection()
        {
            var items = new PerspexList<Item>(new[]
            {
                new Item(),
                new Item(),
            });

            var target = new SelectingItemsControl
            {
                Items = items,
                Template = this.Template(),
            };

            target.ApplyTemplate();
            items.Add(new Item { IsSelected = true });

            Assert.Equal(2, target.SelectedIndex);
            Assert.Equal(items[2], target.SelectedItem);
        }

        [Fact]
        public void Setting_Items_To_Null_Should_Clear_Selection()
        {
            var items = new PerspexList<Item>
            {
                new Item(),
                new Item(),
            };

            var target = new SelectingItemsControl
            {
                Items = items,
                Template = this.Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 1;

            Assert.Equal(items[1], target.SelectedItem);
            Assert.Equal(1, target.SelectedIndex);

            target.Items = null;

            Assert.Equal(null, target.SelectedItem);
            Assert.Equal(-1, target.SelectedIndex);
        }

        [Fact]
        public void Removing_Selected_Item_Should_Clear_Selection()
        {
            var items = new PerspexList<Item>
            {
                new Item(),
                new Item(),
            };

            var target = new SelectingItemsControl
            {
                Items = items,
                Template = this.Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 1;

            Assert.Equal(items[1], target.SelectedItem);
            Assert.Equal(1, target.SelectedIndex);

            items.RemoveAt(1);

            Assert.Equal(null, target.SelectedItem);
            Assert.Equal(-1, target.SelectedIndex);
        }

        [Fact]
        public void Resetting_Items_Collection_Should_Clear_Selection()
        {
            // Need to use ObservableCollection here as PerspexList signals a Clear as an
            // add + remove.
            var items = new ObservableCollection<Item>
            {
                new Item(),
                new Item(),
            };

            var target = new SelectingItemsControl
            {
                Items = items,
                Template = this.Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 1;

            Assert.Equal(items[1], target.SelectedItem);
            Assert.Equal(1, target.SelectedIndex);

            items.Clear();

            Assert.Equal(null, target.SelectedItem);
            Assert.Equal(-1, target.SelectedIndex);
        }

        [Fact]
        public void Focusing_Item_With_Pointer_Should_Select_It()
        {
            var target = new SelectingItemsControl
            {
                Template = this.Template(),
                Items = new[] { "foo", "bar" },
            };

            target.ApplyTemplate();

            var e = new GotFocusEventArgs
            {
                RoutedEvent = InputElement.GotFocusEvent,
                NavigationMethod = NavigationMethod.Pointer,
            };

            target.Presenter.Panel.Children[1].RaiseEvent(e);

            Assert.Equal(1, target.SelectedIndex);

            // GotFocus should be raised on parent control.
            Assert.False(e.Handled);
        }

        [Fact]
        public void Focusing_Item_With_Directional_Keys_Should_Select_It()
        {
            var target = new SelectingItemsControl
            {
                Template = this.Template(),
                Items = new[] { "foo", "bar" },
            };

            target.ApplyTemplate();

            var e = new GotFocusEventArgs
            {
                RoutedEvent = InputElement.GotFocusEvent,
                NavigationMethod = NavigationMethod.Directional,
            };

            target.Presenter.Panel.Children[1].RaiseEvent(e);

            Assert.Equal(1, target.SelectedIndex);
            Assert.False(e.Handled);
        }

        [Fact]
        public void Focusing_Item_With_Tab_Should_Not_Select_It()
        {
            var target = new SelectingItemsControl
            {
                Template = this.Template(),
                Items = new[] { "foo", "bar" },
            };

            target.ApplyTemplate();

            var e = new GotFocusEventArgs
            {
                RoutedEvent = InputElement.GotFocusEvent,
                NavigationMethod = NavigationMethod.Tab,
            };

            target.Presenter.Panel.Children[1].RaiseEvent(e);

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
                Template = this.Template(),
            };

            target.ApplyTemplate();
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
                Template = this.Template(),
            };

            target.ApplyTemplate();
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
                Template = this.Template(),
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
                OriginalSource = notChild,
            });

            Assert.Equal(target.SelectedItem, items[1]);
        }

        private ControlTemplate Template()
        {
            return new ControlTemplate<SelectingItemsControl>(control =>
                new ItemsPresenter
                {
                    Name = "itemsPresenter",
                    [~ItemsPresenter.ItemsProperty] = control[~ListBox.ItemsProperty],
                    [~ItemsPresenter.ItemsPanelProperty] = control[~ListBox.ItemsPanelProperty],
                });
        }

        private class Item : Control, ISelectable
        {
            public bool IsSelected { get; set; }
        }
    }
}
