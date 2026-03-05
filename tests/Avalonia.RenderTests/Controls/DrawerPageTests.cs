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
    public class DrawerPageRenderTests : TestBase
    {
        public DrawerPageRenderTests()
            : base(@"Controls\DrawerPage")
        {
        }

        private static Style FontStyle => new Style(x => x.OfType<TextBlock>())
        {
            Setters = { new Setter(TextBlock.FontFamilyProperty, TestFontFamily) }
        };

        [Fact]
        public async Task DrawerPage_Closed_ShowsTopBar()
        {
            var target = new Decorator
            {
                Width = 500,
                Height = 350,
                Child = new DrawerPage
                {
                    Background = Brushes.White,
                    Header = "My App",
                    DrawerLength = 200,
                    Drawer = new TextBlock
                    {
                        Text = "Drawer content",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        Margin = new Thickness(16),
                        VerticalAlignment = VerticalAlignment.Top
                    },
                    Content = new TextBlock
                    {
                        Text = "Detail content",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task DrawerPage_Open_ShowsDrawerPane()
        {
            var target = new Decorator
            {
                Width = 500,
                Height = 350,
                Child = new DrawerPage
                {
                    Background = Brushes.White,
                    Header = "My App",
                    DrawerLength = 200,
                    IsOpen = true,
                    DrawerBackground = new SolidColorBrush(Color.Parse("#F5F5F5")),
                    Drawer = new TextBlock
                    {
                        Text = "Drawer content",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        Margin = new Thickness(16),
                        VerticalAlignment = VerticalAlignment.Top
                    },
                    Content = new TextBlock
                    {
                        Text = "Detail content",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task DrawerPage_Locked_NoTopBar()
        {
            var target = new Decorator
            {
                Width = 500,
                Height = 350,
                Child = new DrawerPage
                {
                    Background = Brushes.White,
                    DrawerLength = 180,
                    DrawerBehavior = DrawerBehavior.Locked,
                    DrawerBackground = new SolidColorBrush(Color.Parse("#E3F2FD")),
                    Drawer = new TextBlock
                    {
                        Text = "Locked drawer",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        Margin = new Thickness(16),
                        VerticalAlignment = VerticalAlignment.Top
                    },
                    Content = new TextBlock
                    {
                        Text = "Detail content",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task DrawerPage_CompactOverlay_Closed_ShowsRail()
        {
            var target = new Decorator
            {
                Width = 500,
                Height = 350,
                Child = new DrawerPage
                {
                    Background = Brushes.White,
                    Header = "My App",
                    DrawerLength = 200,
                    DrawerLayoutBehavior = DrawerLayoutBehavior.CompactOverlay,
                    CompactDrawerLength = 48,
                    DrawerBackground = new SolidColorBrush(Color.Parse("#E8EAF6")),
                    Drawer = new StackPanel
                    {
                        Margin = new Thickness(0, 4),
                        Children =
                        {
                            new Border { Width = 24, Height = 24, Background = new SolidColorBrush(Color.Parse("#3949AB")), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8) },
                            new Border { Width = 24, Height = 24, Background = new SolidColorBrush(Color.Parse("#E53935")), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4) },
                            new Border { Width = 24, Height = 24, Background = new SolidColorBrush(Color.Parse("#43A047")), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4) },
                        }
                    },
                    Content = new TextBlock
                    {
                        Text = "Detail content",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task DrawerPage_CompactOverlay_Open_PaneOverlaysContent()
        {
            var target = new Decorator
            {
                Width = 500,
                Height = 350,
                Child = new DrawerPage
                {
                    Background = Brushes.White,
                    Header = "My App",
                    DrawerLength = 200,
                    IsOpen = true,
                    DrawerLayoutBehavior = DrawerLayoutBehavior.CompactOverlay,
                    CompactDrawerLength = 48,
                    DrawerBackground = new SolidColorBrush(Color.Parse("#E8EAF6")),
                    Drawer = new TextBlock
                    {
                        Text = "Drawer content",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        Margin = new Thickness(16),
                        VerticalAlignment = VerticalAlignment.Top
                    },
                    Content = new TextBlock
                    {
                        Text = "Detail content",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task DrawerPage_CompactInline_Closed_ShowsRail()
        {
            var target = new Decorator
            {
                Width = 500,
                Height = 350,
                Child = new DrawerPage
                {
                    Background = Brushes.White,
                    Header = "My App",
                    DrawerLength = 200,
                    DrawerLayoutBehavior = DrawerLayoutBehavior.CompactInline,
                    CompactDrawerLength = 48,
                    DrawerBackground = new SolidColorBrush(Color.Parse("#E8F5E9")),
                    Drawer = new StackPanel
                    {
                        Margin = new Thickness(0, 4),
                        Children =
                        {
                            new Border { Width = 24, Height = 24, Background = new SolidColorBrush(Color.Parse("#2E7D32")), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8) },
                            new Border { Width = 24, Height = 24, Background = new SolidColorBrush(Color.Parse("#E53935")), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4) },
                            new Border { Width = 24, Height = 24, Background = new SolidColorBrush(Color.Parse("#FB8C00")), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4) },
                        }
                    },
                    Content = new TextBlock
                    {
                        Text = "Detail content",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task DrawerPage_CompactInline_Open_PanePushesContent()
        {
            var target = new Decorator
            {
                Width = 500,
                Height = 350,
                Child = new DrawerPage
                {
                    Background = Brushes.White,
                    Header = "My App",
                    DrawerLength = 200,
                    IsOpen = true,
                    DrawerLayoutBehavior = DrawerLayoutBehavior.CompactInline,
                    CompactDrawerLength = 48,
                    DrawerBackground = new SolidColorBrush(Color.Parse("#E8F5E9")),
                    Drawer = new TextBlock
                    {
                        Text = "Drawer content",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        Margin = new Thickness(16),
                        VerticalAlignment = VerticalAlignment.Top
                    },
                    Content = new TextBlock
                    {
                        Text = "Detail content",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task DrawerPage_Split_Open_ShowsBothPanes()
        {
            var target = new Decorator
            {
                Width = 500,
                Height = 350,
                Child = new DrawerPage
                {
                    Background = Brushes.White,
                    DrawerLength = 180,
                    DrawerLayoutBehavior = DrawerLayoutBehavior.Split,
                    IsOpen = true,
                    DrawerBackground = new SolidColorBrush(Color.Parse("#FFF3E0")),
                    Drawer = new TextBlock
                    {
                        Text = "Split drawer",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        Margin = new Thickness(16),
                        VerticalAlignment = VerticalAlignment.Top
                    },
                    Content = new TextBlock
                    {
                        Text = "Detail content",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task DrawerPage_RightPlacement_Open()
        {
            var target = new Decorator
            {
                Width = 500,
                Height = 350,
                Child = new DrawerPage
                {
                    Background = Brushes.White,
                    Header = "My App",
                    DrawerLength = 200,
                    IsOpen = true,
                    DrawerPlacement = DrawerPlacement.Right,
                    DrawerBackground = new SolidColorBrush(Color.Parse("#FCE4EC")),
                    Drawer = new TextBlock
                    {
                        Text = "Right drawer",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        Margin = new Thickness(16),
                        VerticalAlignment = VerticalAlignment.Top
                    },
                    Content = new TextBlock
                    {
                        Text = "Detail content",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task DrawerPage_TopPlacement_Open()
        {
            var target = new Decorator
            {
                Width = 500,
                Height = 350,
                Child = new DrawerPage
                {
                    Background = Brushes.White,
                    DrawerLength = 160,
                    IsOpen = true,
                    DrawerPlacement = DrawerPlacement.Top,
                    DrawerBackground = new SolidColorBrush(Color.Parse("#E8EAF6")),
                    Drawer = new TextBlock
                    {
                        Text = "Top drawer",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        Margin = new Thickness(16),
                        VerticalAlignment = VerticalAlignment.Top
                    },
                    Content = new TextBlock
                    {
                        Text = "Detail content",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task DrawerPage_BottomPlacement_Open()
        {
            var target = new Decorator
            {
                Width = 500,
                Height = 350,
                Child = new DrawerPage
                {
                    Background = Brushes.White,
                    DrawerLength = 160,
                    IsOpen = true,
                    DrawerPlacement = DrawerPlacement.Bottom,
                    DrawerBackground = new SolidColorBrush(Color.Parse("#FFF3E0")),
                    Drawer = new TextBlock
                    {
                        Text = "Bottom drawer",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        Margin = new Thickness(16),
                        VerticalAlignment = VerticalAlignment.Top
                    },
                    Content = new TextBlock
                    {
                        Text = "Detail content",
                        Foreground = Brushes.Black,
                        FontFamily = TestFontFamily,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }
    }
}
