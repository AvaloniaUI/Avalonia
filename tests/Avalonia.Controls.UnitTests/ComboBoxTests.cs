using System.Linq;
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
            _helper.Up(target);

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
            var root = new TestRoot(target);

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

        [Fact]
        public void Detaching_Closed_ComboBox_Keeps_Current_Focus()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new ComboBox
                {
                    Items = new[] { new Canvas() },
                    SelectedIndex = 0,
                    Template = GetTemplate(),
                };

                var other = new Control { Focusable = true };

                StackPanel panel;

                var root = new TestRoot { Child = panel = new StackPanel { Children = { target, other } } };

                target.ApplyTemplate();
                target.Presenter.ApplyTemplate();

                other.Focus();

                Assert.True(other.IsFocused);

                panel.Children.Remove(target);

                Assert.True(other.IsFocused);
            }
        }

        [Theory]
        [InlineData(-1, 2, "c", "A item", "B item", "C item")]
        [InlineData(0, 1, "b", "A item", "B item", "C item")]
        [InlineData(2, 2, "x", "A item", "B item", "C item")]
        public void TextSearch_Should_Have_Expected_SelectedIndex(
            int initialSelectedIndex,
            int expectedSelectedIndex,
            string searchTerm,
            params string[] items)
        {
            using (UnitTestApplication.Start(TestServices.MockThreadingInterface))
            {
                var target = new ComboBox
                {
                    Template = GetTemplate(),                    
                    Items = items.Select(x => new ComboBoxItem { Content = x })
                };

                target.ApplyTemplate();
                target.Presenter.ApplyTemplate();
                target.SelectedIndex = initialSelectedIndex;

                var args = new TextInputEventArgs
                {
                    Text = searchTerm,
                    RoutedEvent = InputElement.TextInputEvent
                };

                target.RaiseEvent(args);

                Assert.Equal(expectedSelectedIndex, target.SelectedIndex);
            }
        }
    }
}
