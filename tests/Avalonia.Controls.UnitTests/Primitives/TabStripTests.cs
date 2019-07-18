// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Linq;
using Moq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class TabStripTests
    {
        [Fact]
        public void First_Tab_Should_Be_Selected_By_Default()
        {
            var items = new[]
            {
                new TabItem
                {
                    Name = "first"
                },
                new TabItem
                {
                    Name = "second"
                },
            };

            var target = new TabStrip
            {
                Template = new FuncControlTemplate<TabStrip>(CreateTabStripTemplate),
                Items = items,
            };

            target.ApplyTemplate();

            Assert.Equal(0, target.SelectedIndex);
            Assert.Same(items[0], target.SelectedItem);
        }

        [Fact]
        public void Setting_SelectedItem_Should_Set_Selection()
        {
            var items = new[]
            {
                new TabItem
                {
                    Name = "first"
                },
                new TabItem
                {
                    Name = "second"
                },
            };

            var target = new TabStrip
            {
                Template = new FuncControlTemplate<TabStrip>(CreateTabStripTemplate),
                Items = items,
                SelectedItem = items[1],
            };

            target.ApplyTemplate();

            Assert.Equal(1, target.SelectedIndex);
            Assert.Same(items[1], target.SelectedItem);
        }

        [Fact]
        public void Removing_Selected_Should_Select_Next()
        {
            var items = new ObservableCollection<TabItem>()
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
                Template = new FuncControlTemplate<TabStrip>(CreateTabStripTemplate),
                Items = items
            };

            target.ApplyTemplate();
            target.SelectedItem = items[1];
            Assert.Same(items[1], target.SelectedItem);
            items.RemoveAt(1);

            // Assert for former element [2] now [1] == "3rd"
            Assert.Equal(1, target.SelectedIndex);
            Assert.Same(items[1], target.SelectedItem);
            Assert.Same("3rd", ((TabItem)target.SelectedItem).Name);
        }

        private Control CreateTabStripTemplate(TabStrip parent, INameScope scope)
        {
            return new ItemsPresenter
            {
                Name = "itemsPresenter",
                [!ItemsPresenter.ItemsProperty] = parent[!ItemsControl.ItemsProperty],
            }.RegisterInNameScope(scope);
        }
    }
}
