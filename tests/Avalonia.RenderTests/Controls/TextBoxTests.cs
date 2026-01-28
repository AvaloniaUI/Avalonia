using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Controls
#endif
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

                var watermark = new TextBlock
                {
                    Name = "PART_Watermark",
                    [!TextBlock.TextProperty] = textBox[!TextBox.WatermarkProperty],
                    [!TextBlock.ForegroundProperty] = textBox[!TextBox.WatermarkForegroundProperty],
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

                panel.Children.Add(watermark);
                panel.Children.Add(presenter);
                border.Child = panel;

                return border;
            });
        }

        [Fact]
        public async Task Watermark_With_Red_Foreground()
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
                    Watermark = "Red watermark",
                    WatermarkForeground = Brushes.Red,
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Watermark_With_Blue_Foreground()
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
                    Watermark = "Blue watermark",
                    WatermarkForeground = Brushes.Blue,
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Watermark_With_Default_Foreground()
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
                    Watermark = "Default watermark",
                    WatermarkForeground = Brushes.Gray,
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
    }
}
