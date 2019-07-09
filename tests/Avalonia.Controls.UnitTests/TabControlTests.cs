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

            var template = new FuncControlTemplate<TabItem>((x, __) => new Decorator());

            using (UnitTestApplication.Start(TestServices.RealStyler))
            {
                var root = new TestRoot
                {
                    Styles =
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
                DataTemplates =
                {
                    new FuncDataTemplate<Item>((x, __) => new Button { Content = x })
                },
                Items = items,
            };

            ApplyTemplate(target);

            target.ContentPart.UpdateChild();
            var dataContext = ((TextBlock)target.ContentPart.Child).DataContext;
            Assert.Equal(items[0], dataContext);

            target.SelectedIndex = 1;
            target.ContentPart.UpdateChild();
            dataContext = ((Button)target.ContentPart.Child).DataContext;
            Assert.Equal(items[1], dataContext);

            target.SelectedIndex = 2;
            target.ContentPart.UpdateChild();
            dataContext = ((TextBlock)target.ContentPart.Child).DataContext;
            Assert.Equal("Base", dataContext);

            target.SelectedIndex = 3;
            target.ContentPart.UpdateChild();
            dataContext = ((TextBlock)target.ContentPart.Child).DataContext;
            Assert.Equal("Qux", dataContext);

            target.SelectedIndex = 4;
            target.ContentPart.UpdateChild();
            dataContext = target.ContentPart.DataContext;
            Assert.Equal("Base", dataContext);
        }

        /// <summary>
        /// Non-headered control items should result in TabItems with empty header.
        /// </summary>
        /// <remarks>
        /// If a TabControl is created with non IHeadered controls as its items, don't try to
        /// display the control in the header: if the control is part of the header then
        /// *that* control would also end up in the content region, resulting in dual-parentage 
        /// breakage.
        /// </remarks>
        [Fact]
        public void Non_IHeadered_Control_Items_Should_Be_Ignored()
        {
            var items = new[]
            {
                new TextBlock { Text = "foo" },
                new TextBlock { Text = "bar" },
            };

            var target = new TabControl
            {
                Template = TabControlTemplate(),
                Items = items,
            };

            ApplyTemplate(target);

            var logicalChildren = target.ItemsPresenterPart.Panel.GetLogicalChildren();

            var result = logicalChildren
                .OfType<TabItem>()
                .Select(x => x.Header)
                .ToList();

            Assert.Equal(new object[] { null, null }, result);
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

            var page = (TabItem)target.SelectedItem;

            Assert.Null(page.Content);
        }

        [Fact]
        public void DataTemplate_Created_Content_Should_Be_Logical_Child_After_ApplyTemplate()
        {
            TabControl target = new TabControl
            {
                Template = TabControlTemplate(),
                ContentTemplate = new FuncDataTemplate<string>((x, _) =>
                    new TextBlock { Tag = "bar", Text = x }),
                Items = new[] { "Foo" },
            };

            ApplyTemplate(target);
            target.ContentPart.UpdateChild();

            var content = Assert.IsType<TextBlock>(target.ContentPart.Child);
            Assert.Equal("bar", content.Tag);
            Assert.Same(target, content.GetLogicalParent());
            Assert.Single(target.GetLogicalChildren(), content);
        }

        private IControlTemplate TabControlTemplate()
        {
            return new FuncControlTemplate<TabControl>((parent, scope) =>
                new StackPanel
                {
                    Children =
                    {
                        new ItemsPresenter
                        {
                            Name = "PART_ItemsPresenter",
                            [!TabStrip.ItemsProperty] = parent[!TabControl.ItemsProperty],
                            [!TabStrip.ItemTemplateProperty] = parent[!TabControl.ItemTemplateProperty],
                        }.RegisterInNameScope(scope),
                        new ContentPresenter
                        {
                            Name = "PART_SelectedContentHost",
                            [!ContentPresenter.ContentProperty] = parent[!TabControl.SelectedContentProperty],
                            [!ContentPresenter.ContentTemplateProperty] = parent[!TabControl.SelectedContentTemplateProperty],
                        }.RegisterInNameScope(scope)
                    }
                });
        }

        private IControlTemplate TabItemTemplate()
        {
            return new FuncControlTemplate<TabItem>((parent, scope) =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = parent[!TabItem.HeaderProperty],
                    [!ContentPresenter.ContentTemplateProperty] = parent[!TabItem.HeaderTemplateProperty]
                }.RegisterInNameScope(scope));
        }

        private void ApplyTemplate(TabControl target)
        {
            target.ApplyTemplate();

            target.Presenter.ApplyTemplate();

            foreach (var tabItem in target.GetLogicalChildren().OfType<TabItem>())
            {
                tabItem.Template = TabItemTemplate();

                tabItem.ApplyTemplate();

                ((ContentPresenter)tabItem.Presenter).UpdateChild();
            }

            target.ContentPart.ApplyTemplate();
        }

        private class Item
        {
            public Item(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }
    }
}
