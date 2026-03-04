using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Themes.Simple;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Controls
#endif
{
    public class NavigationPageRenderTests : TestBase
    {
        public NavigationPageRenderTests()
            : base(@"Controls\NavigationPage")
        {
        }

        [Fact]
        public async Task NavigationPage_SinglePage_ShowsNavBar()
        {
            var target = new Decorator
            {
                Width = 400,
                Height = 300,
                Child = new NavigationPage
                {
                    Background = Brushes.White,
                    BarBackground = new SolidColorBrush(Color.Parse("#1565C0")),
                    BarForeground = Brushes.White,
                }
            };

            var nav = (NavigationPage)((Decorator)target).Child!;
            await nav.PushAsync(new ContentPage
            {
                Header = "Home",
                Background = Brushes.White,
                Content = new TextBlock
                {
                    Text = "Welcome to NavigationPage",
                    Foreground = Brushes.Black,
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            });

            target.Styles.Add(new SimpleTheme());
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task NavigationPage_TwoPages_ShowsBackButton()
        {
            var target = new Decorator
            {
                Width = 400,
                Height = 300,
                Child = new NavigationPage
                {
                    Background = Brushes.White,
                    BarBackground = new SolidColorBrush(Color.Parse("#1565C0")),
                    BarForeground = Brushes.White,
                }
            };

            var nav = (NavigationPage)((Decorator)target).Child!;
            await nav.PushAsync(new ContentPage
            {
                Header = "Home",
                Background = Brushes.White,
                Content = new TextBlock
                {
                    Text = "Page 1",
                    Foreground = Brushes.Black,
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            });

            await nav.PushAsync(new ContentPage
            {
                Header = "Details",
                Background = Brushes.White,
                Content = new TextBlock
                {
                    Text = "Page 2",
                    Foreground = Brushes.Black,
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            }, null);

            target.Styles.Add(new SimpleTheme());
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task NavigationPage_CustomBarBackground()
        {
            var target = new Decorator
            {
                Width = 400,
                Height = 300,
                Child = new NavigationPage
                {
                    Background = Brushes.White,
                    BarBackground = new SolidColorBrush(Color.Parse("#2E7D32")),
                    BarForeground = Brushes.White,
                    HasShadow = true
                }
            };

            var nav = (NavigationPage)((Decorator)target).Child!;
            await nav.PushAsync(new ContentPage
            {
                Header = "Green Theme",
                Background = Brushes.White,
                Content = new TextBlock
                {
                    Text = "Custom bar background",
                    Foreground = Brushes.Black,
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            });

            target.Styles.Add(new SimpleTheme());
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }
    }
}
