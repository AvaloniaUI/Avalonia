// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
        public void Setting_SelectedIndex_Should_Add_To_SelectedIndexes()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar" },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 1;

            Assert.Equal(new[] { 1 }, target.SelectedIndexes);
        }

        [Fact]
        public void Adding_SelectedIndexes_Should_Set_SelectedIndex()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar" },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndexes.Add(1);

            Assert.Equal(1, target.SelectedIndex);
        }

        [Fact]
        public void Adding_First_SelectedIndex_Should_Raise_SelectedIndex_SelectedItem_Changed()
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
            target.SelectedIndexes.Add(1);

            Assert.True(indexRaised);
            Assert.True(itemRaised);
        }

        [Fact]
        public void Adding_Subsequent_SelectedIndexes_Should_Not_Raise_SelectedIndex_SelectedItem_Changed()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar" },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndexes.Add(0);

            bool raised = false;
            target.PropertyChanged += (s, e) => 
                raised |= e.Property.Name == "SelectedIndex" ||
                          e.Property.Name == "SelectedItem";

            target.SelectedIndexes.Add(1);

            Assert.False(raised);
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
        public void Removing_Last_SelectedIndex_Should_Raise_SelectedIndex_Changed()
        {
            var target = new TestSelector
            {
                Items = new[] { "foo", "bar" },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndexes.Add(0);

            bool raised = false;
            target.PropertyChanged += (s, e) => 
                raised = e.Property.Name == "SelectedIndex" && 
                         (int)e.OldValue == 0 && 
                         (int)e.NewValue == -1;

            target.SelectedIndexes.RemoveAt(0);

            Assert.True(raised);
        }

        [Fact]
        public void Adding_To_SelectedIndexes_Should_Add_To_SelectedItems()
        {
            var target = new TestSelector
            {
                Items = new[]
                {
                    "foo",
                    "bar",
                },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndexes.Add(1);

            Assert.Equal(new[] { "bar" }, target.SelectedItems);
        }

        [Fact]
        public void Adding_To_SelectedItems_Should_Add_To_SelectedIndexes()
        {
            var target = new TestSelector
            {
                Items = new[]
                {
                    "foo",
                    "bar",
                },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedItems.Add("bar");

            Assert.Equal(new[] { 1 }, target.SelectedIndexes);
        }

        [Fact]
        public void Adding_SelectedIndexes_Should_Set_Item_IsSelected()
        {
            var target = new TestSelector
            {
                Items = new[] 
                {
                    new ListBoxItem(),
                    new ListBoxItem(),
                },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndexes.Add(1);

            Assert.True(((ListBoxItem)target.Presenter.Panel.Children[1]).IsSelected);
        }

        [Fact]
        public void Removing_SelectedIndexes_Should_Clear_Item_IsSelected()
        {
            var target = new TestSelector
            {
                Items = new[]
                {
                    new ListBoxItem(),
                    new ListBoxItem(),
                },
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndexes.Add(1);
            target.SelectedIndexes.Remove(1);

            Assert.False(((ListBoxItem)target.Presenter.Panel.Children[1]).IsSelected);
        }

        private class TestSelector : SelectingItemsControl
        {
            public new IPerspexList<int> SelectedIndexes
            {
                get { return base.SelectedIndexes; }
            }

            public new IPerspexList<object> SelectedItems
            {
                get { return base.SelectedItems; }
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
