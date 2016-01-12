// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Perspex.Controls.Presenters;
using Perspex.Controls.Templates;
using Perspex.Input;
using Perspex.LogicalTree;
using Perspex.Styling;
using Perspex.VisualTree;
using Xunit;

namespace Perspex.Controls.UnitTests
{
    public class ListBoxTests
    {
        [Fact]
        public void ListBox_Should_Find_ItemsPresenter_In_ScrollViewer()
        {
            var target = new ListBox
            {
                Template = new FuncControlTemplate(CreateListBoxTemplate),
            };

            ApplyTemplate(target);

            Assert.IsType<ItemsPresenter>(target.Presenter);
        }

        [Fact]
        public void ListBoxItem_Containers_Should_Be_Generated()
        {
            var items = new[] { "Foo", "Bar", "Baz " };
            var target = new ListBox
            {
                Template = new FuncControlTemplate(CreateListBoxTemplate),
                Items = items,
            };

            ApplyTemplate(target);

            var text = target.Presenter.Panel.Children
                .OfType<ListBoxItem>()
                .Do(x => x.Template = ListBoxItemTemplate())
                .Do(x => { x.ApplyTemplate(); ((ContentPresenter)x.Presenter).UpdateChild(); })
                .Select(x => x.Presenter.Child)
                .OfType<TextBlock>()
                .Select(x => x.Text)
                .ToList();

            Assert.Equal(items, text);
        }

        [Fact]
        public void LogicalChildren_Should_Be_Set_For_DataTemplate_Generated_Items()
        {
            var target = new ListBox
            {
                Template = new FuncControlTemplate(CreateListBoxTemplate),
                Items = new[] { "Foo", "Bar", "Baz " },
            };

            ApplyTemplate(target);

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
                Template = new FuncControlTemplate(CreateListBoxTemplate),
                DataContext = "Base",
                DataTemplates = new DataTemplates
                {
                    new FuncDataTemplate<Item>(x => new Button { Content = x })
                },
                Items = items,
            };

            ApplyTemplate(target);

            var dataContexts = target.Presenter.Panel.Children
                .Cast<Control>()
                .Select(x => x.DataContext)
                .ToList();

            Assert.Equal(
                new object[] { items[0], items[1], "Base", "Base" },
                dataContexts);
        }

        [Fact]
        public void Setting_SelectedItem_Should_Set_Panel_Keyboard_Navigation()
        {
            var target = new ListBox
            {
                Template = new FuncControlTemplate(CreateListBoxTemplate),
                Items = new[] { "Foo", "Bar", "Baz " },
            };

            ApplyTemplate(target);

            target.Presenter.Panel.Children[1].RaiseEvent(new PointerPressEventArgs
            {
                RoutedEvent = InputElement.PointerPressedEvent,
                MouseButton = MouseButton.Left,
            });

            var panel = target.Presenter.Panel;

            Assert.Equal(
                KeyboardNavigation.GetTabOnceActiveElement((InputElement)panel),
                panel.Children[1]);
        }

        private Control CreateListBoxTemplate(ITemplatedControl parent)
        {
            return new ScrollViewer
            {
                Template = new FuncControlTemplate(CreateScrollViewerTemplate),
                Content = new ItemsPresenter
                {
                    Name = "PART_ItemsPresenter",
                    [~ItemsPresenter.ItemsProperty] = parent.GetObservable(ItemsControl.ItemsProperty),
                }
            };
        }

        private FuncControlTemplate ListBoxItemTemplate()
        {
            return new FuncControlTemplate<ListBoxItem>(parent => new ContentPresenter
            {
                Name = "PART_ContentPresenter",
                [!ContentPresenter.ContentProperty] = parent[!ListBoxItem.ContentProperty],
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

        private void ApplyTemplate(ListBox target)
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
