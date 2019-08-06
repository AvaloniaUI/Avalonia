// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class SelectingItemsControlTests_AutoSelect
    {
        [Fact]
        public void First_Item_Should_Be_Selected()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar" },
                Template = Template(),
            };

            target.ApplyTemplate();

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("foo", target.SelectedItem);
        }

        [Fact]
        public void First_Item_Should_Be_Selected_When_Added()
        {
            var items = new AvaloniaList<string>();
            var target = new TestSelector
            {
                Items = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            items.Add("foo");

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("foo", target.SelectedItem);
        }


        [Fact]
        public void First_Item_Should_Be_Selected_When_Reset()
        {
            var items = new ResetOnAdd();
            var target = new TestSelector
            {
                Items = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            items.Add("foo");

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("foo", target.SelectedItem);
        }

        [Fact]
        public void Item_Should_Be_Selected_When_Selection_Removed()
        {
            var items = new AvaloniaList<string>(new[] { "foo", "bar", "baz", "qux" });

            var target = new TestSelector
            {
                Items = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 2;
            items.RemoveAt(2);

            Assert.Equal(2, target.SelectedIndex);
            Assert.Equal("qux", target.SelectedItem);
        }

        [Fact]
        public void Selection_Should_Be_Cleared_When_No_Items_Left()
        {
            var items = new AvaloniaList<string>(new[] { "foo", "bar" });

            var target = new TestSelector
            {
                Items = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 1;
            items.RemoveAt(1);
            items.RemoveAt(0);

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Null(target.SelectedItem);
        }

        private FuncControlTemplate Template()
        {
            return new FuncControlTemplate<SelectingItemsControl>((control, scope) =>
                new ItemsPresenter
                {
                    Name = "itemsPresenter",
                    [~ItemsPresenter.ItemsProperty] = control[~ItemsControl.ItemsProperty],
                    [~ItemsPresenter.ItemsPanelProperty] = control[~ItemsControl.ItemsPanelProperty],
                }.RegisterInNameScope(scope));
        }

        private class TestSelector : SelectingItemsControl
        {
            static TestSelector()
            {
                SelectionModeProperty.OverrideDefaultValue<TestSelector>(SelectionMode.AlwaysSelected);
            }
        }

        private class ResetOnAdd : List<string>, INotifyCollectionChanged
        {
            public event NotifyCollectionChangedEventHandler CollectionChanged;

            public new void Add(string item)
            {
                base.Add(item);
                CollectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }
    }
}
