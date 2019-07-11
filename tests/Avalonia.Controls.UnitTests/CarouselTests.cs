// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class CarouselTests
    {
        [Fact]
        public void First_Item_Should_Be_Selected_By_Default()
        {
            var target = new Carousel
            {
                Template = new FuncControlTemplate<Carousel>(CreateTemplate),
                Items = new[]
                {
                    "Foo",
                    "Bar"
                }
            };

            target.ApplyTemplate();

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("Foo", target.SelectedItem);
        }

        [Fact]
        public void LogicalChild_Should_Be_Selected_Item()
        {
            var target = new Carousel
            {
                Template = new FuncControlTemplate<Carousel>(CreateTemplate),
                Items = new[]
                {
                    "Foo",
                    "Bar"
                }
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            Assert.Single(target.GetLogicalChildren());

            var child = target.GetLogicalChildren().Single();

            Assert.IsType<TextBlock>(child);
            Assert.Equal("Foo", ((TextBlock)child).Text);
        }

        [Fact]
        public void Should_Remove_NonCurrent_Page_When_IsVirtualized_True()
        {
            var target = new Carousel
            {
                Template = new FuncControlTemplate<Carousel>(CreateTemplate),
                Items = new[] { "foo", "bar" },
                IsVirtualized = true,
                SelectedIndex = 0,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            Assert.Single(target.ItemContainerGenerator.Containers);
            target.SelectedIndex = 1;
            Assert.Single(target.ItemContainerGenerator.Containers);
        }

        [Fact]
        public void Selected_Item_Changes_To_First_Item_When_Items_Property_Changes()
        {
            var items = new ObservableCollection<string>
            {
               "Foo",
               "Bar",
               "FooBar"
            };

            var target = new Carousel
            {
                Template = new FuncControlTemplate<Carousel>(CreateTemplate),
                Items = items,
                IsVirtualized = false
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            Assert.Equal(3, target.GetLogicalChildren().Count());

            var child = target.GetLogicalChildren().First();

            Assert.IsType<TextBlock>(child);
            Assert.Equal("Foo", ((TextBlock)child).Text);

            var newItems = items.ToList();
            newItems.RemoveAt(0);

            target.Items = newItems;

            child = target.GetLogicalChildren().First();

            Assert.IsType<TextBlock>(child);
            Assert.Equal("Bar", ((TextBlock)child).Text);
        }

        [Fact]
        public void Selected_Item_Changes_To_First_Item_When_Items_Property_Changes_And_Virtualized()
        {
            var items = new ObservableCollection<string>
            {
               "Foo",
               "Bar",
               "FooBar"
            };

            var target = new Carousel
            {
                Template = new FuncControlTemplate<Carousel>(CreateTemplate),
                Items = items,
                IsVirtualized = true,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            Assert.Single(target.GetLogicalChildren());

            var child = target.GetLogicalChildren().Single();

            Assert.IsType<TextBlock>(child);
            Assert.Equal("Foo", ((TextBlock)child).Text);

            var newItems = items.ToList();
            newItems.RemoveAt(0);

            target.Items = newItems;

            child = target.GetLogicalChildren().Single();

            Assert.IsType<TextBlock>(child);
            Assert.Equal("Bar", ((TextBlock)child).Text);
        }

        [Fact]
        public void Selected_Item_Changes_To_First_Item_When_Item_Added()
        {
            var items = new ObservableCollection<string>();
            var target = new Carousel
            {
                Template = new FuncControlTemplate<Carousel>(CreateTemplate),
                Items = items,
                IsVirtualized = false
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Empty(target.GetLogicalChildren());

            items.Add("Foo");

            Assert.Equal(0, target.SelectedIndex);
            Assert.Single(target.GetLogicalChildren());
        }

        [Fact]
        public void Selected_Index_Changes_To_When_Items_Assigned_Null()
        {
            var items = new ObservableCollection<string>
            {
               "Foo",
               "Bar",
               "FooBar"
            };

            var target = new Carousel
            {
                Template = new FuncControlTemplate<Carousel>(CreateTemplate),
                Items = items,
                IsVirtualized = false
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            Assert.Equal(3, target.GetLogicalChildren().Count());

            var child = target.GetLogicalChildren().First();

            Assert.IsType<TextBlock>(child);
            Assert.Equal("Foo", ((TextBlock)child).Text);

            target.Items = null;

            var numChildren = target.GetLogicalChildren().Count();

            Assert.Equal(0, numChildren);
            Assert.Equal(-1, target.SelectedIndex);
        }

        [Fact]
        public void Selected_Index_Is_Maintained_Carousel_Created_With_Non_Zero_SelectedIndex()
        {
            var items = new ObservableCollection<string>
            {
               "Foo",
               "Bar",
               "FooBar"
            };

            var target = new Carousel
            {
                Template = new FuncControlTemplate<Carousel>(CreateTemplate),
                Items = items,
                IsVirtualized = false,
                SelectedIndex = 2
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            Assert.Equal("FooBar", target.SelectedItem);

            var child = target.GetVisualDescendants().LastOrDefault();

            Assert.IsType<TextBlock>(child);
            Assert.Equal("FooBar", ((TextBlock)child).Text);
        }

        [Fact]
        public void Selected_Item_Changes_To_Next_First_Item_When_Item_Removed_From_Beggining_Of_List()
        {
            var items = new ObservableCollection<string>
            {
               "Foo",
               "Bar",
               "FooBar"
            };

            var target = new Carousel
            {
                Template = new FuncControlTemplate<Carousel>(CreateTemplate),
                Items = items,
                IsVirtualized = false
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            Assert.Equal(3, target.GetLogicalChildren().Count());

            var child = target.GetLogicalChildren().First();

            Assert.IsType<TextBlock>(child);
            Assert.Equal("Foo", ((TextBlock)child).Text);

            items.RemoveAt(0);

            child = target.GetLogicalChildren().First();

            Assert.IsType<TextBlock>(child);
            Assert.Equal("Bar", ((TextBlock)child).Text);
        }

        [Fact]
        public void Selected_Item_Changes_To_NextAvailable_Item_If_SelectedItem_Is_Removed_From_Middle()
        {
            var items = new ObservableCollection<string>
            {
               "Foo",
               "Bar",
               "FooBar"
            };

            var target = new Carousel
            {
                Template = new FuncControlTemplate<Carousel>(CreateTemplate),
                Items = items,
                IsVirtualized = false
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            target.SelectedIndex = 1;

            items.RemoveAt(1);

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal("FooBar", target.SelectedItem);
        }

        private Control CreateTemplate(Carousel control, INameScope scope)
        {
            return new CarouselPresenter
            {
                Name = "PART_ItemsPresenter",
                [~CarouselPresenter.IsVirtualizedProperty] = control[~Carousel.IsVirtualizedProperty],
                [~CarouselPresenter.ItemsProperty] = control[~Carousel.ItemsProperty],
                [~CarouselPresenter.ItemsPanelProperty] = control[~Carousel.ItemsPanelProperty],
                [~CarouselPresenter.SelectedIndexProperty] = control[~Carousel.SelectedIndexProperty],
                [~CarouselPresenter.PageTransitionProperty] = control[~Carousel.PageTransitionProperty],
            }.RegisterInNameScope(scope);
        }
    }
}
