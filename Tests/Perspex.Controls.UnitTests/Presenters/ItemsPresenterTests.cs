// -----------------------------------------------------------------------
// <copyright file="ItemsPresenterTests.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests.Presenters
{
    using System.Linq;
    using Perspex.Collections;
    using Perspex.Controls.Generators;
    using Perspex.Controls.Presenters;
    using Perspex.Input;
    using Perspex.LogicalTree;
    using Perspex.VisualTree;
    using Xunit;

    public class ItemsPresenterTests
    {
        [Fact]
        public void Should_Add_Containers()
        {
            var target = new ItemsPresenter
            {
                Items = new[] { "foo", "bar" },
            };

            target.ApplyTemplate();

            Assert.Equal(2, target.Panel.Children.Count);
            Assert.IsType<TextBlock>(target.Panel.Children[0]);
            Assert.IsType<TextBlock>(target.Panel.Children[1]);
            Assert.Equal("foo", ((TextBlock)target.Panel.Children[0]).Text);
            Assert.Equal("bar", ((TextBlock)target.Panel.Children[1]).Text);
        }

        [Fact]
        public void Should_Add_Containers_Of_Correct_Type()
        {
            var target = new ItemsPresenter
            {
                Items = new[] { "foo", "bar" },
            };

            target.ItemContainerGenerator = new ItemContainerGenerator<ListBoxItem>(target);
            target.ApplyTemplate();

            Assert.Equal(2, target.Panel.Children.Count);
            Assert.IsType<ListBoxItem>(target.Panel.Children[0]);
            Assert.IsType<ListBoxItem>(target.Panel.Children[1]);
        }

        [Fact]
        public void ItemContainerGenerator_Should_Be_Picked_Up_From_TemplatedControl()
        {
            var parent = new TestItemsControl();
            var target = new ItemsPresenter
            {
                TemplatedParent = parent,
            };

            Assert.IsType<ItemContainerGenerator<TestItem>>(target.ItemContainerGenerator);
        }

        [Fact]
        public void Should_Remove_Containers()
        {
            var items = new PerspexList<string>(new[] { "foo", "bar" });
            var target = new ItemsPresenter
            {
                Items = items,
            };

            target.ApplyTemplate();
            items.RemoveAt(0);

            Assert.Equal(1, target.Panel.Children.Count);
            Assert.Equal("bar", ((TextBlock)target.Panel.Children[0]).Text);
        }

        [Fact]
        public void Clearing_Items_Should_Remove_Containers()
        {
            var target = new ItemsPresenter
            {
                Items = new[] { "foo", "bar" },
            };

            target.ApplyTemplate();
            target.Items = null;

            Assert.Empty(target.Panel.Children);
        }

        [Fact]
        public void Should_Handle_Null_Items()
        {
            var items = new PerspexList<string>(new[] { "foo", null, "bar" });

            var target = new ItemsPresenter
            {
                Items = items,
            };

            target.ApplyTemplate();

            var text = target.Panel.Children.OfType<TextBlock>().Select(x => x.Text).ToList();
            Assert.Equal(new[] { "foo", "bar" }, text);

            items.RemoveAt(1);

            text = target.Panel.Children.OfType<TextBlock>().Select(x => x.Text).ToList();
            Assert.Equal(new[] { "foo", "bar" }, text);
        }

        [Fact]
        public void Should_Handle_Duplicate_Items()
        {
            var items = new PerspexList<int>(new[] { 1, 2, 1 });

            var target = new ItemsPresenter
            {
                Items = items,
            };

            target.ApplyTemplate();
            items.RemoveAt(2);

            var text = target.Panel.Children.OfType<TextBlock>().Select(x => x.Text);
            Assert.Equal(new[] { "1", "2" }, text);
        }

        [Fact]
        public void Panel_Should_Be_Created_From_ItemsPanel_Template()
        {
            var panel = new Panel();
            var target = new ItemsPresenter
            {
                ItemsPanel = new ItemsPanelTemplate(() => panel),
            };

            target.ApplyTemplate();

            Assert.Equal(panel, target.Panel);
        }

        [Fact]
        public void Panel_TemplatedParent_Should_Be_Set()
        {
            var target = new ItemsPresenter();

            target.ApplyTemplate();

            Assert.Equal(target, target.Panel.TemplatedParent);
        }

        [Fact]
        public void Panel_TabNavigation_Should_Be_Set_To_Once()
        {
            var target = new ItemsPresenter();

            target.ApplyTemplate();

            Assert.Equal(KeyboardNavigationMode.Once, KeyboardNavigation.GetTabNavigation(target.Panel));
        }

        [Fact]
        public void Panel_TabNavigation_Should_Be_Set_To_ItemsPresenter_Value()
        {
            var target = new ItemsPresenter();

            KeyboardNavigation.SetTabNavigation(target, KeyboardNavigationMode.Cycle);
            target.ApplyTemplate();

            Assert.Equal(KeyboardNavigationMode.Cycle, KeyboardNavigation.GetTabNavigation(target.Panel));
        }

        [Fact]
        public void Panel_Should_Be_Visual_Child()
        {
            var target = new ItemsPresenter();

            target.ApplyTemplate();

            var child = target.GetVisualChildren().Single();

            Assert.Equal(target.Panel, child);
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
