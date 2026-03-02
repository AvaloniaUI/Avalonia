using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
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
    public class ContentPageRenderTests : TestBase
    {
        public ContentPageRenderTests()
            : base(@"Controls\ContentPage")
        {
        }

        [Fact]
        public async Task ContentPage_Default_Content()
        {
            var target = new Decorator
            {
                Width = 400,
                Height = 200,
                Child = new ContentPage
                {
                    Background = Brushes.White,
                    Header = "My Page",
                    Content = new TextBlock
                    {
                        Text = "Hello, ContentPage!",
                        Foreground = Brushes.Black,
                        FontSize = 16,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                }
            };

            target.Styles.Add(new SimpleTheme());
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task ContentPage_WithTopAndBottomCommandBars()
        {
            var target = new Decorator
            {
                Width = 400,
                Height = 260,
                Child = new ContentPage
                {
                    Background = Brushes.White,
                    Header = "Editor",
                    TopCommandBar = new CommandBar
                    {
                        Background = Brushes.LightGray,
                        PrimaryCommands =
                        {
                            new AppBarButton
                            {
                                Label = "Save",
                                Icon = new Path
                                {
                                    Data = StreamGeometry.Parse("M15,9H5V5H15M12,19A3,3 0 0,1 9,16A3,3 0 0,1 12,13A3,3 0 0,1 15,16A3,3 0 0,1 12,19M17,3H5C3.89,3 3,3.9 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V7L17,3Z"),
                                    Fill = Brushes.Black,
                                    Width = 20,
                                    Height = 20,
                                    Stretch = Stretch.Uniform
                                }
                            },
                            new AppBarSeparator(),
                            new AppBarToggleButton
                            {
                                Label = "Bold",
                                Icon = new Path
                                {
                                    Data = StreamGeometry.Parse("M15.6,10.79C17.04,10.07 18,8.64 18,7C18,4.79 16.21,3 14,3H7V21H14.73C16.78,21 18.5,19.37 18.5,17.32C18.5,15.82 17.72,14.53 16.5,13.77C16.2,13.59 15.9,13.44 15.6,13.32V10.79M10,6.5H13C13.83,6.5 14.5,7.17 14.5,8C14.5,8.83 13.83,9.5 13,9.5H10V6.5M13.5,17.5H10V14H13.5C14.33,14 15,14.67 15,15.5C15,16.33 14.33,17.5 13.5,17.5Z"),
                                    Fill = Brushes.Black,
                                    Width = 20,
                                    Height = 20,
                                    Stretch = Stretch.Uniform
                                }
                            }
                        }
                    },
                    BottomCommandBar = new CommandBar
                    {
                        Background = Brushes.LightGray,
                        DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed,
                        OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Collapsed,
                        PrimaryCommands =
                        {
                            new AppBarButton
                            {
                                Icon = new Path
                                {
                                    Data = StreamGeometry.Parse("M19,13H13V19H11V13H5V11H11V5H13V11H19V13Z"),
                                    Fill = Brushes.Black,
                                    Width = 20,
                                    Height = 20,
                                    Stretch = Stretch.Uniform
                                }
                            }
                        }
                    },
                    Content = new TextBlock
                    {
                        Text = "Page Content",
                        Foreground = Brushes.Black,
                        FontSize = 14,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                }
            };

            target.Styles.Add(new SimpleTheme());
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }
    }
}
