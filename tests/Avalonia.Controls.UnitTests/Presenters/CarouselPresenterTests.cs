// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Moq;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Xunit;
using System.Collections.ObjectModel;

namespace Avalonia.Controls.UnitTests.Presenters
{
    public class CarouselPresenterTests
    {
        [Fact]
        public void Should_Register_With_Host_When_TemplatedParent_Set()
        {
            var host = new Mock<IItemsPresenterHost>();
            var target = new CarouselPresenter();

            target.SetValue(Control.TemplatedParentProperty, host.Object);

            host.Verify(x => x.RegisterItemsPresenter(target));
        }

        [Fact]
        public void ApplyTemplate_Should_Create_Panel()
        {
            var target = new CarouselPresenter
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
            var target = new CarouselPresenter
            {
                TemplatedParent = parent,
            };

            Assert.IsType<ItemContainerGenerator<TestItem>>(target.ItemContainerGenerator);
        }

        [Fact]
        public void Setting_SelectedIndex_Should_Show_Page()
        {
            var target = new CarouselPresenter
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
            var target = new CarouselPresenter
            {
                Items = new[] { "foo", "bar" },
                SelectedIndex = 0,
            };

            target.ApplyTemplate();
            target.SelectedIndex = 1;

            Assert.IsType<TextBlock>(target.Panel.Children[0]);
            Assert.Equal("bar", ((TextBlock)target.Panel.Children[0]).Text);
        }

        [Fact]
        public void Should_Remove_NonCurrent_Page_When_IsVirtualized_True()
        {
            var target = new CarouselPresenter
            {
                Items = new[] { "foo", "bar" },
                IsVirtualized = true,
                SelectedIndex = 0,
            };

            target.ApplyTemplate();
            Assert.Equal(1, target.ItemContainerGenerator.Containers.Count());
            target.SelectedIndex = 1;
            Assert.Equal(1, target.ItemContainerGenerator.Containers.Count());
        }

        [Fact]
        public void Should_Not_Remove_NonCurrent_Page_When_IsVirtualized_False()
        {
            var target = new CarouselPresenter
            {
                Items = new[] { "foo", "bar" },
                IsVirtualized = false,
                SelectedIndex = 0,
            };

            target.ApplyTemplate();
            Assert.Equal(1, target.ItemContainerGenerator.Containers.Count());
            Assert.Equal(1, target.Panel.Children.Count);
            target.SelectedIndex = 1;
            Assert.Equal(2, target.ItemContainerGenerator.Containers.Count());
            Assert.Equal(2, target.Panel.Children.Count);
            target.SelectedIndex = 0;
            Assert.Equal(2, target.ItemContainerGenerator.Containers.Count());
            Assert.Equal(2, target.Panel.Children.Count);
        }

        [Fact]
        public void Should_Remove_Controls_When_IsVirtualized_Is_False()
        {
            ObservableCollection<string> items = new ObservableCollection<string>();
            
            var target = new CarouselPresenter
            {
                Items = items,              
                SelectedIndex = 0,
                IsVirtualized = false,
            };

            target.ApplyTemplate();
            target.SelectedIndex = 0;
            items.Add("foo");
            target.SelectedIndex = 0;

            Assert.Equal(1, target.ItemContainerGenerator.Containers.Count());
            Assert.Equal(1, target.Panel.Children.Count);

            items.Add("bar");
            Assert.Equal(1, target.ItemContainerGenerator.Containers.Count());
            Assert.Equal(1, target.Panel.Children.Count);

            target.SelectedIndex = 1;
            Assert.Equal(2, target.ItemContainerGenerator.Containers.Count());
            Assert.Equal(2, target.Panel.Children.Count);            

            items.Remove(items[0]);
            Assert.Equal(1, target.ItemContainerGenerator.Containers.Count());
            Assert.Equal(1, target.Panel.Children.Count);            

            items.Remove(items[0]);
            Assert.Equal(0, target.ItemContainerGenerator.Containers.Count());
            Assert.Equal(0, target.Panel.Children.Count);            
        }

        [Fact]
        public void Should_have_correct_index_itemscontainer()
        {
            ObservableCollection<string> items = new ObservableCollection<string>();

            var target = new CarouselPresenter
            {
                Items = items,
                SelectedIndex = 0,
                IsVirtualized = false,
            };

            target.ApplyTemplate();
            target.SelectedIndex = 0;
            items.Add("foo");
            target.SelectedIndex = 0;

            Assert.Equal(1, target.ItemContainerGenerator.Containers.Count());
            Assert.Equal(1, target.Panel.Children.Count);

            items.Add("bar");
            Assert.Equal(1, target.ItemContainerGenerator.Containers.Count());
            Assert.Equal(1, target.Panel.Children.Count);

            target.SelectedIndex = 1;
            Assert.Equal(2, target.ItemContainerGenerator.Containers.Count());
            Assert.Equal(2, target.Panel.Children.Count);
            Assert.Equal(0, target.ItemContainerGenerator.Containers.First().Index);

            items.Remove(items[0]);
            Assert.Equal(1, target.ItemContainerGenerator.Containers.Count());
            Assert.Equal(1, target.Panel.Children.Count);
            Assert.Equal(1, target.ItemContainerGenerator.Containers.First().Index);

            items.Remove(items[0]);
            Assert.Equal(0, target.ItemContainerGenerator.Containers.Count());
            Assert.Equal(0, target.Panel.Children.Count);            
        }

        private class TestItem : ContentControl
        {
        }

        private class TestItemsControl : ItemsControl
        {
            protected override IItemContainerGenerator CreateItemContainerGenerator()
            {
                return new ItemContainerGenerator<TestItem>(this, TestItem.ContentProperty);
            }
        }
    }
}
