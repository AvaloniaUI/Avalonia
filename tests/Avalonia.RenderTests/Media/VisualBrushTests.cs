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
            get
            {
                return System.IO.Path.Combine(OutputPath, "github_icon.png");
            }
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
                            Child = new Panel
                            {
                                Height = 26,
                                Width = 150,
                                Background = Brushes.Green
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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
                            Child = new Panel
                            {
                                Height = 10,
                                Width = 50
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

        [Fact]
        public async Task VisualBrush_Grip_96_Dpi()
        {
            var target = new Border
            {
                Width = 100,
                Height = 10,
                Background = new VisualBrush
                {
                    SourceRect = new RelativeRect(0, 0, 4, 5, RelativeUnit.Absolute),
                    DestinationRect = new RelativeRect(0, 0, 4, 5, RelativeUnit.Absolute),
                    TileMode = TileMode.Tile,
                    Stretch = Stretch.UniformToFill,
                    Visual = new Canvas
                    {
                        Width = 4,
                        Height = 5,
                        Background = Brushes.WhiteSmoke,
                        Children =
                        {
                            new Rectangle
                            {
                                Width = 1,
                                Height = 1,
                                Fill = Brushes.Red,
                                [Canvas.LeftProperty] = 2,
                            },
                            new Rectangle
                            {
                                Width = 1,
                                Height = 1,
                                Fill = Brushes.Red,
                                [Canvas.TopProperty] = 2,
                            },
                            new Rectangle
                            {
                                Width = 1,
                                Height = 1,
                                Fill = Brushes.Red,
                                [Canvas.LeftProperty] = 2,
                                [Canvas.TopProperty] = 4,
                            }
                        }
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task VisualBrush_Grip_144_Dpi()
        {
            var target = new Border
            {
                Width = 100,
                Height = 7.5,
                Background = new VisualBrush
                {
                    SourceRect = new RelativeRect(0, 0, 4, 5, RelativeUnit.Absolute),
                    DestinationRect = new RelativeRect(0, 0, 4, 5, RelativeUnit.Absolute),
                    TileMode = TileMode.Tile,
                    Stretch = Stretch.UniformToFill,
                    Visual = new Canvas
                    {
                        Width = 4,
                        Height = 5,
                        Background = Brushes.WhiteSmoke,
                        Children =
                        {
                            new Rectangle
                            {
                                Width = 1,
                                Height = 1,
                                Fill = Brushes.Red,
                                [Canvas.LeftProperty] = 2,
                            },
                            new Rectangle
                            {
                                Width = 1,
                                Height = 1,
                                Fill = Brushes.Red,
                                [Canvas.TopProperty] = 2,
                            },
                            new Rectangle
                            {
                                Width = 1,
                                Height = 1,
                                Fill = Brushes.Red,
                                [Canvas.LeftProperty] = 2,
                                [Canvas.TopProperty] = 4,
                            }
                        }
                    }
                }
            };

            await RenderToFile(target, dpi: 144);
            CompareImages();
        }

        [Fact]
        public async Task VisualBrush_Grip_192_Dpi()
        {
            var target = new Border
            {
                Width = 100,
                Height = 10,
                Background = new VisualBrush
                {
                    SourceRect = new RelativeRect(0, 0, 4, 5, RelativeUnit.Absolute),
                    DestinationRect = new RelativeRect(0, 0, 4, 5, RelativeUnit.Absolute),
                    TileMode = TileMode.Tile,
                    Stretch = Stretch.UniformToFill,
                    Visual = new Canvas
                    {
                        Width = 4,
                        Height = 5,
                        Background = Brushes.WhiteSmoke,
                        Children =
                        {
                            new Rectangle
                            {
                                Width = 1,
                                Height = 1,
                                Fill = Brushes.Red,
                                [Canvas.LeftProperty] = 2,
                            },
                            new Rectangle
                            {
                                Width = 1,
                                Height = 1,
                                Fill = Brushes.Red,
                                [Canvas.TopProperty] = 2,
                            },
                            new Rectangle
                            {
                                Width = 1,
                                Height = 1,
                                Fill = Brushes.Red,
                                [Canvas.LeftProperty] = 2,
                                [Canvas.TopProperty] = 4,
                            }
                        }
                    }
                }
            };

            await RenderToFile(target, dpi: 192);
            CompareImages();
        }

        [Fact]
        public async Task VisualBrush_Checkerboard_96_Dpi()
        {
            var target = new Border
            {
                Width = 200,
                Height = 200,
                Background = new VisualBrush
                {
                    DestinationRect = new RelativeRect(0, 0, 16, 16, RelativeUnit.Absolute),
                    TileMode = TileMode.Tile,
                    Visual = new Canvas
                    {
                        Width = 16,
                        Height= 16,
                        Background = Brushes.Red,
                        Children =
                        {
                            new Rectangle
                            {
                                Width = 8,
                                Height = 8,
                                Fill = Brushes.Green,
                            },
                            new Rectangle
                            {
                                Width = 8,
                                Height = 8,
                                Fill = Brushes.Green,
                                [Canvas.LeftProperty] = 8,
                                [Canvas.TopProperty] = 8,
                            },
                        }
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task VisualBrush_Checkerboard_144_Dpi()
        {
            var target = new Border
            {
                Width = 200,
                Height = 200,
                Background = new VisualBrush
                {
                    DestinationRect = new RelativeRect(0, 0, 16, 16, RelativeUnit.Absolute),
                    TileMode = TileMode.Tile,
                    Visual = new Canvas
                    {
                        Width = 16,
                        Height = 16,
                        Background = Brushes.Red,
                        Children =
                        {
                            new Rectangle
                            {
                                Width = 8,
                                Height = 8,
                                Fill = Brushes.Green,
                            },
                            new Rectangle
                            {
                                Width = 8,
                                Height = 8,
                                Fill = Brushes.Green,
                                [Canvas.LeftProperty] = 8,
                                [Canvas.TopProperty] = 8,
                            },
                        }
                    }
                }
            };

            await RenderToFile(target, dpi: 144);
            CompareImages();
        }

        [Fact]
        public async Task VisualBrush_Checkerboard_192_Dpi()
        {
            var target = new Border
            {
                Width = 200,
                Height = 200,
                Background = new VisualBrush
                {
                    DestinationRect = new RelativeRect(0, 0, 16, 16, RelativeUnit.Absolute),
                    TileMode = TileMode.Tile,
                    Visual = new Canvas
                    {
                        Width = 16,
                        Height = 16,
                        Background = Brushes.Red,
                        Children =
                        {
                            new Rectangle
                            {
                                Width = 8,
                                Height = 8,
                                Fill = Brushes.Green,
                            },
                            new Rectangle
                            {
                                Width = 8,
                                Height = 8,
                                Fill = Brushes.Green,
                                [Canvas.LeftProperty] = 8,
                                [Canvas.TopProperty] = 8,
                            },
                        }
                    }
                }
            };

            await RenderToFile(target, dpi: 192);
            CompareImages();
        }
        
        
        [Theory,
         InlineData(false),
         InlineData(true)
        ]
        public async Task VisualBrush_Is_Properly_Mapped(bool relative)
        {
            var brush = new VisualBrush()
            {
                Stretch = Stretch.Fill,
                TileMode = TileMode.Tile,
                DestinationRect = relative
                    ? new RelativeRect(0, 0, 1, 1, RelativeUnit.Relative)
                    : new RelativeRect(0, 0, 256, 256, RelativeUnit.Absolute),
                Visual = Visual
            };

            var testName =
                $"{nameof(VisualBrush_Is_Properly_Mapped)}_{brush.DestinationRect.Unit}";
            await RenderToFile(new RelativePointTestPrimitivesHelper(brush), testName);
            CompareImages(testName);
        }
    }
}
