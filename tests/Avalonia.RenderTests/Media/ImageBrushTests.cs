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
    public class ImageBrushTests : TestBase
    {
        public ImageBrushTests()
            : base(@"Media\ImageBrush")
        {
        }

        private string BitmapPath
        {
            get
            {
                return System.IO.Path.Combine(OutputPath, "github_icon.png");
            }
        }

        private string SmallBitmapPath
        {
            get
            {
                return System.IO.Path.Combine(OutputPath, "github_icon_small.png");
            }
        }

        [Fact]
        public async Task ImageBrush_NullSource()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Margin = new Thickness(8),
                    Fill = new ImageBrush()
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task ImageBrush_Tile_Fill()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Margin = new Thickness(8),
                    Fill = new ImageBrush
                    {
                        Stretch = Stretch.Fill,
                        TileMode = TileMode.Tile,
                        DestinationRect = new RelativeRect(0, 0, 25, 30, RelativeUnit.Absolute),
                        Source = new Bitmap(BitmapPath),
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task ImageBrush_Tile_UniformToFill()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Margin = new Thickness(8),
                    Fill = new ImageBrush
                    {
                        Stretch = Stretch.Uniform,
                        TileMode = TileMode.Tile,
                        DestinationRect = new RelativeRect(0, 0, 25, 30, RelativeUnit.Absolute),
                        Source = new Bitmap(BitmapPath),
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task ImageBrush_Tile_Small_Image()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Margin = new Thickness(8),
                    Fill = new ImageBrush
                    {
                        Stretch = Stretch.None,
                        TileMode = TileMode.Tile,
                        Source = new Bitmap(SmallBitmapPath),
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task ImageBrush_NoStretch_NoTile_Alignment_TopLeft()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Fill = new ImageBrush
                    {
                        Stretch = Stretch.None,
                        TileMode = TileMode.None,
                        AlignmentX = AlignmentX.Left,
                        AlignmentY = AlignmentY.Top,
                        Source = new Bitmap(BitmapPath),
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task ImageBrush_NoStretch_NoTile_Alignment_Center()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Fill = new ImageBrush
                    {
                        Stretch = Stretch.None,
                        TileMode = TileMode.None,
                        AlignmentX = AlignmentX.Center,
                        AlignmentY = AlignmentY.Center,
                        Source = new Bitmap(BitmapPath),
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task ImageBrush_NoStretch_NoTile_Alignment_BottomRight()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Fill = new ImageBrush
                    {
                        Stretch = Stretch.None,
                        TileMode = TileMode.None,
                        AlignmentX = AlignmentX.Right,
                        AlignmentY = AlignmentY.Bottom,
                        Source = new Bitmap(BitmapPath),
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task ImageBrush_Fill_NoTile()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 920,
                Height = 920,
                Child = new Rectangle
                {
                    Fill = new ImageBrush
                    {
                        Stretch = Stretch.Fill,
                        TileMode = TileMode.None,
                        Source = new Bitmap(BitmapPath),
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task ImageBrush_Uniform_NoTile()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 300,
                Height = 200,
                Child = new Rectangle
                {
                    Fill = new ImageBrush
                    {
                        Stretch = Stretch.Uniform,
                        TileMode = TileMode.None,
                        Source = new Bitmap(BitmapPath),
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task ImageBrush_UniformToFill_NoTile()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 300,
                Height = 200,
                Child = new Rectangle
                {
                    Fill = new ImageBrush
                    {
                        Stretch = Stretch.UniformToFill,
                        TileMode = TileMode.None,
                        Source = new Bitmap(BitmapPath),
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task ImageBrush_NoStretch_NoTile_BottomRightQuarterSource()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Fill = new ImageBrush
                    {
                        Stretch = Stretch.None,
                        TileMode = TileMode.None,
                        SourceRect = new RelativeRect(250, 250, 250, 250, RelativeUnit.Absolute),
                        Source = new Bitmap(BitmapPath),
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
        
        [Fact]
        public async Task ImageBrush_NoStretch_NoTile_BottomRightQuarterDest()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Fill = new ImageBrush
                    {
                        Stretch = Stretch.None,
                        TileMode = TileMode.None,
                        DestinationRect = new RelativeRect(92, 92, 92, 92, RelativeUnit.Absolute),
                        Source = new Bitmap(BitmapPath),
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
        
        [Fact]
        public async Task ImageBrush_NoStretch_NoTile_BottomRightQuarterSource_BottomRightQuarterDest()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Fill = new ImageBrush
                    {
                        Stretch = Stretch.None,
                        TileMode = TileMode.None,
                        SourceRect = new RelativeRect(0.5, 0.5, 0.5, 0.5, RelativeUnit.Relative),
                        DestinationRect = new RelativeRect(0.5, 0.5, 0.5, 0.5, RelativeUnit.Relative),
                        Source = new Bitmap(BitmapPath),
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task ImageBrush_NoStretch_Tile_BottomRightQuarterSource_CenterQuarterDest()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Fill = new ImageBrush
                    {
                        Stretch = Stretch.None,
                        TileMode = TileMode.Tile,
                        SourceRect = new RelativeRect(0.5, 0.5, 0.5, 0.5, RelativeUnit.Relative),
                        DestinationRect = new RelativeRect(0.25, 0.25, 0.5, 0.5, RelativeUnit.Relative),
                        Source = new Bitmap(BitmapPath),
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }


        [Fact]
        public async Task ImageBrush_NoStretch_FlipX_TopLeftDest()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Fill = new ImageBrush
                    {
                        Stretch = Stretch.None,
                        TileMode = TileMode.FlipX,
                        DestinationRect = new RelativeRect(0, 0, 0.5, 0.5, RelativeUnit.Relative),
                        Source = new Bitmap(BitmapPath),
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
        
        [Fact]
        public async Task ImageBrush_NoStretch_FlipY_TopLeftDest()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Fill = new ImageBrush
                    {
                        Stretch = Stretch.None,
                        TileMode = TileMode.FlipY,
                        DestinationRect = new RelativeRect(0, 0, 0.5, 0.5, RelativeUnit.Relative),
                        Source = new Bitmap(BitmapPath),
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task ImageBrush_NoStretch_FlipXY_TopLeftDest()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Fill = new ImageBrush
                    {
                        Stretch = Stretch.None,
                        TileMode = TileMode.FlipXY,
                        DestinationRect = new RelativeRect(0, 0, 0.5, 0.5, RelativeUnit.Relative),
                        Source = new Bitmap(BitmapPath),
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Theory,
         InlineData(false),
         InlineData(true)
        ]
        public async Task ImageBrush_Is_Properly_Mapped(bool relative)
        {
            var brush = new ImageBrush
            {
                Stretch = Stretch.Fill,
                TileMode = TileMode.Tile,
                DestinationRect = relative
                    ? new RelativeRect(0, 0, 1, 1, RelativeUnit.Relative)
                    : new RelativeRect(0, 0, 256, 256, RelativeUnit.Absolute),
                Source = new Bitmap(BitmapPath),
            };

            var testName =
                $"{nameof(ImageBrush_Is_Properly_Mapped)}_{brush.DestinationRect.Unit}";
            await RenderToFile(new RelativePointTestPrimitivesHelper(brush), testName);
            CompareImages(testName);
        }
    }
}
