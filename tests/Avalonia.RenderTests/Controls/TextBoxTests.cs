using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Skia.RenderTests
{
    public class TextBoxTests : TestBase
    {
        public TextBoxTests()
            : base(@"Controls\TextBox")
        {
        }

        private static IControlTemplate CreateTextBoxTemplate()
        {
            return new FuncControlTemplate<TextBox>((textBox, scope) =>
            {
                var border = new Border
                {
                    Background = textBox.Background,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(4),
                };

                var panel = new Panel();

                var placeholder = new TextBlock
                {
                    Name = "PART_Placeholder",
                    [!TextBlock.TextProperty] = textBox[!TextBox.PlaceholderTextProperty],
                    [!TextBlock.ForegroundProperty] = textBox[!TextBox.PlaceholderForegroundProperty],
                    FontFamily = textBox.FontFamily,
                    FontSize = textBox.FontSize,
                    VerticalAlignment = VerticalAlignment.Center,
                    Opacity = 0.5,
                }.RegisterInNameScope(scope);

                var presenter = new TextPresenter
                {
                    Name = "PART_TextPresenter",
                    [!TextPresenter.TextProperty] = textBox[!TextBox.TextProperty],
                    [!TextPresenter.CaretIndexProperty] = textBox[!TextBox.CaretIndexProperty],
                    FontFamily = textBox.FontFamily,
                    FontSize = textBox.FontSize,
                }.RegisterInNameScope(scope);

                panel.Children.Add(placeholder);
                panel.Children.Add(presenter);
                border.Child = panel;

                return border;
            });
        }

        [Fact]
        public async Task Placeholder_With_Red_Foreground()
        {
            var target = new Border
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 50,
                Background = Brushes.White,
                Child = new TextBox
                {
                    Template = CreateTextBoxTemplate(),
                    FontFamily = TestFontFamily,
                    FontSize = 12,
                    Background = Brushes.White,
                    PlaceholderText = "Red placeholder",
                    PlaceholderForeground = Brushes.Red,
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Placeholder_With_Blue_Foreground()
        {
            var target = new Border
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 50,
                Background = Brushes.White,
                Child = new TextBox
                {
                    Template = CreateTextBoxTemplate(),
                    FontFamily = TestFontFamily,
                    FontSize = 12,
                    Background = Brushes.White,
                    PlaceholderText = "Blue placeholder",
                    PlaceholderForeground = Brushes.Blue,
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Placeholder_With_Default_Foreground()
        {
            var target = new Border
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 50,
                Background = Brushes.White,
                Child = new TextBox
                {
                    Template = CreateTextBoxTemplate(),
                    FontFamily = TestFontFamily,
                    FontSize = 12,
                    Background = Brushes.White,
                    PlaceholderText = "Default placeholder",
                    PlaceholderForeground = Brushes.Gray,
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
    }
}
