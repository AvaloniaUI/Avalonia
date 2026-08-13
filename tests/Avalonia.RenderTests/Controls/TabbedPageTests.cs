using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
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
    public class TabbedPageRenderTests : TestBase
    {
        public TabbedPageRenderTests()
            : base(@"Controls\TabbedPage")
        {
        }

        private static Style FontStyle => new Style(x => x.OfType<TextBlock>())
        {
            Setters = { new Setter(TextBlock.FontFamilyProperty, TestFontFamily) }
        };

        private static TabbedPage CreateBasicTabbedPage() => new TabbedPage
        {
            Background = Brushes.White,
            Pages = new Page[]
            {
                new ContentPage
                {
                    Header = "Home",
                    Content = new TextBlock
                    {
                        Text = "Home page",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                },
                new ContentPage
                {
                    Header = "Favorites",
                    Content = new TextBlock
                    {
                        Text = "Favorites page",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                },
                new ContentPage
                {
                    Header = "Settings",
                    Content = new TextBlock
                    {
                        Text = "Settings page",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            }
        };

        [Fact]
        public async Task TabbedPage_Default_TopPlacement_FirstTabSelected()
        {
            var tabbedPage = CreateBasicTabbedPage();
            tabbedPage.TabPlacement = TabPlacement.Top;
            tabbedPage.SelectedIndex = 0;

            var target = new Decorator
            {
                Width = 400,
                Height = 300,
                Child = tabbedPage
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task TabbedPage_TopPlacement_SecondTabSelected()
        {
            var tabbedPage = CreateBasicTabbedPage();
            tabbedPage.TabPlacement = TabPlacement.Top;
            tabbedPage.SelectedIndex = 1;

            var target = new Decorator
            {
                Width = 400,
                Height = 300,
                Child = tabbedPage
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task TabbedPage_BottomPlacement()
        {
            var tabbedPage = CreateBasicTabbedPage();
            tabbedPage.TabPlacement = TabPlacement.Bottom;
            tabbedPage.SelectedIndex = 0;

            var target = new Decorator
            {
                Width = 400,
                Height = 300,
                Child = tabbedPage
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task TabbedPage_LeftPlacement()
        {
            var tabbedPage = CreateBasicTabbedPage();
            tabbedPage.TabPlacement = TabPlacement.Left;
            tabbedPage.SelectedIndex = 0;

            var target = new Decorator
            {
                Width = 500,
                Height = 300,
                Child = tabbedPage
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task TabbedPage_RightPlacement()
        {
            var tabbedPage = CreateBasicTabbedPage();
            tabbedPage.TabPlacement = TabPlacement.Right;
            tabbedPage.SelectedIndex = 0;

            var target = new Decorator
            {
                Width = 500,
                Height = 300,
                Child = tabbedPage
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task TabbedPage_CustomBarBackground()
        {
            var tabbedPage = CreateBasicTabbedPage();
            tabbedPage.TabPlacement = TabPlacement.Top;
            tabbedPage.SelectedIndex = 0;
            tabbedPage.Resources["TabbedPageTabStripBackground"] = new SolidColorBrush(Color.Parse("#2196F3"));
            tabbedPage.Resources["TabbedPageTabItemHeaderForegroundSelected"] = Brushes.White;
            tabbedPage.Resources["TabbedPageTabItemHeaderForegroundUnselected"] = Brushes.White;

            var target = new Decorator
            {
                Width = 400,
                Height = 300,
                Child = tabbedPage
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task TabbedPage_CustomTabColors()
        {
            var tabbedPage = CreateBasicTabbedPage();
            tabbedPage.TabPlacement = TabPlacement.Bottom;
            tabbedPage.SelectedIndex = 1;
            tabbedPage.Resources["TabbedPageTabItemHeaderForegroundSelected"] = new SolidColorBrush(Color.Parse("#E91E63"));
            tabbedPage.Resources["TabbedPageTabItemHeaderForegroundUnselected"] = new SolidColorBrush(Color.Parse("#9E9E9E"));

            var target = new Decorator
            {
                Width = 400,
                Height = 300,
                Child = tabbedPage
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task TabbedPage_TwoTabs()
        {
            var target = new Decorator
            {
                Width = 400,
                Height = 300,
                Child = new TabbedPage
                {
                    Background = Brushes.White,
                    TabPlacement = TabPlacement.Top,
                    Pages = new Page[]
                    {
                        new ContentPage
                        {
                            Header = "First",
                            Content = new TextBlock
                            {
                                Text = "First tab content",
                                Foreground = Brushes.Black,
                                FontFamily = TestFontFamily,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            }
                        },
                        new ContentPage
                        {
                            Header = "Second",
                            Content = new TextBlock
                            {
                                Text = "Second tab content",
                                Foreground = Brushes.Black,
                                FontFamily = TestFontFamily,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            }
                        }
                    }
                }
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task TabbedPage_WithIcons_TopPlacement()
        {
            var homeGeometry = StreamGeometry.Parse("M10,20V14H14V20H19V12H22L12,3L2,12H5V20H10Z");
            var searchGeometry = StreamGeometry.Parse("M9.5,3A6.5,6.5 0 0,1 16,9.5C16,11.11 15.41,12.59 14.44,13.73L14.71,14H15.5L20.5,19L19,20.5L14,15.5V14.71L13.73,14.44C12.59,15.41 11.11,16 9.5,16A6.5,6.5 0 0,1 3,9.5A6.5,6.5 0 0,1 9.5,3M9.5,5C7,5 5,7 5,9.5C5,12 7,14 9.5,14C12,14 14,12 14,9.5C14,7 12,5 9.5,5Z");
            var settingsGeometry = StreamGeometry.Parse("M12,15.5A3.5,3.5 0 0,1 8.5,12A3.5,3.5 0 0,1 12,8.5A3.5,3.5 0 0,1 15.5,12A3.5,3.5 0 0,1 12,15.5M19.43,12.97C19.47,12.65 19.5,12.33 19.5,12C19.5,11.67 19.47,11.34 19.43,11L21.54,9.37C21.73,9.22 21.78,8.95 21.66,8.73L19.66,5.27C19.54,5.05 19.27,4.96 19.05,5.05L16.56,6.05C16.04,5.66 15.5,5.32 14.87,5.07L14.5,2.42C14.46,2.18 14.25,2 14,2H10C9.75,2 9.54,2.18 9.5,2.42L9.13,5.07C8.5,5.32 7.96,5.66 7.44,6.05L4.95,5.05C4.73,4.96 4.46,5.05 4.34,5.27L2.34,8.73C2.21,8.95 2.27,9.22 2.46,9.37L4.57,11C4.53,11.34 4.5,11.67 4.5,12C4.5,12.33 4.53,12.65 4.57,12.97L2.46,14.63C2.27,14.78 2.21,15.05 2.34,15.27L4.34,18.73C4.46,18.95 4.73,19.04 4.95,18.95L7.44,17.94C7.96,18.34 8.5,18.68 9.13,18.93L9.5,21.58C9.54,21.82 9.75,22 10,22H14C14.25,22 14.46,21.82 14.5,21.58L14.87,18.93C15.5,18.67 16.04,18.34 16.56,17.94L19.05,18.95C19.27,19.04 19.54,18.95 19.66,18.73L21.66,15.27C21.78,15.05 21.73,14.78 21.54,14.63L19.43,12.97Z");

            var tabbedPage = new TabbedPage
            {
                Background = Brushes.White,
                TabPlacement = TabPlacement.Top,
                Pages = new Page[]
                {
                    new ContentPage
                    {
                        Header = "Home",
                        Icon = homeGeometry,
                        Content = new TextBlock
                        {
                            Text = "Home page",
                            Foreground = Brushes.Black,
                            FontFamily = TestFontFamily,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    },
                    new ContentPage
                    {
                        Header = "Search",
                        Icon = searchGeometry,
                        Content = new TextBlock
                        {
                            Text = "Search page",
                            Foreground = Brushes.Black,
                            FontFamily = TestFontFamily,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    },
                    new ContentPage
                    {
                        Header = "Settings",
                        Icon = settingsGeometry,
                        Content = new TextBlock
                        {
                            Text = "Settings page",
                            Foreground = Brushes.Black,
                            FontFamily = TestFontFamily,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                }
            };

            tabbedPage.SelectedIndex = 0;

            var target = new Decorator
            {
                Width = 400,
                Height = 300,
                Child = tabbedPage
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task TabbedPage_WithIcons_BottomPlacement()
        {
            var homeGeometry = StreamGeometry.Parse("M10,20V14H14V20H19V12H22L12,3L2,12H5V20H10Z");
            var searchGeometry = StreamGeometry.Parse("M9.5,3A6.5,6.5 0 0,1 16,9.5C16,11.11 15.41,12.59 14.44,13.73L14.71,14H15.5L20.5,19L19,20.5L14,15.5V14.71L13.73,14.44C12.59,15.41 11.11,16 9.5,16A6.5,6.5 0 0,1 3,9.5A6.5,6.5 0 0,1 9.5,3M9.5,5C7,5 5,7 5,9.5C5,12 7,14 9.5,14C12,14 14,12 14,9.5C14,7 12,5 9.5,5Z");
            var settingsGeometry = StreamGeometry.Parse("M12,15.5A3.5,3.5 0 0,1 8.5,12A3.5,3.5 0 0,1 12,8.5A3.5,3.5 0 0,1 15.5,12A3.5,3.5 0 0,1 12,15.5M19.43,12.97C19.47,12.65 19.5,12.33 19.5,12C19.5,11.67 19.47,11.34 19.43,11L21.54,9.37C21.73,9.22 21.78,8.95 21.66,8.73L19.66,5.27C19.54,5.05 19.27,4.96 19.05,5.05L16.56,6.05C16.04,5.66 15.5,5.32 14.87,5.07L14.5,2.42C14.46,2.18 14.25,2 14,2H10C9.75,2 9.54,2.18 9.5,2.42L9.13,5.07C8.5,5.32 7.96,5.66 7.44,6.05L4.95,5.05C4.73,4.96 4.46,5.05 4.34,5.27L2.34,8.73C2.21,8.95 2.27,9.22 2.46,9.37L4.57,11C4.53,11.34 4.5,11.67 4.5,12C4.5,12.33 4.53,12.65 4.57,12.97L2.46,14.63C2.27,14.78 2.21,15.05 2.34,15.27L4.34,18.73C4.46,18.95 4.73,19.04 4.95,18.95L7.44,17.94C7.96,18.34 8.5,18.68 9.13,18.93L9.5,21.58C9.54,21.82 9.75,22 10,22H14C14.25,22 14.46,21.82 14.5,21.58L14.87,18.93C15.5,18.67 16.04,18.34 16.56,17.94L19.05,18.95C19.27,19.04 19.54,18.95 19.66,18.73L21.66,15.27C21.78,15.05 21.73,14.78 21.54,14.63L19.43,12.97Z");

            var tabbedPage = new TabbedPage
            {
                Background = Brushes.White,
                TabPlacement = TabPlacement.Bottom,
                Pages = new Page[]
                {
                    new ContentPage
                    {
                        Header = "Home",
                        Icon = homeGeometry,
                        Content = new TextBlock
                        {
                            Text = "Home page",
                            Foreground = Brushes.Black,
                            FontFamily = TestFontFamily,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    },
                    new ContentPage
                    {
                        Header = "Search",
                        Icon = searchGeometry,
                        Content = new TextBlock
                        {
                            Text = "Search page",
                            Foreground = Brushes.Black,
                            FontFamily = TestFontFamily,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    },
                    new ContentPage
                    {
                        Header = "Settings",
                        Icon = settingsGeometry,
                        Content = new TextBlock
                        {
                            Text = "Settings page",
                            Foreground = Brushes.Black,
                            FontFamily = TestFontFamily,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                }
            };

            tabbedPage.SelectedIndex = 0;

            var target = new Decorator
            {
                Width = 400,
                Height = 300,
                Child = tabbedPage
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }
    }
}
