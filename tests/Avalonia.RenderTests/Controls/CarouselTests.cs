using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.UnitTests;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Controls
#endif
{
    public class CarouselRenderTests : TestBase
    {
        public CarouselRenderTests()
            : base(@"Controls\Carousel")
        {
        }

        private static Style FontStyle => new Style(x => x.OfType<TextBlock>())
        {
            Setters = { new Setter(TextBlock.FontFamilyProperty, TestFontFamily) }
        };

        [Fact]
        public async Task Carousel_ViewportFraction_MiddleItemSelected_ShowsSidePeeks()
        {
            var carousel = new Carousel
            {
                Background = Brushes.Transparent,
                ViewportFraction = 0.8,
                SelectedIndex = 1,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                ItemsSource = new Control[]
                {
                    CreateCard("One", "#D8574B", "#F7C5BE"),
                    CreateCard("Two", "#3E7AD9", "#BCD0F7"),
                    CreateCard("Three", "#3D9B67", "#BEE4CB"),
                }
            };

            var target = new Border
            {
                Width = 520,
                Height = 340,
                Background = Brushes.White,
                Padding = new Thickness(20),
                Child = carousel
            };

            AvaloniaLocator.CurrentMutable.Bind<ICursorFactory>().ToConstant(new CursorFactoryStub());
            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        private static Control CreateCard(string label, string background, string accent)
        {
            return new Border
            {
                Margin = new Thickness(14, 12),
                CornerRadius = new CornerRadius(18),
                ClipToBounds = true,
                Background = Brush.Parse(background),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(2),
                Child = new Grid
                {
                    Children =
                    {
                        new Border
                        {
                            Height = 56,
                            Background = Brush.Parse(accent),
                            VerticalAlignment = VerticalAlignment.Top
                        },
                        new Border
                        {
                            Width = 88,
                            Height = 88,
                            CornerRadius = new CornerRadius(44),
                            Background = Brushes.White,
                            Opacity = 0.9,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        new Border
                        {
                            Background = new SolidColorBrush(Color.Parse("#80000000")),
                            VerticalAlignment = VerticalAlignment.Bottom,
                            Padding = new Thickness(12),
                            Child = new TextBlock
                            {
                                Text = label,
                                Foreground = Brushes.White,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                FontWeight = FontWeight.SemiBold
                            }
                        }
                    }
                }
            };
        }

        private sealed class CursorFactoryStub : ICursorFactory
        {
            public ICursorImpl GetCursor(StandardCursorType cursorType) => new CursorStub();

            public ICursorImpl CreateCursor(Bitmap cursor, PixelPoint hotSpot) => new CursorStub();

            private sealed class CursorStub : ICursorImpl
            {
                public void Dispose()
                {
                }
            }
        }
    }
}
