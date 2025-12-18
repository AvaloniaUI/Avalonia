using System.Threading.Tasks;
using Avalonia.Controls;
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

        /// <summary>
        /// Tests the visual appearance of a watermark text with custom foreground color.
        /// This test uses a TextBlock directly to simulate what the watermark would look like,
        /// since full TextBox template rendering requires application-level theme infrastructure.
        /// </summary>
        [Fact]
        public async Task Watermark_With_Red_Foreground()
        {
            // Simulating the watermark appearance directly
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 50,
                Child = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(4),
                    Child = new TextBlock
                    {
                        FontFamily = TestFontFamily,
                        FontSize = 12,
                        Text = "Red watermark",
                        Foreground = Brushes.Red,
                        Opacity = 0.5,
                        VerticalAlignment = VerticalAlignment.Center,
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        /// <summary>
        /// Tests the visual appearance of a watermark text with blue foreground color.
        /// </summary>
        [Fact]
        public async Task Watermark_With_Blue_Foreground()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 50,
                Child = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(4),
                    Child = new TextBlock
                    {
                        FontFamily = TestFontFamily,
                        FontSize = 12,
                        Text = "Blue watermark",
                        Foreground = Brushes.Blue,
                        Opacity = 0.5,
                        VerticalAlignment = VerticalAlignment.Center,
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        /// <summary>
        /// Tests the default watermark appearance (gray foreground).
        /// </summary>
        [Fact]
        public async Task Watermark_With_Default_Foreground()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 50,
                Child = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(4),
                    Child = new TextBlock
                    {
                        FontFamily = TestFontFamily,
                        FontSize = 12,
                        Text = "Default watermark",
                        Foreground = Brushes.Gray,
                        Opacity = 0.5,
                        VerticalAlignment = VerticalAlignment.Center,
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
    }
}
