using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Xunit;

namespace Avalonia.Skia.RenderTests
{
    public class ImageDrawingTests : TestBase
    {
        public ImageDrawingTests()
            : base(@"Media\ImageDrawing")
        {
        }

        private string BitmapPath
        {
            get
            {
                return System.IO.Path.Combine(OutputPath, "github_icon.png");
            }
        }

        [Fact]
        public async Task ImageDrawing_Fill()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Image
                {
                    Source = new DrawingImage
                    {
                        Drawing = new ImageDrawing
                        {
                            ImageSource = new Bitmap(BitmapPath),
                            Rect = new Rect(0, 0, 200, 200),
                        }
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task ImageDrawing_Viewbox()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Image
                {
                    Source = new DrawingImage
                    {
                        Viewbox = new Rect(48, 37, 100, 125),
                        Drawing = new ImageDrawing
                        {
                            ImageSource = new Bitmap(BitmapPath),
                            Rect = new Rect(0, 0, 200, 200),
                        }
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task ImageDrawing_BottomRight()
        {
            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new Image
                {
                    Source = new DrawingImage
                    {
                        Drawing = new DrawingGroup
                        {
                            Children =
                            {
                                new GeometryDrawing
                                {
                                    Geometry = StreamGeometry.Parse("m0,0 l200,200"),
                                    Brush = Brushes.Black,
                                },
                                new ImageDrawing
                                {
                                    ImageSource = new Bitmap(BitmapPath),
                                    Rect = new Rect(100, 100, 100, 100),
                                }
                            }
                        }
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Should_Render_DrawingBrushTransform()
        {
            var target = new Border
            {
                Width = 400,
                Height = 400,
                Child = new DrawingBrushTransformTest()
            };

            await RenderToFile(target);
            CompareImages();
        }

        public class DrawingBrushTransformTest : Control
        {
            private readonly DrawingBrush _brush;

            public DrawingBrushTransformTest()
            {
                _brush = new DrawingBrush()
                {
                    TileMode = TileMode.None,
                    SourceRect = new RelativeRect(0, 0, 1, 1, RelativeUnit.Relative),
                    DestinationRect = new RelativeRect(0, 0, 50, 50, RelativeUnit.Absolute),
                    Transform = new TranslateTransform(150, 150),
                    Drawing = new DrawingGroup()
                    {
                        Children = new DrawingCollection()
                        {
                            new GeometryDrawing
                            {
                                Brush = Brushes.Crimson,
                                Geometry = new RectangleGeometry(new(0, 0, 100, 100))
                            },
                            new GeometryDrawing
                            {
                                Brush = Brushes.Blue,
                                Geometry = new RectangleGeometry(new(20, 20, 60, 60))
                            }
                        }
                    }
                };
            }

            public override void Render(DrawingContext drawingContext)
            {
                var pop = drawingContext.PushTransform(Matrix.CreateTranslation(100, 100));
                var rc = new Rect(0, 0, 200, 200);
                drawingContext.DrawRectangle(new SolidColorBrush(Colors.DimGray), null, rc);
                drawingContext.DrawRectangle(_brush, null, rc);

                pop.Dispose();
            }
        }
    }
}
