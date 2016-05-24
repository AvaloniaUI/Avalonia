// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ListBoxTests
    {
        [Fact]
        public void Should_Use_ItemTemplate_To_Create_Item_Content()
        {
            var target = new ListBox
            {
                Template = CreateListBoxTemplate(),
                Items = new[] { "Foo" },
                ItemTemplate = new FuncDataTemplate<string>(_ => new Canvas()),
            };

            Prepare(target);

            var container = (ListBoxItem)target.Presenter.Panel.Children[0];
            Assert.IsType<Canvas>(container.Presenter.Child);
        }

        [Fact]
        public void ListBox_Should_Find_ItemsPresenter_In_ScrollViewer()
        {
            var target = new ListBox
            {
                Template = CreateListBoxTemplate(),
            };

            Prepare(target);

            Assert.IsType<ItemsPresenter>(target.Presenter);
        }

        [Fact]
        public void ListBoxItem_Containers_Should_Be_Generated()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var items = new[] { "Foo", "Bar", "Baz " };
                var target = new ListBox
                {
                    Template = CreateListBoxTemplate(),
                    Items = items,
                };

                Prepare(target);

                var text = target.Presenter.Panel.Children
                    .OfType<ListBoxItem>()
                    .Select(x => x.Presenter.Child)
                    .OfType<TextBlock>()
                    .Select(x => x.Text)
                    .ToList();

                Assert.Equal(items, text);
            }
        }

        [Fact]
        public void LogicalChildren_Should_Be_Set_For_DataTemplate_Generated_Items()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var target = new ListBox
                {
                    Template = CreateListBoxTemplate(),
                    Items = new[] { "Foo", "Bar", "Baz " },
                };

                Prepare(target);

                Assert.Equal(3, target.GetLogicalChildren().Count());

                foreach (var child in target.GetLogicalChildren())
                {
                    Assert.IsType<ListBoxItem>(child);
                }
            }
        }

        [Fact]
        public void DataContexts_Should_Be_Correctly_Set()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
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
                    Template = CreateListBoxTemplate(),
                    DataContext = "Base",
                    DataTemplates = new DataTemplates
                {
                    new FuncDataTemplate<Item>(x => new Button { Content = x })
                },
                    Items = items,
                };

                Prepare(target);

                var dataContexts = target.Presenter.Panel.Children
                    .Cast<Control>()
                    .Select(x => x.DataContext)
                    .ToList();

                Assert.Equal(
                    new object[] { items[0], items[1], "Base", "Base" },
                    dataContexts);
            }
        }

        private FuncControlTemplate CreateListBoxTemplate()
        {
            return new FuncControlTemplate<ListBox>(parent => 
                new ScrollViewer
                {
                    Name = "PART_ScrollViewer",
                    Template = new FuncControlTemplate(CreateScrollViewerTemplate),
                    Content = new ItemsPresenter
                    {
                        Name = "PART_ItemsPresenter",
                        [~ItemsPresenter.ItemsProperty] = parent.GetObservable(ItemsControl.ItemsProperty),
                        [~ItemsPresenter.ItemsPanelProperty] = parent.GetObservable(ItemsControl.ItemsPanelProperty),
                    }
                });
        }

        private FuncControlTemplate ListBoxItemTemplate()
        {
            return new FuncControlTemplate<ListBoxItem>(parent => new ContentPresenter
            {
                Name = "PART_ContentPresenter",
                [!ContentPresenter.ContentProperty] = parent[!ListBoxItem.ContentProperty],
                [!ContentPresenter.ContentTemplateProperty] = parent[!ListBoxItem.ContentTemplateProperty],
            });
        }

        private Control CreateScrollViewerTemplate(ITemplatedControl parent)
        {
            return new ScrollContentPresenter
            {
                Name = "PART_ContentPresenter",
                [~ContentPresenter.ContentProperty] = parent.GetObservable(ContentControl.ContentProperty),
            };
        }

        private void Prepare(ListBox target)
        {
            // Apply the template to the ListBox itself.
            target.ApplyTemplate();

            // Then to its inner ScrollViewer.
            var scrollViewer = (ScrollViewer)target.GetVisualChildren().Single();
            scrollViewer.ApplyTemplate();

            // Then make the ScrollViewer create its child.
            ((ContentPresenter)scrollViewer.Presenter).UpdateChild();

            // Now the ItemsPresenter should be reigstered, so apply its template.
            target.Presenter.ApplyTemplate();

            // Because ListBox items are virtualized we need to do a layout to make them appear.
            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            // Now set and apply the item templates.
            foreach (ListBoxItem item in target.Presenter.Panel.Children)
            {
                item.Template = ListBoxItemTemplate();
                item.ApplyTemplate();
                item.Presenter.ApplyTemplate();
                ((ContentPresenter)item.Presenter).UpdateChild();
            }

            // The items were created before the template was applied, so now we need to go back
            // and re-arrange everything.
            foreach (IControl i in target.GetSelfAndVisualDescendents())
            {
                i.InvalidateArrange();
            }

            target.Arrange(new Rect(0, 0, 100, 100));
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
