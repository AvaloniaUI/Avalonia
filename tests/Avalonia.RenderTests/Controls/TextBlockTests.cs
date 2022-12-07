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
    public class TextBlockTests : TestBase
    {
        public TextBlockTests()
            : base(@"Controls\TextBlock")
        {
        }

        [Win32Fact("Has text")]
        public async Task Wrapping_NoWrap()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new TextBlock
                {
                    FontFamily = new FontFamily("Courier New"),
                    Background = Brushes.Red,
                    FontSize = 12,
                    Foreground = Brushes.Black,
                    Text = "Neque porro quisquam est qui dolorem ipsum quia dolor sit amet, consectetur, adipisci velit",
                    VerticalAlignment = VerticalAlignment.Top,
                    TextWrapping = TextWrapping.NoWrap,
                }
            };

            await RenderToFile(target);
            CompareImages();
        }


        [Win32Fact("Has text")]
        public async Task RestrictedHeight_VerticalAlign()
        {
            Control text(VerticalAlignment verticalAlingnment, bool clip = true, bool restrictHeight = true)
            {
                return new Border()
                {
                    BorderBrush = Brushes.Blue,
                    BorderThickness = new Thickness(1),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Height = restrictHeight ? 20 : double.NaN,
                    Margin = new Thickness(1),
                    Child = new TextBlock
                    {
                        FontFamily = new FontFamily("Courier New"),
                        Background = Brushes.Red,
                        FontSize = 24,
                        Foreground = Brushes.Black,
                        Text = "L",
                        VerticalAlignment = verticalAlingnment,
                        ClipToBounds = clip
                    }
                };
            }
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 180,
                Height = 80,

                Child = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        text(VerticalAlignment.Stretch, restrictHeight: false),
                        text(VerticalAlignment.Center),
                        text(VerticalAlignment.Stretch),
                        text(VerticalAlignment.Top),
                        text(VerticalAlignment.Bottom),
                        text(VerticalAlignment.Center, clip:false),
                        text(VerticalAlignment.Stretch, clip:false),
                        text(VerticalAlignment.Top, clip:false),
                        text(VerticalAlignment.Bottom, clip:false),
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
    }
}
