// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Xunit;

#if AVALONIA_CAIRO
namespace Avalonia.Cairo.RenderTests.Controls
#elif AVALONIA_SKIA
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
        public void Border_1px_Border()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 1,
                }
            };

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void Border_2px_Border()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                }
            };

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void Border_Fill()
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

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void Border_Brush_Offsets_Content()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                    Child = new Border
                    {
                        Background = Brushes.Red,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void Border_Padding_Offsets_Content()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                    Padding = new Thickness(2),
                    Child = new Border
                    {
                        Background = Brushes.Red,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void Border_Margin_Offsets_Content()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                    Child = new Border
                    {
                        Background = Brushes.Red,
                        Margin = new Thickness(2),
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_CAIRO
        [Fact(Skip = "Font scaling currently broken on cairo")]
#else
        [Fact]
#endif
        public void Border_Centers_Content_Horizontally()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                    Child = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = "Segoe UI",
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Center,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_CAIRO
        [Fact(Skip = "Font scaling currently broken on cairo")]
#elif AVALONIA_SKIA_SKIP_FAIL
        [Fact(Skip = "Waiting for new FormattedText")]
#else
        [Fact]
#endif
        public void Border_Centers_Content_Vertically()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                    Child = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = "Segoe UI",
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Center,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_CAIRO
        [Fact(Skip = "Font scaling currently broken on cairo")]
#else
        [Fact]
#endif
        public void Border_Stretches_Content_Horizontally()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                    Child = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = "Segoe UI",
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_CAIRO
        [Fact(Skip = "Font scaling currently broken on cairo")]
#else
        [Fact]
#endif
        public void Border_Stretches_Content_Vertically()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                    Child = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = "Segoe UI",
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Stretch,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_CAIRO
        [Fact(Skip = "Font scaling currently broken on cairo")]
#else
        [Fact]
#endif
        public void Border_Left_Aligns_Content()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                    Child = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = "Segoe UI",
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Left,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_CAIRO
        [Fact(Skip = "Font scaling currently broken on cairo")]
#else
        [Fact]
#endif
        public void Border_Right_Aligns_Content()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                    Child = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = "Segoe UI",
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Right,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_CAIRO
        [Fact(Skip = "Font scaling currently broken on cairo")]
#elif AVALONIA_SKIA_SKIP_FAIL
        [Fact(Skip = "Waiting for new FormattedText")]
#else
        [Fact]
#endif
        public void Border_Top_Aligns_Content()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                    Child = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = "Segoe UI",
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Top,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_CAIRO
        [Fact(Skip = "Font scaling currently broken on cairo")]
#elif AVALONIA_SKIA_SKIP_FAIL
        [Fact(Skip = "Waiting for new FormattedText")]
#else
        [Fact]
#endif
        public void Border_Bottom_Aligns_Content()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                    Child = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = "Segoe UI",
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Bottom,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void Border_Nested_Rotate()
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

            RenderToFile(target);
            CompareImages();
        }
    }
}
