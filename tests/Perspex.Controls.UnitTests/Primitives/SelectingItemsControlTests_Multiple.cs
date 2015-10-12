// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Perspex.Collections;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Xunit;

namespace Perspex.Controls.UnitTests.Primitives
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

            Assert.Equal(new[] { "bar" }, target.SelectedItems.ToList());
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
            target.SelectedItems = new[] { "bar" };

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
            target.SelectedItems = new PerspexList<object>();

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Equal(null, target.SelectedItem);
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
            target.SelectedItems = new PerspexList<object> { items[0], items[1] };

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

            target.SelectedItems = new PerspexList<object> { items[0], items[1] };

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

            Assert.Equal(new[] { "bar", "baz", "qux" }, target.SelectedItems.ToList());
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

            Assert.Equal(new[] { "qux", "baz", "bar" }, target.SelectedItems.ToList());
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

            Assert.Equal(new[] { "baz", "qux", "qiz" }, target.SelectedItems.ToList());
        }

        private class TestSelector : SelectingItemsControl
        {
            public new IList<object> SelectedItems
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

        private ControlTemplate Template()
        {
            return new ControlTemplate<SelectingItemsControl>(control =>
                new ItemsPresenter
                {
                    Name = "itemsPresenter",
                    [~ItemsPresenter.ItemsProperty] = control[~ItemsControl.ItemsProperty],
                    [~ItemsPresenter.ItemsPanelProperty] = control[~ItemsControl.ItemsPanelProperty],
                });
        }
    }
}
