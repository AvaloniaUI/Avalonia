// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Linq;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.LogicalTree;
using Xunit;

namespace Perspex.Controls.UnitTests
{
    public class TabControlTests
    {
        [Fact]
        public void First_Tab_Should_Be_Selected_By_Default()
        {
            TabItem selected;
            var target = new TabControl
            {
                Template = new ControlTemplate<TabControl>(CreateTabControlTemplate),
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

            Assert.Equal(selected, target.SelectedItem);
            Assert.Equal(selected, target.SelectedTab);
        }

        [Fact]
        public void Setting_SelectedItem_Should_Set_SelectedTab()
        {
            var target = new TabControl
            {
                Template = new ControlTemplate<TabControl>(CreateTabControlTemplate),
                Items = new[]
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
                }
            };

            target.ApplyTemplate();
            target.SelectedItem = target.Items.Cast<TabItem>().ElementAt(1);

            Assert.Same(target.SelectedTab, target.SelectedItem);
        }

        [Fact]
        public void Logical_Child_Should_Be_Selected_Tab_Content()
        {
            var target = new TabControl
            {
                Template = new ControlTemplate<TabControl>(CreateTabControlTemplate),
                Items = new[]
                {
                    new TabItem
                    {
                        Content = "foo"
                    },
                    new TabItem
                    {
                        Content = "bar"
                    },
                },
            };

            target.ApplyTemplate();

            Assert.Equal(1, target.GetLogicalChildren().Count());
            Assert.Equal("foo", ((TextBlock)target.GetLogicalChildren().First()).Text);
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
                Template = new ControlTemplate<TabControl>(CreateTabControlTemplate),
                Items = collection,
            };

            target.ApplyTemplate();
            target.SelectedItem = collection[1];
            collection.RemoveAt(1);

            // compare with former [2] now [1] == "3rd"
            Assert.Same(collection[1], target.SelectedItem);
            Assert.Same(target.SelectedTab, target.SelectedItem);
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
                Template = new ControlTemplate<TabControl>(CreateTabControlTemplate),
                DataContext = "Base",
                DataTemplates = new DataTemplates
                {
                    new DataTemplate<Item>(x => new Button { Content = x })
                },
                Items = items,
            };

            target.ApplyTemplate();

            var dataContext = ((TextBlock)target.GetLogicalChildren().Single()).DataContext;
            Assert.Equal(items[0], dataContext);

            target.SelectedIndex = 1;
            dataContext = ((Button)target.GetLogicalChildren().Single()).DataContext;
            Assert.Equal(items[1], dataContext);

            target.SelectedIndex = 2;
            dataContext = ((TextBlock)target.GetLogicalChildren().Single()).DataContext;
            Assert.Equal("Base", dataContext);

            target.SelectedIndex = 3;
            dataContext = ((TextBlock)target.GetLogicalChildren().Single()).DataContext;
            Assert.Equal("Qux", dataContext);

            target.SelectedIndex = 4;
            dataContext = ((TextBlock)target.GetLogicalChildren().Single()).DataContext;
            Assert.Equal("Base", dataContext);
        }

        private Control CreateTabControlTemplate(TabControl parent)
        {
            return new StackPanel
            {
                Children = new Controls
                {
                    new TabStrip
                    {
                        Name = "tabStrip",
                        Template = new ControlTemplate<TabStrip>(CreateTabStripTemplate),
                        [!ItemsControl.ItemsProperty] = parent[!ItemsControl.ItemsProperty],
                        [!!TabStrip.SelectedTabProperty] = parent[!!TabControl.SelectedTabProperty]
                    },
                    new Deck
                    {
                        Name = "deck",
                        Template = new ControlTemplate<Deck>(CreateDeckTemplate),
                        MemberSelector = parent.ContentSelector,
                        [!ItemsControl.ItemsProperty] = parent[!ItemsControl.ItemsProperty],
                        [!SelectingItemsControl.SelectedItemProperty] = parent[!SelectingItemsControl.SelectedItemProperty],
                    }
                }
            };
        }

        private Control CreateTabStripTemplate(TabStrip parent)
        {
            return new ItemsPresenter
            {
                Name = "itemsPresenter",
                [~ItemsPresenter.ItemsProperty] = parent[~ItemsControl.ItemsProperty],
            };
        }

        private Control CreateDeckTemplate(Deck control)
        {
            return new DeckPresenter
            {
                Name = "itemsPresenter",
                [!DeckPresenter.ItemsProperty] = control[!ItemsControl.ItemsProperty],
                [!DeckPresenter.ItemsPanelProperty] = control[!ItemsControl.ItemsPanelProperty],
                [!DeckPresenter.MemberSelectorProperty] = control[!ItemsControl.MemberSelectorProperty],
                [!DeckPresenter.SelectedIndexProperty] = control[!SelectingItemsControl.SelectedIndexProperty],
                [~DeckPresenter.TransitionProperty] = control[~Deck.TransitionProperty],
            };
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
