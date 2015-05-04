// -----------------------------------------------------------------------
// <copyright file="TabControlTests.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests
{
    using System.Linq;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.LogicalTree;
    using Perspex.Styling;
    using Xunit;

    public class TabControlTests
    {
        [Fact]
        public void First_Tab_Should_Be_Selected_By_Default()
        {
            var target = new TabControl
            {
                Template = ControlTemplate.Create<TabControl>(this.CreateTabControlTemplate),
                Items = new[]
                {
                    new TabItem
                    {
                        Id = "first"
                    },
                    new TabItem
                    {
                        Id = "second"
                    },
                }
            };

            target.ApplyTemplate();

            Assert.NotNull(target.SelectedTab);
            Assert.Equal(target.SelectedTab, target.SelectedItem);
        }

        [Fact]
        public void First_Tab_Content_Should_Be_Displayed_By_Default()
        {
            var target = new TabControl
            {
                Template = ControlTemplate.Create<TabControl>(this.CreateTabControlTemplate),
                Items = new[]
                {
                    new TabItem
                    {
                        Content = new TextBlock(),
                    },
                    new TabItem
                    {
                        Content = new Border(),
                    },
                }
            };

            target.ApplyTemplate();

            Assert.IsType<TextBlock>(target.SelectedContent);
        }

        [Fact]
        public void Setting_SelectedItem_Should_Set_SelectedTab()
        {
            var target = new TabControl
            {
                Template = ControlTemplate.Create<TabControl>(this.CreateTabControlTemplate),
                Items = new[]
                {
                    new TabItem
                    {
                        Id = "first"
                    },
                    new TabItem
                    {
                        Id = "second"
                    },
                }
            };

            target.ApplyTemplate();
            target.SelectedItem = target.Items.Cast<TabItem>().ElementAt(1);

            Assert.Same(target.SelectedTab, target.SelectedItem);
        }

        [Fact]
        public void Setting_SelectedTab_Should_Set_SelectedItem()
        {
            var target = new TabControl
            {
                Template = ControlTemplate.Create<TabControl>(this.CreateTabControlTemplate),
                Items = new[]
                {
                    new TabItem
                    {
                        Id = "first"
                    },
                    new TabItem
                    {
                        Id = "second"
                    },
                }
            };

            target.ApplyTemplate();
            target.SelectedTab = target.Items.Cast<TabItem>().ElementAt(1);

            Assert.Same(target.SelectedItem, target.SelectedTab);
        }

        [Fact]
        public void Logical_Child_Should_Be_SelectedContent()
        {
            var target = new TabControl
            {
                Template = ControlTemplate.Create<TabControl>(this.CreateTabControlTemplate),
                Items = new[]
                {
                    new TabItem
                    {
                        Content = new TextBlock { Id = "Foo" }
                    },
                    new TabItem
                    {
                        Content = new TextBlock { Id = "Foo" }
                    },
                },
            };

            target.ApplyTemplate();

            Assert.Equal(1, target.GetLogicalChildren().Count());
            Assert.Equal("Foo", ((TextBlock)target.GetLogicalChildren().First()).Id);
        }

        private Control CreateTabControlTemplate(TabControl parent)
        {
            return new StackPanel
            {
                Children = new Controls
                {
                    new TabStrip
                    {
                        Id = "tabStrip",
                        Template = ControlTemplate.Create<TabStrip>(this.CreateTabStripTemplate),
                        [~TabStrip.ItemsProperty] = parent[~TabControl.ItemsProperty],
                        [~~TabStrip.SelectedTabProperty] = parent[~~TabControl.SelectedTabProperty]
                    },
                    new Deck
                    {
                        Id = "deck",
                        [~Deck.ItemsProperty] = parent[~TabControl.ItemsProperty],
                        [!Deck.SelectedItemProperty] = parent[!TabControl.SelectedItemProperty],
                    }
                }
            };
        } 

        private Control CreateTabStripTemplate(TabStrip parent)
        {
            return new ItemsPresenter
            {
                Id = "itemsPresenter",
                [~ItemsPresenter.ItemsProperty] = parent[~TabStrip.ItemsProperty],
            };
        }
    }
}
