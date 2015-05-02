// -----------------------------------------------------------------------
// <copyright file="SelectingItemsControlTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives.UnitTests
{
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Input;
    using Xunit;

    public class SelectingItemsControlTests
    {
        [Fact]
        public void Item_IsSelected_Should_Initially_Be_False()
        {
            var items = new[]
            {
                new Item(),
                new Item(),
            };

            var target = new Target
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

            var target = new Target
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
        public void Setting_SelectedItem_Should_Set_SelectedIndex()
        {
            var items = new[]
            {
                new Item(),
                new Item(),
            };

            var target = new Target
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

            var target = new Target
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

            var target = new Target
            {
                Items = items,
                Template = this.Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 1;

            Assert.Equal(items[1], target.SelectedItem);
        }

        [Fact]
        public void Setting_SelectedIndex_Should_Coerce()
        {
            var items = new[]
            {
                new Item(),
                new Item(),
            };
            
            var target = new Target
            {
                Items = items,
                Template = this.Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 2;

            Assert.Equal(1, target.SelectedIndex);
        }

        [Fact]
        public void Setting_SelectedIndex_With_No_Items_Should_Not_Throw_Exception()
        {
            var target = new Target
            {
                Template = this.Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 2;

            Assert.Equal(-1, target.SelectedIndex);
        }

        [Fact]
        public void Setting_SelectedItem_With_No_Items_Should_Not_Throw_Exception()
        {
            var target = new Target
            {
                Template = this.Template(),
            };

            target.ApplyTemplate();
            target.SelectedItem = new Item();
        }

        [Fact]
        public void PointerPressed_Event_Should_Be_Handled()
        {
            var target = new Target();

            var e = new PointerPressEventArgs
            {
                RoutedEvent = InputElement.PointerPressedEvent
            };

            target.RaiseEvent(e);

            Assert.True(e.Handled);
        }

        private ControlTemplate Template()
        {
            return ControlTemplate.Create<Target>(control =>
                new ItemsPresenter
                {
                    Id = "itemsPresenter",
                    [~ItemsPresenter.ItemsProperty] = control[~ListBox.ItemsProperty],
                    [~ItemsPresenter.ItemsPanelProperty] = control[~ListBox.ItemsPanelProperty],
                });
        }

        private class Target : SelectingItemsControl
        {
        }

        private class Item : Control, ISelectable
        {
            public bool IsSelected { get; set; }
        }
    }
}
