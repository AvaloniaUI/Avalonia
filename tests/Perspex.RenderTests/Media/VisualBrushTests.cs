// -----------------------------------------------------------------------
// <copyright file="VisualBrushTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.RenderTests.Media
{
    using Perspex.Controls;
    using Perspex.Controls.Shapes;
    using Perspex.Layout;
    using Perspex.Media;
    using Xunit;

    public class VisualBrushTests : TestBase
    {
        public VisualBrushTests()
            : base(@"Media\VisualBrush")
        {
        }

        [Fact]
        public void VisualBrush_Align_TopLeft()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Fill = new VisualBrush
                    {
                        AlignmentX = AlignmentX.Left,
                        AlignmentY = AlignmentY.Top,
                        Stretch = Stretch.None,
                        Visual = new Border
                        {
                            Width = 92,
                            Height = 92,
                            Background = Brushes.Red,
                            BorderBrush = Brushes.Black,
                            BorderThickness = 2,
                            Child = new TextBlock
                            {
                                Text = "Perspex",
                                FontSize = 12,
                                FontFamily = "Arial",
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                            }
                        }
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [Fact]
        public void VisualBrush_Align_Center()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Fill = new VisualBrush
                    {
                        AlignmentX = AlignmentX.Center,
                        AlignmentY = AlignmentY.Center,
                        Stretch = Stretch.None,
                        Visual = new Border
                        {
                            Width = 92,
                            Height = 92,
                            Background = Brushes.Red,
                            BorderBrush = Brushes.Black,
                            BorderThickness = 2,
                            Child = new TextBlock
                            {
                                Text = "Perspex",
                                FontSize = 12,
                                FontFamily = "Arial",
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                            }
                        }
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [Fact]
        public void VisualBrush_Align_BottomRight()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Fill = new VisualBrush
                    {
                        AlignmentX = AlignmentX.Right,
                        AlignmentY = AlignmentY.Bottom,
                        Stretch = Stretch.None,
                        Visual = new Border
                        {
                            Width = 92,
                            Height = 92,
                            Background = Brushes.Red,
                            BorderBrush = Brushes.Black,
                            BorderThickness = 2,
                            Child = new TextBlock
                            {
                                Text = "Perspex",
                                FontSize = 12,
                                FontFamily = "Arial",
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                            }
                        }
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [Fact]
        public void VisualBrush_Stretch_Fill_Large()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 920,
                Height = 920,
                Child = new Rectangle
                {
                    Fill = new VisualBrush
                    {
                        Stretch = Stretch.Fill,
                        Visual = new Border
                        {
                            Width = 92,
                            Height = 92,
                            Background = Brushes.Red,
                            BorderBrush = Brushes.Black,
                            BorderThickness = 2,
                            Child = new TextBlock
                            {
                                Text = "Perspex",
                                FontSize = 12,
                                FontFamily = "Arial",
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                            }
                        }
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [Fact]
        public void VisualBrush_Stretch_Uniform()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 920,
                Height = 820,
                Child = new Rectangle
                {
                    Fill = new VisualBrush
                    {
                        Stretch = Stretch.Uniform,
                        Visual = new Border
                        {
                            Width = 92,
                            Height = 92,
                            Background = Brushes.Red,
                            BorderBrush = Brushes.Black,
                            BorderThickness = 2,
                            Child = new TextBlock
                            {
                                Text = "Perspex",
                                FontSize = 12,
                                FontFamily = "Arial",
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                            }
                        }
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        [Fact]
        public void VisualBrush_Stretch_UniformToFill()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 920,
                Height = 820,
                Child = new Rectangle
                {
                    Fill = new VisualBrush
                    {
                        Stretch = Stretch.UniformToFill,
                        Visual = new Border
                        {
                            Width = 92,
                            Height = 92,
                            Background = Brushes.Red,
                            BorderBrush = Brushes.Black,
                            BorderThickness = 2,
                            Child = new TextBlock
                            {
                                Text = "Perspex",
                                FontSize = 12,
                                FontFamily = "Arial",
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                            }
                        }
                    }
                }
            };

            this.RenderToFile(target);
            this.CompareImages();
        }

        ////[Fact]
        ////public void VisualBrush_Line_Fill()
        ////{
        ////    Decorator target = new Decorator
        ////    {
        ////        Padding = new Thickness(8),
        ////        Width = 200,
        ////        Height = 200,
        ////        Child = new Line
        ////        {
        ////            X1 = 16,
        ////            Y1 = 16,
        ////            X2 = 184,
        ////            Y2 = 184,
        ////            StrokeThickness = 40,
        ////            StrokeStartLineCap = "Triangle",
        ////            StrokeEndLineCap = "Triangle",
        ////            Fill = new VisualBrush
        ////            {
        ////                Visual = new Border
        ////                {
        ////                    Width = 92,
        ////                    Height = 92,
        ////                    Background = Brushes.Red,
        ////                    BorderBrush = Brushes.Black,
        ////                    BorderThickness = 2,
        ////                    Child = new TextBlock
        ////                    {
        ////                        Text = "Perspex",
        ////                        FontSize = 12,
        ////                        FontFamily = "Arial",
        ////                        HorizontalAlignment = HorizontalAlignment.Center,
        ////                        VerticalAlignment = VerticalAlignment.Center,
        ////                    }
        ////                }
        ////            }
        ////        }
        ////    };

        ////    this.RenderToFile(target);
        ////    this.CompareImages();
        ////}
    }
}
