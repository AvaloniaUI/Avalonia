// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;
using Perspex.Controls.Shapes;
using Perspex.Layout;
using Perspex.Media;
using Perspex.Media.Imaging;
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

        private string BitmapPath
        {
            get { return System.IO.Path.Combine(OutputPath, "github_icon.png"); }
        }

        private Control Visual
        {
            get 
            {
                return new Panel
                {
                    Children = new Perspex.Controls.Controls
                    {
                        new Image
                        {
                            Source = new Bitmap(BitmapPath),
                        },
                        new Border
                        {
                            BorderBrush = Brushes.Blue,
                            BorderThickness = 2,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Child = new TextBlock
                            {
                                FontSize = 24,
                                FontFamily = "Arial",
                                Background = Brushes.Green,
                                Foreground = Brushes.Yellow,
                                Text = "VisualBrush",
                            }
                        }
                    }
                };
            }
        }

        [Fact]
        public void VisualBrush_NoStretch_NoTile_Alignment_TopLeft()
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
                        TileMode = TileMode.None,
                        AlignmentX = AlignmentX.Left,
                        AlignmentY = AlignmentY.Top,
                        Visual = Visual,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void VisualBrush_NoStretch_NoTile_Alignment_Center()
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
                        TileMode = TileMode.None,
                        AlignmentX = AlignmentX.Center,
                        AlignmentY = AlignmentY.Center,
                        Visual = Visual,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void VisualBrush_NoStretch_NoTile_Alignment_BottomRight()
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
                        TileMode = TileMode.None,
                        AlignmentX = AlignmentX.Right,
                        AlignmentY = AlignmentY.Bottom,
                        Visual = Visual,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void VisualBrush_Fill_NoTile()
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
                        TileMode = TileMode.None,
                        Visual = Visual,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void VisualBrush_Uniform_NoTile()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 300,
                Height = 200,
                Child = new Rectangle
                {
                    Fill = new VisualBrush
                    {
                        Stretch = Stretch.Uniform,
                        TileMode = TileMode.None,
                        Visual = Visual,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void VisualBrush_UniformToFill_NoTile()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 300,
                Height = 200,
                Child = new Rectangle
                {
                    Fill = new VisualBrush
                    {
                        Stretch = Stretch.UniformToFill,
                        TileMode = TileMode.None,
                        Visual = Visual,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void VisualBrush_NoStretch_NoTile_BottomRightQuarterSource()
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
                        TileMode = TileMode.None,
                        SourceRect = new RelativeRect(250, 250, 250, 250, RelativeUnit.Absolute),
                        Visual = Visual,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void VisualBrush_NoStretch_NoTile_BottomRightQuarterDest()
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
                        TileMode = TileMode.None,
                        DestinationRect = new RelativeRect(92, 92, 92, 92, RelativeUnit.Absolute),
                        Visual = Visual,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void VisualBrush_NoStretch_NoTile_BottomRightQuarterSource_BottomRightQuarterDest()
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
                        TileMode = TileMode.None,
                        SourceRect = new RelativeRect(0.5, 0.5, 0.5, 0.5, RelativeUnit.Relative),
                        DestinationRect = new RelativeRect(0.5, 0.5, 0.5, 0.5, RelativeUnit.Relative),
                        Visual = Visual,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void VisualBrush_NoStretch_Tile_BottomRightQuarterSource_CenterQuarterDest()
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
                        SourceRect = new RelativeRect(0.5, 0.5, 0.5, 0.5, RelativeUnit.Relative),
                        DestinationRect = new RelativeRect(0.25, 0.25, 0.5, 0.5, RelativeUnit.Relative),
                        Visual = Visual,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

#if PERSPEX_CAIRO
        [Fact(Skip = "TileMode.FlipX not yet supported on cairo")]
#else
        [Fact]
#endif
        public void VisualBrush_NoStretch_FlipX_TopLeftDest()
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
                        DestinationRect = new RelativeRect(0, 0, 0.5, 0.5, RelativeUnit.Relative),
                        Visual = Visual,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

#if PERSPEX_CAIRO
        [Fact(Skip = "TileMode.FlipY not yet supported on cairo")]
#else
        [Fact]
#endif
        public void VisualBrush_NoStretch_FlipY_TopLeftDest()
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
                        DestinationRect = new RelativeRect(0, 0, 0.5, 0.5, RelativeUnit.Relative),
                        Visual = Visual,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void VisualBrush_NoStretch_FlipXY_TopLeftDest()
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
                        DestinationRect = new RelativeRect(0, 0, 0.5, 0.5, RelativeUnit.Relative),
                        Visual = Visual,
                    }
                }
            };

            RenderToFile(target);
            CompareImages();
        }
    }
}
