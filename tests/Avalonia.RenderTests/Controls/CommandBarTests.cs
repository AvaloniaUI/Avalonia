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

        private static Style OverflowLabelRectStyle => new Style(x => x.OfType<CommandBarButton>().Template().Name("PART_Label"))
        {
            Setters =
            {
                new Setter(TextBlock.WidthProperty, 50d),
                new Setter(TextBlock.HeightProperty, 10d),
                new Setter(TextBlock.BackgroundProperty, Brushes.Black),
            }
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
                        new CommandBarButton
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
                        new CommandBarButton
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
                        new CommandBarSeparator(),
                        new CommandBarToggleButton
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
                        new CommandBarButton
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
                        new CommandBarButton
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
                        new CommandBarSeparator(),
                        new CommandBarToggleButton
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
        public async Task CommandBarButton_Overflow_ShowsLabel_WhenCompact()
        {
            var target = new Decorator
            {
                Width = 180,
                Height = 46,
                Child = new Border
                {
                    Background = Brushes.LightGray,
                    Padding = new Thickness(4),
                    Child = new CommandBarButton
                    {
                        Label = string.Empty,
                        IsCompact = true,
                        IsInOverflow = true,
                        LabelPosition = CommandBarDefaultLabelPosition.Right,
                        Icon = new Path
                        {
                            Data = StreamGeometry.Parse("M19,13H13V19H11V13H5V11H11V5H13V11H19V13Z"),
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
            target.Styles.Add(OverflowLabelRectStyle);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

    }
}
