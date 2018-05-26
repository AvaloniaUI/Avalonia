// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Media
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
                    Children =
                    {
                        new Image
                        {
                            Source = new Bitmap(BitmapPath),
                        },
                        new Border
                        {
                            BorderBrush = Brushes.Blue,
                            BorderThickness = new Thickness(2),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Child = new TextBlock
                            {
                                FontSize = 24,
                                FontFamily = new FontFamily("Arial"),
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
        public async Task VisualBrush_NoStretch_NoTile_Alignment_TopLeft()
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

            await RenderToFile(target);
            CompareImages();
        }
        
#if AVALONIA_SKIA_SKIP_FAIL
        [Fact(Skip = "FIXME")]
#else
        [Fact]
#endif
        public async Task VisualBrush_NoStretch_NoTile_Alignment_Center()
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

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task VisualBrush_NoStretch_NoTile_Alignment_BottomRight()
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

            await RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_SKIA_SKIP_FAIL
        [Fact(Skip = "FIXME")]
#else
        [Fact]
#endif
        public async Task VisualBrush_Fill_NoTile()
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

            await RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_SKIA_SKIP_FAIL
        [Fact(Skip = "FIXME")]
#else
        [Fact]
#endif
        public async Task VisualBrush_Uniform_NoTile()
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

            await RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_SKIA_SKIP_FAIL
        [Fact(Skip = "FIXME")]
#else
        [Fact]
#endif
        public async Task VisualBrush_UniformToFill_NoTile()
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

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task VisualBrush_NoStretch_NoTile_BottomRightQuarterSource()
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

            await RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_SKIA_SKIP_FAIL
        [Fact(Skip = "FIXME")]
#else
        [Fact]
#endif
        public async Task VisualBrush_NoStretch_NoTile_BottomRightQuarterDest()
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

            await RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_SKIA_SKIP_FAIL
        [Fact(Skip = "FIXME")]
#else
        [Fact]
#endif
        public async Task VisualBrush_NoStretch_NoTile_BottomRightQuarterSource_BottomRightQuarterDest()
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

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task VisualBrush_NoStretch_Tile_BottomRightQuarterSource_CenterQuarterDest()
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

            await RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_SKIA_SKIP_FAIL
        [Fact(Skip = "FIXME")]
#else
        [Fact]
#endif
        public async Task VisualBrush_NoStretch_FlipX_TopLeftDest()
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

            await RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_SKIA_SKIP_FAIL
        [Fact(Skip = "FIXME")]
#else
        [Fact]
#endif
        public async Task VisualBrush_NoStretch_FlipY_TopLeftDest()
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

            await RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_SKIA_SKIP_FAIL
        [Fact(Skip = "FIXME")]
#else
        [Fact]
#endif
        public async Task VisualBrush_NoStretch_FlipXY_TopLeftDest()
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

            await RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_SKIA_SKIP_FAIL
        [Fact(Skip = "FIXME")]
#else
        [Fact]
#endif
        public async Task VisualBrush_InTree_Visual()
        {
            Border source;
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Grid
                {
                    RowDefinitions = new RowDefinitions("Auto,*"),
                    Children =
                    {
                        (source = new Border
                        {
                            Background = Brushes.Yellow,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Child = new TextBlock
                            {
                                Text = "Visual"
                            }
                        }),
                        new Border
                        {
                            Background = new VisualBrush
                            {
                                Stretch = Stretch.Uniform,
                                Visual = source,
                            },
                            [Grid.RowProperty] = 1,
                        }
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
    }
}
