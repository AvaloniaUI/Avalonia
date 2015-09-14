// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls.Generators;
using Perspex.Controls.Presenters;
using Perspex.Controls.Templates;
using Xunit;

namespace Perspex.Controls.UnitTests.Presenters
{
    public class DeckPresenterTests
    {
        [Fact]
        public void ApplyTemplate_Should_Create_Panel()
        {
            var target = new DeckPresenter
            {
                ItemsPanel = new FuncTemplate<IPanel>(() => new Panel()),
            };

            target.ApplyTemplate();

            Assert.IsType<Panel>(target.Panel);
        }

        [Fact]
        public void ItemContainerGenerator_Should_Be_Picked_Up_From_TemplatedControl()
        {
            var parent = new TestItemsControl();
            var target = new DeckPresenter
            {
                TemplatedParent = parent,
            };

            Assert.IsType<ItemContainerGenerator<TestItem>>(target.ItemContainerGenerator);
        }

        [Fact]
        public void Setting_SelectedIndex_Should_Show_Page()
        {
            var target = new DeckPresenter
            {
                Items = new[] { "foo", "bar" },
                SelectedIndex = 0,
            };

            target.ApplyTemplate();

            Assert.IsType<TextBlock>(target.Panel.Children[0]);
            Assert.Equal("foo", ((TextBlock)target.Panel.Children[0]).Text);
        }

        [Fact]
        public void Changing_SelectedIndex_Should_Show_Page()
        {
            var target = new DeckPresenter
            {
                Items = new[] { "foo", "bar" },
                SelectedIndex = 0,
            };

            target.ApplyTemplate();
            target.SelectedIndex = 1;

            Assert.IsType<TextBlock>(target.Panel.Children[0]);
            Assert.Equal("bar", ((TextBlock)target.Panel.Children[0]).Text);
        }

        private class TestItem : ContentControl
        {
        }

        private class TestItemsControl : ItemsControl
        {
            protected override IItemContainerGenerator CreateItemContainerGenerator()
            {
                return new ItemContainerGenerator<TestItem>(this);
            }
        }
    }
}
