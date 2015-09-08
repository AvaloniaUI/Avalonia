





namespace Perspex.Controls.UnitTests
{
    using System.Collections.ObjectModel;
    using System.Linq;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Templates;
    using Perspex.LogicalTree;
    using Xunit;

    public class TabControlTests
    {
        [Fact]
        public void First_Tab_Should_Be_Selected_By_Default()
        {
            TabItem selected;
            var target = new TabControl
            {
                Template = new ControlTemplate<TabControl>(this.CreateTabControlTemplate),
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
                Template = new ControlTemplate<TabControl>(this.CreateTabControlTemplate),
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
                Template = new ControlTemplate<TabControl>(this.CreateTabControlTemplate),
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
                Template = new ControlTemplate<TabControl>(this.CreateTabControlTemplate),
                Items = collection,
            };

            target.ApplyTemplate();
            target.SelectedItem = collection[1];
            collection.RemoveAt(1);

            // compare with former [2] now [1] == "3rd"
            Assert.Same(collection[1], target.SelectedItem);
            Assert.Same(target.SelectedTab, target.SelectedItem);
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
                        Template = new ControlTemplate<TabStrip>(this.CreateTabStripTemplate),
                        [!TabStrip.ItemsProperty] = parent[!TabControl.ItemsProperty],
                        [!!TabStrip.SelectedTabProperty] = parent[!!TabControl.SelectedTabProperty]
                    },
                    new Deck
                    {
                        Name = "deck",
                        Template = new ControlTemplate<Deck>(this.CreateDeckTemplate),
                        DataTemplates = new DataTemplates
                        {
                            new DataTemplate<TabItem>(x => (Control)parent.MaterializeDataTemplate(x.Content)),
                        },
                        [!Deck.ItemsProperty] = parent[!TabControl.ItemsProperty],
                        [!Deck.SelectedItemProperty] = parent[!TabControl.SelectedItemProperty],
                    }
                }
            };
        }

        private Control CreateTabStripTemplate(TabStrip parent)
        {
            return new ItemsPresenter
            {
                Name = "itemsPresenter",
                [~ItemsPresenter.ItemsProperty] = parent[~TabStrip.ItemsProperty],
            };
        }

        private Control CreateDeckTemplate(Deck control)
        {
            return new DeckPresenter
            {
                Name = "itemsPresenter",
                [!ItemsPresenter.ItemsProperty] = control[!Deck.ItemsProperty],
                [!ItemsPresenter.ItemsPanelProperty] = control[!Deck.ItemsPanelProperty],
                [!DeckPresenter.SelectedIndexProperty] = control[!Deck.SelectedIndexProperty],
                [~DeckPresenter.TransitionProperty] = control[~Deck.TransitionProperty],
            };
        }
    }
}
