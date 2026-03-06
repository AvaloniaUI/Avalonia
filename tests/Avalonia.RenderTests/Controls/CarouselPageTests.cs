using System.Collections;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Controls
#endif
{
    public class CarouselPageRenderTests : TestBase
    {
        public CarouselPageRenderTests()
            : base(@"Controls\CarouselPage")
        {
        }

        private static Style FontStyle => new Style(x => x.OfType<TextBlock>())
        {
            Setters = { new Setter(TextBlock.FontFamilyProperty, TestFontFamily) }
        };

        private static ContentPage MakePage(string label, string bgHex, string fgHex) =>
            new ContentPage
            {
                Header = label,
                Background = new SolidColorBrush(Color.Parse(bgHex)),
                Content = new TextBlock
                {
                    Text = label,
                    Foreground = new SolidColorBrush(Color.Parse(fgHex)),
                    FontSize = 28,
                    FontWeight = FontWeight.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };

        [Fact]
        public async Task CarouselPage_Blue_Page()
        {
            var cp = new CarouselPage { Background = Brushes.White, PageTransition = null };
            ((IList)cp.Pages!).Add(MakePage("Page 1", "#1565C0", "#FFFFFF"));

            var target = new Decorator { Width = 400, Height = 300, Child = cp };
            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task CarouselPage_Green_Page()
        {
            var cp = new CarouselPage { Background = Brushes.White, PageTransition = null };
            ((IList)cp.Pages!).Add(MakePage("Page 2", "#2E7D32", "#FFFFFF"));

            var target = new Decorator { Width = 400, Height = 300, Child = cp };
            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task CarouselPage_Red_Page()
        {
            var cp = new CarouselPage { Background = Brushes.White, PageTransition = null };
            ((IList)cp.Pages!).Add(MakePage("Page 3", "#C62828", "#FFFFFF"));

            var target = new Decorator { Width = 400, Height = 300, Child = cp };
            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task CarouselPage_ThreePages_FirstSelected()
        {
            var cp = new CarouselPage { Background = Brushes.White, PageTransition = null };
            ((IList)cp.Pages!).Add(MakePage("Page 1", "#1565C0", "#FFFFFF"));
            ((IList)cp.Pages!).Add(MakePage("Page 2", "#2E7D32", "#FFFFFF"));
            ((IList)cp.Pages!).Add(MakePage("Page 3", "#C62828", "#FFFFFF"));

            var target = new Decorator { Width = 400, Height = 300, Child = cp };
            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task CarouselPage_CustomBackground()
        {
            var cp = new CarouselPage
            {
                Background = new SolidColorBrush(Color.Parse("#212121")),
                PageTransition = null
            };
            ((IList)cp.Pages!).Add(MakePage("Dark Theme", "#F57F17", "#212121"));

            var target = new Decorator { Width = 400, Height = 300, Child = cp };
            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }
    }
}
