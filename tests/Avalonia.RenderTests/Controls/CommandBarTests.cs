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
    public class CommandBarRenderTests : TestBase
    {
        public CommandBarRenderTests()
            : base(@"Controls\CommandBar")
        {
        }

        private static Style FontStyle => new Style(x => x.OfType<TextBlock>())
        {
            Setters = { new Setter(TextBlock.FontFamilyProperty, TestFontFamily) }
        };

        [Fact]
        public async Task CommandBar_Default_PrimaryCommands()
        {
            var target = new Decorator
            {
                Width = 500,
                Height = 60,
                Child = new CommandBar
                {
                    Background = Brushes.LightGray,
                    PrimaryCommands =
                    {
                        new AppBarButton
                        {
                            Label = "New",
                            Icon = new Path
                            {
                                Data = StreamGeometry.Parse("M19,13H13V19H11V13H5V11H11V5H13V11H19V13Z"),
                                Fill = Brushes.Black,
                                Width = 20,
                                Height = 20,
                                Stretch = Stretch.Uniform
                            }
                        },
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
                }
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task CommandBar_Compact_LabelCollapsed()
        {
            var target = new Decorator
            {
                Width = 300,
                Height = 60,
                Child = new CommandBar
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
                        },
                        new AppBarButton
                        {
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
                            IsChecked = true,
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
                }
            };

            target.Styles.Add(new SimpleTheme());
            target.Styles.Add(FontStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task AppBarButton_Overflow_ShowsLabel_WhenCompact()
        {
            var target = new Decorator
            {
                Width = 180,
                Height = 46,
                Child = new Border
                {
                    Background = Brushes.LightGray,
                    Padding = new Thickness(4),
                    Child = new AppBarButton
                    {
                        Label = "Settings",
                        IsCompact = true,
                        IsInOverflow = true,
                        LabelPosition = CommandBarDefaultLabelPosition.Right,
                        Icon = new Path
                        {
                            Data = StreamGeometry.Parse("M19.43,12.98C19.47,12.66 19.5,12.34 19.5,12C19.5,11.66 19.47,11.33 19.42,11L21.54,9.34C21.73,9.19 21.78,8.92 21.66,8.7L19.66,5.24C19.54,5.02 19.29,4.93 19.06,5.02L16.56,6.03C16.04,5.63 15.5,5.3 14.87,5.05L14.5,2.42C14.46,2.18 14.25,2 14,2H10C9.75,2 9.54,2.18 9.5,2.42L9.13,5.05C8.5,5.3 7.96,5.64 7.44,6.03L4.94,5.02C4.71,4.93 4.46,5.02 4.34,5.24L2.34,8.7C2.21,8.92 2.27,9.19 2.46,9.34L4.58,11C4.53,11.33 4.5,11.66 4.5,12C4.5,12.34 4.53,12.66 4.57,12.98L2.45,14.64C2.26,14.79 2.21,15.06 2.33,15.28L4.33,18.74C4.45,18.96 4.7,19.05 4.93,18.96L7.43,17.95C7.95,18.35 8.5,18.68 9.12,18.93L9.49,21.56C9.54,21.8 9.75,22 10,22H14C14.25,22 14.46,21.82 14.5,21.58L14.87,18.95C15.5,18.7 16.04,18.36 16.56,17.97L19.06,18.98C19.29,19.07 19.54,18.98 19.66,18.76L21.66,15.3C21.78,15.08 21.73,14.81 21.54,14.66L19.43,12.98M12,15.5A3.5,3.5 0 1,1 12,8A3.5,3.5 0 0,1 12,15.5Z"),
                            Fill = Brushes.Black,
                            Width = 16,
                            Height = 16,
                            Stretch = Stretch.Uniform
                        }
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
