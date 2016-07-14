// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ListBoxTests_Single
    {
        [Fact]
        public void Focusing_Item_With_Tab_Should_Not_Select_It()
        {
            var target = new ListBox
            {
                Template = new FuncControlTemplate(CreateListBoxTemplate),
                Items = new[] { "Foo", "Bar", "Baz " },
            };

            ApplyTemplate(target);

            target.Presenter.Panel.Children[0].RaiseEvent(new GotFocusEventArgs
            {
                RoutedEvent = InputElement.GotFocusEvent,
                NavigationMethod = NavigationMethod.Tab,
            });

            Assert.Equal(-1, target.SelectedIndex);
        }

        [Fact]
        public void Focusing_Item_With_Arrow_Key_Should_Select_It()
        {
            var target = new ListBox
            {
                Template = new FuncControlTemplate(CreateListBoxTemplate),
                Items = new[] { "Foo", "Bar", "Baz " },
            };

            ApplyTemplate(target);

            target.Presenter.Panel.Children[0].RaiseEvent(new GotFocusEventArgs
            {
                RoutedEvent = InputElement.GotFocusEvent,
                NavigationMethod = NavigationMethod.Directional,
            });

            Assert.Equal(0, target.SelectedIndex);
        }

        [Fact]
        public void Clicking_Item_Should_Select_It()
        {
            var target = new ListBox
            {
                Template = new FuncControlTemplate(CreateListBoxTemplate),
                Items = new[] { "Foo", "Bar", "Baz " },
            };

            ApplyTemplate(target);

            target.Presenter.Panel.Children[0].RaiseEvent(new PointerPressedEventArgs
            {
                RoutedEvent = InputElement.PointerPressedEvent,
                MouseButton = MouseButton.Left,
            });

            Assert.Equal(0, target.SelectedIndex);
        }

        [Fact]
        public void Clicking_Selected_Item_Should_Not_Deselect_It()
        {
            var target = new ListBox
            {
                Template = new FuncControlTemplate(CreateListBoxTemplate),
                Items = new[] { "Foo", "Bar", "Baz " },
            };

            ApplyTemplate(target);
            target.SelectedIndex = 0;

            target.Presenter.Panel.Children[0].RaiseEvent(new PointerPressedEventArgs
            {
                RoutedEvent = InputElement.PointerPressedEvent,
                MouseButton = MouseButton.Left,
            });

            Assert.Equal(0, target.SelectedIndex);
        }

        [Fact]
        public void Clicking_Item_Should_Select_It_When_SelectionMode_Toggle()
        {
            var target = new ListBox
            {
                Template = new FuncControlTemplate(CreateListBoxTemplate),
                Items = new[] { "Foo", "Bar", "Baz " },
                SelectionMode = SelectionMode.Single | SelectionMode.Toggle,
            };

            ApplyTemplate(target);

            target.Presenter.Panel.Children[0].RaiseEvent(new PointerPressedEventArgs
            {
                RoutedEvent = InputElement.PointerPressedEvent,
                MouseButton = MouseButton.Left,
            });

            Assert.Equal(0, target.SelectedIndex);
        }

        [Fact]
        public void Clicking_Selected_Item_Should_Deselect_It_When_SelectionMode_Toggle()
        {
            var target = new ListBox
            {
                Template = new FuncControlTemplate(CreateListBoxTemplate),
                Items = new[] { "Foo", "Bar", "Baz " },
                SelectionMode = SelectionMode.Toggle,
            };

            ApplyTemplate(target);
            target.SelectedIndex = 0;

            target.Presenter.Panel.Children[0].RaiseEvent(new PointerPressedEventArgs
            {
                RoutedEvent = InputElement.PointerPressedEvent,
                MouseButton = MouseButton.Left,
            });

            Assert.Equal(-1, target.SelectedIndex);
        }

        [Fact]
        public void Clicking_Selected_Item_Should_Not_Deselect_It_When_SelectionMode_ToggleAlwaysSelected()
        {
            var target = new ListBox
            {
                Template = new FuncControlTemplate(CreateListBoxTemplate),
                Items = new[] { "Foo", "Bar", "Baz " },
                SelectionMode = SelectionMode.Toggle | SelectionMode.AlwaysSelected,
            };

            ApplyTemplate(target);
            target.SelectedIndex = 0;

            target.Presenter.Panel.Children[0].RaiseEvent(new PointerPressedEventArgs
            {
                RoutedEvent = InputElement.PointerPressedEvent,
                MouseButton = MouseButton.Left,
            });

            Assert.Equal(0, target.SelectedIndex);
        }

        [Fact]
        public void Clicking_Another_Item_Should_Select_It_When_SelectionMode_Toggle()
        {
            var target = new ListBox
            {
                Template = new FuncControlTemplate(CreateListBoxTemplate),
                Items = new[] { "Foo", "Bar", "Baz " },
                SelectionMode = SelectionMode.Single | SelectionMode.Toggle,
            };

            ApplyTemplate(target);
            target.SelectedIndex = 1;

            target.Presenter.Panel.Children[0].RaiseEvent(new PointerPressedEventArgs
            {
                RoutedEvent = InputElement.PointerPressedEvent,
                MouseButton = MouseButton.Left,
            });

            Assert.Equal(0, target.SelectedIndex);
        }

        [Fact]
        public void Setting_Item_IsSelected_Sets_ListBox_Selection()
        {
            var target = new ListBox
            {
                Template = new FuncControlTemplate(CreateListBoxTemplate),
                Items = new[] { "Foo", "Bar", "Baz " },
            };

            ApplyTemplate(target);

            ((ListBoxItem)target.GetLogicalChildren().ElementAt(1)).IsSelected = true;

            Assert.Equal("Bar", target.SelectedItem);
            Assert.Equal(1, target.SelectedIndex);
        }

        private Control CreateListBoxTemplate(ITemplatedControl parent)
        {
            return new ScrollViewer
            {
                Template = new FuncControlTemplate(CreateScrollViewerTemplate),
                Content = new ItemsPresenter
                {
                    Name = "PART_ItemsPresenter",
                    [~ItemsPresenter.ItemsProperty] = parent.GetObservable(ItemsControl.ItemsProperty).AsBinding(),
                }
            };
        }

        private Control CreateScrollViewerTemplate(ITemplatedControl parent)
        {
            return new ScrollContentPresenter
            {
                Name = "PART_ContentPresenter",
                [~ContentPresenter.ContentProperty] = parent.GetObservable(ContentControl.ContentProperty).AsBinding(),
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
    }
}
