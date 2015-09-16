// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;
using Perspex.Controls.Shapes;
using Perspex.Layout;
using Perspex.Media;
using Xunit;

#if PERSPEX_CAIRO
namespace Perspex.Cairo.RenderTests.Media
#else
namespace Perspex.Direct2D1.RenderTests.Media
#endif
{
    public class VisualBrushTests : TestBase
    {
        public VisualBrushTests()
            : base(@"Media\VisualBrush")
        {
        }

#if PERSPEX_CAIRO
        [Fact(Skip = "VisualBrush not yet implemented on Cairo")]
#else
        [Fact]
#endif
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

            RenderToFile(target);
            CompareImages();
        }

#if PERSPEX_CAIRO
        [Fact(Skip = "VisualBrush not yet implemented on Cairo")]
#else
        [Fact]
#endif
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

            RenderToFile(target);
            CompareImages();
        }

#if PERSPEX_CAIRO
        [Fact(Skip = "VisualBrush not yet implemented on Cairo")]
#else
        [Fact]
#endif
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

            RenderToFile(target);
            CompareImages();
        }

#if PERSPEX_CAIRO
        [Fact(Skip = "VisualBrush not yet implemented on Cairo")]
#else
        [Fact]
#endif
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

            RenderToFile(target);
            CompareImages();
        }

#if PERSPEX_CAIRO
        [Fact(Skip = "VisualBrush not yet implemented on Cairo")]
#else
        [Fact]
#endif
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

            RenderToFile(target);
            CompareImages();
        }

#if PERSPEX_CAIRO
        [Fact(Skip = "VisualBrush not yet implemented on Cairo")]
#else
        [Fact]
#endif
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

            RenderToFile(target);
            CompareImages();
        }

#if PERSPEX_CAIRO
        [Fact(Skip = "VisualBrush not yet implemented on Cairo")]
#else
        [Fact]
#endif
        public void VisualBrush_SourceRect_Absolute()
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
                        SourceRect = new RelativeRect(40, 40, 100, 100, RelativeUnit.Pixels),
                        Visual = new Border
                        {
                            Width = 180,
                            Height = 180,
                            Background = Brushes.Red,
                            BorderBrush = Brushes.Black,
                            BorderThickness = 2,
                            Child = new Ellipse
                            {
                                Width = 100,
                                Height = 100,
                                Fill = Brushes.Yellow,
                                VerticalAlignment = VerticalAlignment.Center,
                                HorizontalAlignment = HorizontalAlignment.Center,
                            }
                        }
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

#if PERSPEX_CAIRO
        [Fact(Skip = "VisualBrush not yet implemented on Cairo")]
#else
        [Fact]
#endif
        public void VisualBrush_DestinationRect_Absolute()
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
                        DestinationRect = new RelativeRect(92, 92, 92, 92, RelativeUnit.Pixels),
                        Visual = new Border
                        {
                            Width = 180,
                            Height = 180,
                            Background = Brushes.Red,
                            BorderBrush = Brushes.Black,
                            BorderThickness = 2,
                            Child = new Ellipse
                            {
                                Width = 100,
                                Height = 100,
                                Fill = Brushes.Yellow,
                                VerticalAlignment = VerticalAlignment.Center,
                                HorizontalAlignment = HorizontalAlignment.Center,
                            }
                        }
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

#if PERSPEX_CAIRO
        [Fact(Skip = "VisualBrush not yet implemented on Cairo")]
#else
        [Fact]
#endif
        public void VisualBrush_SourceRect_DestinationRect_Absolute()
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
                        SourceRect = new RelativeRect(40, 40, 100, 100, RelativeUnit.Pixels),
                        DestinationRect = new RelativeRect(92, 92, 92, 92, RelativeUnit.Pixels),
                        Visual = new Border
                        {
                            Width = 180,
                            Height = 180,
                            Background = Brushes.Red,
                            BorderBrush = Brushes.Black,
                            BorderThickness = 2,
                            Child = new Ellipse
                            {
                                Width = 100,
                                Height = 100,
                                Fill = Brushes.Yellow,
                                VerticalAlignment = VerticalAlignment.Center,
                                HorizontalAlignment = HorizontalAlignment.Center,
                            }
                        }
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

#if PERSPEX_CAIRO
        [Fact(Skip = "VisualBrush not yet implemented on Cairo")]
#else
        [Fact]
#endif
        public void VisualBrush_SourceRect_DestinationRect_Percent()
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
                        SourceRect = new RelativeRect(0.22, 0.22, 0.56, 0.56, RelativeUnit.Percent),
                        DestinationRect = new RelativeRect(0.5, 0.5, 0.5, 0.5, RelativeUnit.Percent),
                        Visual = new Border
                        {
                            Width = 180,
                            Height = 180,
                            Background = Brushes.Red,
                            BorderBrush = Brushes.Black,
                            BorderThickness = 2,
                            Child = new Ellipse
                            {
                                Width = 100,
                                Height = 100,
                                Fill = Brushes.Yellow,
                                VerticalAlignment = VerticalAlignment.Center,
                                HorizontalAlignment = HorizontalAlignment.Center,
                            }
                        }
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

#if PERSPEX_CAIRO
        [Fact(Skip = "VisualBrush not yet implemented on Cairo")]
#else
        [Fact]
#endif
        public void VisualBrush_Tile()
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
                        Stretch = Stretch.None,
                        TileMode = TileMode.Tile,
                        DestinationRect = new RelativeRect(0.25, 0.25, 0.5, 0.5, RelativeUnit.Percent),
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

            RenderToFile(target);
            CompareImages();
        }

#if PERSPEX_CAIRO
        [Fact(Skip = "VisualBrush not yet implemented on Cairo")]
#else
        [Fact]
#endif
        public void VisualBrush_Tile_Alignment_BottomRight()
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
                        Stretch = Stretch.None,
                        TileMode = TileMode.Tile,
                        AlignmentX = AlignmentX.Right,
                        AlignmentY = AlignmentY.Bottom,
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

            RenderToFile(target);
            CompareImages();
        }

#if PERSPEX_CAIRO
        [Fact(Skip = "VisualBrush not yet implemented on Cairo")]
#else
        [Fact]
#endif
        public void VisualBrush_FlipX()
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
                        Stretch = Stretch.None,
                        TileMode = TileMode.FlipX,
                        DestinationRect = new RelativeRect(0, 0, 0.5, 0.5, RelativeUnit.Percent),
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

            RenderToFile(target);
            CompareImages();
        }

#if PERSPEX_CAIRO
        [Fact(Skip = "VisualBrush not yet implemented on Cairo")]
#else
        [Fact]
#endif
        public void VisualBrush_FlipY()
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
                        Stretch = Stretch.None,
                        TileMode = TileMode.FlipY,
                        DestinationRect = new RelativeRect(0, 0, 0.5, 0.5, RelativeUnit.Percent),
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

            RenderToFile(target);
            CompareImages();
        }

#if PERSPEX_CAIRO
        [Fact(Skip = "VisualBrush not yet implemented on Cairo")]
#else
        [Fact]
#endif
        public void VisualBrush_FlipXY()
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
                        Stretch = Stretch.None,
                        TileMode = TileMode.FlipXY,
                        DestinationRect = new RelativeRect(0, 0, 0.5, 0.5, RelativeUnit.Percent),
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

            RenderToFile(target);
            CompareImages();
        }
    }
}
