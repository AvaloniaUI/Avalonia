// -----------------------------------------------------------------------
// <copyright file="TabStripTests.cs" company="Steven Kirk">
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

    public class TabStripTests
    {
        [Fact]
        public void First_Tab_Should_Be_Selected_By_Default()
        {
            var target = new TabStrip
            {
                Template = ControlTemplate.Create<TabStrip>(this.CreateTabStripTemplate),
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

            Assert.Equal(target.Items.Cast<TabItem>().First(), target.SelectedItem);
            Assert.Equal(target.Items.Cast<TabItem>().First(), target.SelectedTab);
        }

        [Fact]
        public void Setting_SelectedItem_Should_Set_SelectedTab()
        {
            var target = new TabStrip
            {
                Template = ControlTemplate.Create<TabStrip>(this.CreateTabStripTemplate),
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
            var target = new TabStrip
            {
                Template = ControlTemplate.Create<TabStrip>(this.CreateTabStripTemplate),
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
