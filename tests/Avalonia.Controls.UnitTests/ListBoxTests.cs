// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
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
                Template = ListBoxTemplate(),
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
                Template = ListBoxTemplate(),
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
                    Template = ListBoxTemplate(),
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
                    Template = ListBoxTemplate(),
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
                    Template = ListBoxTemplate(),
                    DataContext = "Base",
                    DataTemplates =
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

        [Fact]
        public void Selection_Should_Be_Cleared_On_Recycled_Items()
        {
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                Items = Enumerable.Range(0, 20).Select(x => $"Item {x}").ToList(),
                ItemTemplate = new FuncDataTemplate<string>(x => new TextBlock { Height = 10 }),
                SelectedIndex = 0,
            };

            Prepare(target);

            // Make sure we're virtualized and first item is selected.
            Assert.Equal(10, target.Presenter.Panel.Children.Count);
            Assert.True(((ListBoxItem)target.Presenter.Panel.Children[0]).IsSelected);

            // Scroll down a page.
            target.Scroll.Offset = new Vector(0, 10);

            // Make sure recycled item isn't now selected.
            Assert.False(((ListBoxItem)target.Presenter.Panel.Children[0]).IsSelected);
        }

        [Fact]
        public void ScrollViewer_Should_Have_Correct_Extent_And_Viewport()
        {
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                Items = Enumerable.Range(0, 20).Select(x => $"Item {x}").ToList(),
                ItemTemplate = new FuncDataTemplate<string>(x => new TextBlock { Width = 20, Height = 10 }),
                SelectedIndex = 0,
            };

            Prepare(target);

            Assert.Equal(new Size(20, 20), target.Scroll.Extent);
            Assert.Equal(new Size(100, 10), target.Scroll.Viewport);
        }

        [Fact]
        public void Containers_Correct_After_Clear_Add_Remove()
        {
            // Issue #1936
            var items = new AvaloniaList<string>(Enumerable.Range(0, 11).Select(x => $"Item {x}"));
            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                Items = items,
                ItemTemplate = new FuncDataTemplate<string>(x => new TextBlock { Width = 20, Height = 10 }),
                SelectedIndex = 0,
            };

            Prepare(target);

            items.Clear();
            items.AddRange(Enumerable.Range(0, 11).Select(x => $"Item {x}"));
            items.Remove("Item 2");

            Assert.Equal(
                items,
                target.Presenter.Panel.Children.Cast<ListBoxItem>().Select(x => (string)x.Content));
        }


        [Fact]
        public void ListBox_After_Scroll_IndexOutOfRangeException_Shouldnt_Be_Thrown()
        {
            var items = Enumerable.Range(0, 11).Select(x => $"{x}").ToArray();

            var target = new ListBox
            {
                Template = ListBoxTemplate(),
                Items = items,
                ItemTemplate = new FuncDataTemplate<string>(x => new TextBlock { Height = 11 })
            };

            Prepare(target);

            var panel = target.Presenter.Panel as IVirtualizingPanel;

            var listBoxItems = panel.Children.OfType<ListBoxItem>();

            //virtualization should have created exactly 10 items
            Assert.Equal(10, listBoxItems.Count());
            Assert.Equal("0", listBoxItems.First().DataContext);
            Assert.Equal("9", listBoxItems.Last().DataContext);

            //instead pixeloffset > 0 there could be pretty complex sequence for repro
            //it involves add/remove/scroll to end multiple actions
            //which i can't find so far :(, but this is the simplest way to add it to unit test
            panel.PixelOffset = 1;

            //here scroll to end -> IndexOutOfRangeException is thrown
            target.Scroll.Offset = new Vector(0, 2);

            Assert.True(true);
        }

        private FuncControlTemplate ListBoxTemplate()
        {
            return new FuncControlTemplate<ListBox>(parent =>
                new ScrollViewer
                {
                    Name = "PART_ScrollViewer",
                    Template = ScrollViewerTemplate(),
                    Content = new ItemsPresenter
                    {
                        Name = "PART_ItemsPresenter",
                        [~ItemsPresenter.ItemsProperty] = parent.GetObservable(ItemsControl.ItemsProperty).ToBinding(),
                        [~ItemsPresenter.ItemsPanelProperty] = parent.GetObservable(ItemsControl.ItemsPanelProperty).ToBinding(),
                        [~ItemsPresenter.VirtualizationModeProperty] = parent.GetObservable(ListBox.VirtualizationModeProperty).ToBinding(),
                    }
                });
        }

        private FuncControlTemplate ListBoxItemTemplate()
        {
            return new FuncControlTemplate<ListBoxItem>(parent =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = parent[!ListBoxItem.ContentProperty],
                    [!ContentPresenter.ContentTemplateProperty] = parent[!ListBoxItem.ContentTemplateProperty],
                });
        }

        private FuncControlTemplate ScrollViewerTemplate()
        {
            return new FuncControlTemplate<ScrollViewer>(parent =>
                new ScrollContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [~ScrollContentPresenter.ContentProperty] = parent.GetObservable(ScrollViewer.ContentProperty).ToBinding(),
                    [~~ScrollContentPresenter.ExtentProperty] = parent[~~ScrollViewer.ExtentProperty],
                    [~~ScrollContentPresenter.OffsetProperty] = parent[~~ScrollViewer.OffsetProperty],
                    [~~ScrollContentPresenter.ViewportProperty] = parent[~~ScrollViewer.ViewportProperty],
                });
        }

        private void Prepare(ListBox target)
        {
            // The ListBox needs to be part of a rooted visual tree.
            var root = new TestRoot();
            root.Child = target;

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
            foreach (IControl i in target.GetSelfAndVisualDescendants())
            {
                i.InvalidateMeasure();
            }

            target.Measure(new Size(100, 100));
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
