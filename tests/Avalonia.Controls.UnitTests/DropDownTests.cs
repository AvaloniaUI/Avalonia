// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class DropDownTests
    {
        [Fact]
        public void Clicking_On_Control_Toggles_IsDropDownOpen()
        {
            var target = new DropDown
            {
                Items = new[] { "Foo", "Bar" },
            };

            target.RaiseEvent(new PointerPressedEventArgs
            {
                RoutedEvent = InputElement.PointerPressedEvent,
            });

            Assert.True(target.IsDropDownOpen);

            target.RaiseEvent(new PointerPressedEventArgs
            {
                RoutedEvent = InputElement.PointerPressedEvent,
            });

            Assert.False(target.IsDropDownOpen);
        }

        [Fact]
        public void SelectionBoxItem_Is_Rectangle_With_VisualBrush_When_Selection_Is_Control()
        {
            var items = new[] { new Canvas() };
            var target = new DropDown
            {
                Items = items,
                SelectedIndex = 0,
            };

            var rectangle = target.GetValue(DropDown.SelectionBoxItemProperty) as Rectangle;
            Assert.NotNull(rectangle);

            var brush = rectangle.Fill as VisualBrush;
            Assert.NotNull(brush);
            Assert.Same(items[0], brush.Visual);
        }

        [Fact]
        public void SelectionBoxItem_Rectangle_Is_Removed_From_Logical_Tree()
        {
            var target = new DropDown
            {
                Items = new[] { new Canvas() },
                SelectedIndex = 0,
                Template = GetTemplate(),
            };

            var root = new TestRoot { Child = target };
            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            var rectangle = target.GetValue(DropDown.SelectionBoxItemProperty) as Rectangle;
            Assert.True(((ILogical)target).IsAttachedToLogicalTree);
            Assert.True(((ILogical)rectangle).IsAttachedToLogicalTree);

            rectangle.DetachedFromLogicalTree += (s, e) => { };

            root.Child = null;

            Assert.False(((ILogical)target).IsAttachedToLogicalTree);
            Assert.False(((ILogical)rectangle).IsAttachedToLogicalTree);
        }

        private FuncControlTemplate GetTemplate()
        {
            return new FuncControlTemplate<DropDown>(parent =>
            {
                return new Panel
                {
                    Name = "container",
                    Children =
                    {
                        new ContentControl
                        {
                            [!ContentControl.ContentProperty] = parent[!DropDown.SelectionBoxItemProperty],
                        },
                        new ToggleButton
                        {
                            Name = "toggle",
                        },
                        new Popup
                        {
                            Name = "PART_Popup",
                            Child = new ItemsPresenter
                            {
                                Name = "PART_ItemsPresenter",
                                [!ItemsPresenter.ItemsProperty] = parent[!DropDown.ItemsProperty],
                            }
                        }
                    }
                };
            });
        }
    }
}
