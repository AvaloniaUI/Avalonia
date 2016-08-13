// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Xunit;

#if AVALONIA_CAIRO
namespace Avalonia.Cairo.RenderTests.Media
#elif AVALONIA_SKIA
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
            get { return System.IO.Path.Combine(OutputPath, "github_icon.png"); }
        }

        [Fact]
        public void ImageBrush_NoStretch_NoTile_Alignment_TopLeft()
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

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void ImageBrush_NoStretch_NoTile_Alignment_Center()
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

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void ImageBrush_NoStretch_NoTile_Alignment_BottomRight()
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

            RenderToFile(target);
            CompareImages();
        }
#if AVALONIA_SKIA_SKIP_FAIL
        [Fact(Skip = "FIXME")]
#else
        [Fact]
#endif
        public void ImageBrush_Fill_NoTile()
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

            RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_CAIRO
        [Fact(Skip = "Font image stretch currently broken on cairo")]
#elif AVALONIA_SKIA_SKIP_FAIL
        [Fact(Skip = "FIXME")]
#else
        [Fact]
#endif
        public void ImageBrush_Uniform_NoTile()
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

            RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_CAIRO
        [Fact(Skip = "Font image stretch currently broken on cairo")]
#elif AVALONIA_SKIA_SKIP_FAIL
        [Fact(Skip = "FIXME")]
#else
        [Fact]
#endif
        public void ImageBrush_UniformToFill_NoTile()
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

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void ImageBrush_NoStretch_NoTile_BottomRightQuarterSource()
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

            RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_SKIA_SKIP_FAIL
        [Fact(Skip = "FIXME")]
#else
        [Fact]
#endif
        public void ImageBrush_NoStretch_NoTile_BottomRightQuarterDest()
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

            RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_SKIA_SKIP_FAIL
        [Fact(Skip = "FIXME")]
#else
        [Fact]
#endif
        public void ImageBrush_NoStretch_NoTile_BottomRightQuarterSource_BottomRightQuarterDest()
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

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void ImageBrush_NoStretch_Tile_BottomRightQuarterSource_CenterQuarterDest()
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

            RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_CAIRO
        [Fact(Skip = "TileMode.FlipX not yet supported on cairo")]
#else
        [Fact]
#endif
        public void ImageBrush_NoStretch_FlipX_TopLeftDest()
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

            RenderToFile(target);
            CompareImages();
        }

#if AVALONIA_CAIRO
        [Fact(Skip = "TileMode.FlipY not yet supported on cairo")]
#else
        [Fact]
#endif
        public void ImageBrush_NoStretch_FlipY_TopLeftDest()
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

            RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public void ImageBrush_NoStretch_FlipXY_TopLeftDest()
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

            RenderToFile(target);
            CompareImages();
        }
    }
}
