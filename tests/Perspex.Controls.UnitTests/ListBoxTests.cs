// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Perspex.Controls.Presenters;
using Perspex.Controls.Templates;
using Perspex.LogicalTree;
using Perspex.Styling;
using Xunit;

namespace Perspex.Controls.UnitTests
{
    public class ListBoxTests
    {
        [Fact]
        public void LogicalChildren_Should_Be_Set()
        {
            var target = new ListBox
            {
                Template = new ControlTemplate(CreateListBoxTemplate),
                Items = new[] { "Foo", "Bar", "Baz " },
            };

            target.ApplyTemplate();

            Assert.Equal(3, target.GetLogicalChildren().Count());

            foreach (var child in target.GetLogicalChildren())
            {
                Assert.IsType<ListBoxItem>(child);
            }
        }

        [Fact]
        public void DataContexts_Should_Be_Correctly_Set()
        {
            var items = new object[]
            {
                "Foo",
                new Item("Bar"),
                new TextBlock { Text = "Baz" },
                new ListBoxItem { Content = "Qux" },
            };

            var target = new ListBox
            {
                Template = new ControlTemplate(CreateListBoxTemplate),
                DataContext = "Base",
                DataTemplates = new DataTemplates
                {
                    new FuncDataTemplate<Item>(x => new Button { Content = x })
                },
                Items = items,
            };

            target.ApplyTemplate();

            var dataContexts = target.Presenter.Panel.Children
                .Cast<Control>()
                .Select(x => x.DataContext)
                .ToList();

            Assert.Equal(
                new object[] { items[0], items[1], "Base", "Base" },
                dataContexts);
        }

        private Control CreateListBoxTemplate(ITemplatedControl parent)
        {
            return new ScrollViewer
            {
                Template = new ControlTemplate(CreateScrollViewerTemplate),
                Content = new ItemsPresenter
                {
                    Name = "itemsPresenter",
                    [~ItemsPresenter.ItemsProperty] = parent.GetObservable(ItemsControl.ItemsProperty),
                }
            };
        }

        private Control CreateScrollViewerTemplate(ITemplatedControl parent)
        {
            return new ScrollContentPresenter
            {
                [~ContentPresenter.ContentProperty] = parent.GetObservable(ContentControl.ContentProperty),
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
