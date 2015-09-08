// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Linq;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Xunit;

namespace Perspex.Controls.UnitTests.Primitives
{
    public class TabStripTests
    {
        [Fact]
        public void First_Tab_Should_Be_Selected_By_Default()
        {
            var target = new TabStrip
            {
                Template = new ControlTemplate<TabStrip>(CreateTabStripTemplate),
                Items = new[]
                {
                    new TabItem
                    {
                        Name = "first"
                    },
                    new TabItem
                    {
                        Name = "second"
                    },
                }
            };

            target.ApplyTemplate();

            Assert.Equal(target.Items.Cast<TabItem>().First(), target.SelectedItem);
            Assert.Equal(target.Items.Cast<TabItem>().First(), target.SelectedTab);
        }

        [Fact]
        public void Setting_SelectedItem_Should_Set_SelectedTab()
        {
            var target = new TabStrip
            {
                Template = new ControlTemplate<TabStrip>(CreateTabStripTemplate),
                Items = new[]
                {
                    new TabItem
                    {
                        Name = "first"
                    },
                    new TabItem
                    {
                        Name = "second"
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
            var target = new TabStrip
            {
                Template = new ControlTemplate<TabStrip>(CreateTabStripTemplate),
                Items = new[]
                {
                    new TabItem
                    {
                        Name = "first"
                    },
                    new TabItem
                    {
                        Name = "second"
                    },
                }
            };

            target.ApplyTemplate();
            target.SelectedTab = target.Items.Cast<TabItem>().ElementAt(1);

            Assert.Same(target.SelectedItem, target.SelectedTab);
        }

        [Fact]
        public void Removing_Selected_Should_Select_Next()
        {
            var list = new ObservableCollection<TabItem>()
                {
                    new TabItem
                    {
                        Name = "first"
                    },
                    new TabItem
                    {
                        Name = "second"
                    },
                    new TabItem
                    {
                        Name = "3rd"
                    },
                };

            var target = new TabStrip
            {
                Template = new ControlTemplate<TabStrip>(CreateTabStripTemplate),
                Items = list
            };

            target.ApplyTemplate();
            target.SelectedTab = list[1];
            Assert.Same(list[1], target.SelectedTab);
            list.RemoveAt(1);

            // Assert for former element [2] now [1] == "3rd"
            Assert.Same(list[1], target.SelectedTab);
            Assert.Same("3rd", target.SelectedTab.Name);
        }

        private Control CreateTabStripTemplate(TabStrip parent)
        {
            return new ItemsPresenter
            {
                Name = "itemsPresenter",
                [~ItemsPresenter.ItemsProperty] = parent[~ItemsControl.ItemsProperty],
            };
        }
    }
}
