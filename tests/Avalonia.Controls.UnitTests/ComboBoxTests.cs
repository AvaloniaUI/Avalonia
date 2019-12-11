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
    public class ComboBoxTests
    {
        MouseTestHelper _helper = new MouseTestHelper();
        
        [Fact]
        public void Clicking_On_Control_Toggles_IsDropDownOpen()
        {
            var target = new ComboBox
            {
                Items = new[] { "Foo", "Bar" },
            };

            _helper.Down(target);
            _helper.Up(target);
            Assert.True(target.IsDropDownOpen);

            _helper.Down(target);

            Assert.False(target.IsDropDownOpen);
        }

        [Fact]
        public void SelectionBoxItem_Is_Rectangle_With_VisualBrush_When_Selection_Is_Control()
        {
            var items = new[] { new Canvas() };
            var target = new ComboBox
            {
                Items = items,
                SelectedIndex = 0,
            };

            var rectangle = target.GetValue(ComboBox.SelectionBoxItemProperty) as Rectangle;
            Assert.NotNull(rectangle);

            var brush = rectangle.Fill as VisualBrush;
            Assert.NotNull(brush);
            Assert.Same(items[0], brush.Visual);
        }

        [Fact]
        public void SelectionBoxItem_Rectangle_Is_Removed_From_Logical_Tree()
        {
            var target = new ComboBox
            {
                Items = new[] { new Canvas() },
                SelectedIndex = 0,
                Template = GetTemplate(),
            };

            var root = new TestRoot { Child = target };
            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            var rectangle = target.GetValue(ComboBox.SelectionBoxItemProperty) as Rectangle;
            Assert.True(((ILogical)target).IsAttachedToLogicalTree);
            Assert.True(((ILogical)rectangle).IsAttachedToLogicalTree);

            rectangle.DetachedFromLogicalTree += (s, e) => { };

            root.Child = null;

            Assert.False(((ILogical)target).IsAttachedToLogicalTree);
            Assert.False(((ILogical)rectangle).IsAttachedToLogicalTree);
        }

        private FuncControlTemplate GetTemplate()
        {
            return new FuncControlTemplate<ComboBox>((parent, scope) =>
            {
                return new Panel
                {
                    Name = "container",
                    Children =
                    {
                        new ContentControl
                        {
                            [!ContentControl.ContentProperty] = parent[!ComboBox.SelectionBoxItemProperty],
                        },
                        new ToggleButton
                        {
                            Name = "toggle",
                        }.RegisterInNameScope(scope),
                        new Popup
                        {
                            Name = "PART_Popup",
                            Child = new ItemsPresenter
                            {
                                Name = "PART_ItemsPresenter",
                                [!ItemsPresenter.ItemsProperty] = parent[!ComboBox.ItemsProperty],
                            }.RegisterInNameScope(scope)
                        }.RegisterInNameScope(scope)
                    }
                };
            });
        }
    }
}
