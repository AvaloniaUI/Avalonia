// -----------------------------------------------------------------------
// <copyright file="BorderTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.RenderTests.Controls
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Perspex.Controls;
    using Perspex.Layout;
    using Perspex.Media;

    [TestClass]
    public class BorderTests : TestBase
    {
        public BorderTests()
            : base(@"Controls\Border")
        {
        }

        [TestMethod]
        public void Border_1px_Border()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Content = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 1,
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [TestMethod]
        public void Border_2px_Border()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Content = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [TestMethod]
        public void Border_Fill()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Content = new Border
                {
                    Background = Brushes.Red,
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [TestMethod]
        public void Border_Brush_Offsets_Content()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Content = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                    Content = new Border
                    {
                        Background = Brushes.Red,
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [TestMethod]
        public void Border_Padding_Offsets_Content()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Content = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                    Padding = new Thickness(2),
                    Content = new Border
                    {
                        Background = Brushes.Red,
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [TestMethod]
        public void Border_Margin_Offsets_Content()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Content = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                    Content = new Border
                    {
                        Background = Brushes.Red,
                        Margin = new Thickness(2),
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [TestMethod]
        public void Border_Centers_Content_Horizontally()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Content = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                    Content = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = "Segoe UI",
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Center,
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [TestMethod]
        public void Border_Centers_Content_Vertically()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Content = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                    Content = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = "Segoe UI",
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Center,
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [TestMethod]
        public void Border_Stretches_Content_Horizontally()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Content = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                    Content = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = "Segoe UI",
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [TestMethod]
        public void Border_Stretches_Content_Vertically()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Content = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                    Content = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = "Segoe UI",
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Stretch,
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [TestMethod]
        public void Border_Left_Aligns_Content()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Content = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                    Content = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = "Segoe UI",
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Left,
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [TestMethod]
        public void Border_Right_Aligns_Content()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Content = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                    Content = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = "Segoe UI",
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Right,
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [TestMethod]
        public void Border_Top_Aligns_Content()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Content = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                    Content = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = "Segoe UI",
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Top,
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [TestMethod]
        public void Border_Bottom_Aligns_Content()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Content = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = 2,
                    Content = new TextBlock
                    {
                        Text = "Foo",
                        Background = Brushes.Red,
                        FontFamily = "Segoe UI",
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Bottom,
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [TestMethod]
        public void Border_Nested_Rotate()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Content = new Border
                {
                    Background = Brushes.Coral,
                    Width = 100,
                    Height = 100,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Content = new Border
                    {
                        Margin = new Thickness(25),
                        Background = Brushes.Chocolate,
                    },
                    RenderTransform = new RotateTransform(45),
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }
    }
}
