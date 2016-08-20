// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TabControlTests
    {
        [Fact]
        public void First_Tab_Should_Be_Selected_By_Default()
        {
            TabItem selected;
            var target = new TabControl
            {
                Template = TabControlTemplate(),
                Items = new[]
                {
                    (selected = new TabItem
                    {
                        Name = "first",
                        Content = "foo",
                    }),
                    new TabItem
                    {
                        Name = "second",
                        Content = "bar",
                    },
                }
            };

            target.ApplyTemplate();

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(selected, target.SelectedItem);
        }

        [Fact]
        public void Logical_Children_Should_Be_TabItems()
        {
            var items = new[]
            {
                new TabItem
                {
                    Content = "foo"
                },
                new TabItem
                {
                    Content = "bar"
                },
            };

            var target = new TabControl
            {
                Template = TabControlTemplate(),
                Items = items,
            };

            Assert.Equal(items, target.GetLogicalChildren());
            target.ApplyTemplate();
            Assert.Equal(items, target.GetLogicalChildren());
        }

        [Fact]
        public void Removal_Should_Set_Next_Tab()
        {
            var collection = new ObservableCollection<TabItem>()
            {
                new TabItem
                {
                    Name = "first",
                    Content = "foo",
                },
                new TabItem
                {
                    Name = "second",
                    Content = "bar",
                },
                new TabItem
                {
                    Name = "3rd",
                    Content = "barf",
                },
            };

            var target = new TabControl
            {
                Template = TabControlTemplate(),
                Items = collection,
            };

            target.ApplyTemplate();
            target.SelectedItem = collection[1];
            collection.RemoveAt(1);

            // compare with former [2] now [1] == "3rd"
            Assert.Same(collection[1], target.SelectedItem);
        }


        [Fact]
        public void TabItem_Templates_Should_Be_Set_Before_TabItem_ApplyTemplate()
        {
            var collection = new[]
            {
                new TabItem
                {
                    Name = "first",
                    Content = "foo",
                },
                new TabItem
                {
                    Name = "second",
                    Content = "bar",
                },
                new TabItem
                {
                    Name = "3rd",
                    Content = "barf",
                },
            };

            var template = new FuncControlTemplate<TabItem>(x => new Decorator());

            using (UnitTestApplication.Start(TestServices.RealStyler))
            {
                var root = new TestRoot
                {
                    Styles = new Styles
                    {
                        new Style(x => x.OfType<TabItem>())
                        {
                            Setters = new[]
                            {
                                new Setter(TemplatedControl.TemplateProperty, template)
                            }
                        }
                    },
                    Child = new TabControl
                    {
                        Template = TabControlTemplate(),
                        Items = collection,
                    }
                };
            }

            Assert.Same(collection[0].Template, template);
            Assert.Same(collection[1].Template, template);
            Assert.Same(collection[2].Template, template);
        }

        [Fact]
        public void DataContexts_Should_Be_Correctly_Set()
        {
            var items = new object[]
            {
                "Foo",
                new Item("Bar"),
                new TextBlock { Text = "Baz" },
                new TabItem { Content = "Qux" },
                new TabItem { Content = new TextBlock { Text = "Bob" } }
            };

            var target = new TabControl
            {
                Template = TabControlTemplate(),
                DataContext = "Base",
                DataTemplates = new DataTemplates
                {
                    new FuncDataTemplate<Item>(x => new Button { Content = x })
                },
                Items = items,
            };

            ApplyTemplate(target);
            var carousel = (Carousel)target.Pages;

            var container = (ContentPresenter)carousel.Presenter.Panel.Children.Single();
            container.UpdateChild();
            var dataContext = ((TextBlock)container.Child).DataContext;
            Assert.Equal(items[0], dataContext);

            target.SelectedIndex = 1;
            container = (ContentPresenter)carousel.Presenter.Panel.Children.Single();
            container.UpdateChild();
            dataContext = ((Button)container.Child).DataContext;
            Assert.Equal(items[1], dataContext);

            target.SelectedIndex = 2;
            dataContext = ((TextBlock)carousel.Presenter.Panel.Children.Single()).DataContext;
            Assert.Equal("Base", dataContext);

            target.SelectedIndex = 3;
            container = (ContentPresenter)carousel.Presenter.Panel.Children[0];
            container.UpdateChild();
            dataContext = ((TextBlock)container.Child).DataContext;
            Assert.Equal("Qux", dataContext);

            target.SelectedIndex = 4;
            dataContext = ((TextBlock)carousel.Presenter.Panel.Children.Single()).DataContext;
            Assert.Equal("Base", dataContext);
        }

        [Fact]
        public void Should_Handle_Changing_To_TabItem_With_Null_Content()
        {
            TabControl target = new TabControl
            {
                Template = TabControlTemplate(),
                Items = new[]
                {
                    new TabItem { Header = "Foo" },
                    new TabItem { Header = "Foo", Content = new Decorator() },
                    new TabItem { Header = "Baz" },
                },
            };

            ApplyTemplate(target);

            target.SelectedIndex = 2;

            var carousel = (Carousel)target.Pages;
            var page = (TabItem)carousel.SelectedItem;

            Assert.Null(page.Content);
        }

        private IControlTemplate TabControlTemplate()
        {
            return new FuncControlTemplate<TabControl>(parent => 
                new StackPanel
                {
                    Children = new Controls
                    {
                        new ItemsPresenter
                        {
                            Name = "PART_ItemsPresenter",
                            [!TabStrip.ItemsProperty] = parent[!TabControl.ItemsProperty],
                            [!TabStrip.ItemTemplateProperty] = parent[!TabControl.ItemTemplateProperty],
                        },
                        new Carousel
                        {
                            Name = "PART_Content",
                            Template = new FuncControlTemplate<Carousel>(CreateCarouselTemplate),
                            MemberSelector = TabControl.ContentSelector,
                            [!Carousel.ItemsProperty] = parent[!TabControl.ItemsProperty],
                            [!Carousel.SelectedIndexProperty] = parent[!TabControl.SelectedIndexProperty],
                        }
                    }
                });
        }

        private IControlTemplate TabItemTemplate()
        {
            return new FuncControlTemplate<TabItem>(parent =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = parent[!TabItem.ContentProperty],
                    [!ContentPresenter.ContentTemplateProperty] = parent[!TabItem.ContentTemplateProperty],
                });
        }

        private Control CreateCarouselTemplate(Carousel control)
        {
            return new CarouselPresenter
            {
                Name = "PART_ItemsPresenter",
                [!CarouselPresenter.ItemsProperty] = control[!ItemsControl.ItemsProperty],
                [!CarouselPresenter.ItemsPanelProperty] = control[!ItemsControl.ItemsPanelProperty],
                [!CarouselPresenter.MemberSelectorProperty] = control[!ItemsControl.MemberSelectorProperty],
                [!CarouselPresenter.SelectedIndexProperty] = control[!SelectingItemsControl.SelectedIndexProperty],
                [~CarouselPresenter.TransitionProperty] = control[~Carousel.TransitionProperty],
            };
        }

        private void ApplyTemplate(TabControl target)
        {
            target.ApplyTemplate();
            var carousel = (Carousel)target.Pages;
            carousel.ApplyTemplate();
            carousel.Presenter.ApplyTemplate();
        }

        private class Item
        {
            public Item(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }

        private class TabViewModel
        {
            public TabViewModel(string header, string content, bool enabled = true)
            {
                Header = header;
                Content = content;
                Enabled = enabled;
            }

            public string Header { get; }
            public string Content { get; }
            public bool Enabled { get; }
        }
    }
}
