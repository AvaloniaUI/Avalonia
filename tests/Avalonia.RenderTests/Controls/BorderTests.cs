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
    public class BorderTests : TestBase
    {
        public BorderTests()
            : base(@"Controls\Border")
        {
        }

        [Fact]
        public async Task Border_1px_Border()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Border_2px_Border()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2),
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Border_Uniform_CornerRadius()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(16),
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Border_NonUniform_CornerRadius()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(16, 4, 7, 10),
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Border_Fill()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = Brushes.Red,
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Border_Brush_Offsets_Content()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2),
                    Child = new Border
                    {
                        Background = Brushes.Red,
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Border_Padding_Offsets_Content()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2),
                    Padding = new Thickness(2),
                    Child = new Border
                    {
                        Background = Brushes.Red,
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Border_Margin_Offsets_Content()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2),
                    Child = new Border
                    {
                        Background = Brushes.Red,
                        Margin = new Thickness(2),
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }


        [Win32Fact("Has text")]
        public async Task Border_Centers_Content_Horizontally()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2),
                    Child = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Center,
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Win32Fact("Has text")]
        public async Task Border_Centers_Content_Vertically()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2),
                    Child = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Center,
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Border_Stretches_Content_Horizontally()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2),
                    Child = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Border_Stretches_Content_Vertically()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2),
                    Child = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Stretch,
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Win32Fact("Has text")]
        public async Task Border_Left_Aligns_Content()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2),
                    Child = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Left,
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Win32Fact("Has text")]
        public async Task Border_Right_Aligns_Content()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2),
                    Child = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Right,
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Win32Fact("Has text")]
        public async Task Border_Top_Aligns_Content()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2),
                    Child = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Top,
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Win32Fact("Has text")]
        public async Task Border_Bottom_Aligns_Content()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2),
                    Child = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Bottom,
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Border_Nested_Rotate()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = Brushes.Coral,
                    Width = 100,
                    Height = 100,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new Border
                    {
                        Margin = new Thickness(25),
                        Background = Brushes.Chocolate,
                    },
                    RenderTransform = new RotateTransform(45),
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
    }
}
