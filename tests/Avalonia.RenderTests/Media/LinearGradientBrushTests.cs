using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Media
#endif
{
    public class LinearGradientBrushTests : TestBase
    {
        public LinearGradientBrushTests() : base(@"Media\LinearGradientBrush")
        {
        }
        
        [Fact]
        public async Task LinearGradientBrush_RedBlue_Horizontal_Fill()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = new LinearGradientBrush
                    {
                        StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                        EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                        GradientStops =
                        {
                            new GradientStop { Color = Colors.Red, Offset = 0 },
                            new GradientStop { Color = Colors.Blue, Offset = 1 }
                        }
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
        
        [Fact]
        public async Task LinearGradientBrush_RedBlue_Vertical_Fill()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = new LinearGradientBrush
                    {
                        StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
                        EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative),
                        GradientStops =
                        {
                            new GradientStop { Color = Colors.Red, Offset = 0 },
                            new GradientStop { Color = Colors.Blue, Offset = 1 }
                        }
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task LinearGradientBrush_DrawingContext()
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop { Color = Colors.Red, Offset = 0 },
                    new GradientStop { Color = Colors.Blue, Offset = 1 }
                }
            };

            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new DrawnControl(c =>
                {
                    c.DrawRectangle(brush, null, new Rect(0, 0, 100, 100));

                    using (c.PushTransform(Matrix.CreateTranslation(100, 100)))
                        c.DrawRectangle(brush, null, new Rect(0, 0, 100, 100));
                }),
            };

            await RenderToFile(target);
            CompareImages();
        }

        private class DrawnControl : Control
        {
            private readonly Action<DrawingContext> _render;
            public DrawnControl(Action<DrawingContext> render) => _render = render;
            public override void Render(DrawingContext context) => _render(context);
        }

        [Theory,
            InlineData(false),
            InlineData(true)
        ]
        public async Task LinearGradientBrushIsProperlyMapped(bool relative)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = relative ? new RelativePoint(0, 0, RelativeUnit.Relative) : new RelativePoint(50,0, RelativeUnit.Absolute),
                EndPoint = relative ? new RelativePoint(1, 1, RelativeUnit.Relative) : new RelativePoint(150,0, RelativeUnit.Absolute),
                GradientStops =
                {
                    new GradientStop { Color = Colors.Red, Offset = 0 },
                    new GradientStop { Color = Colors.Blue, Offset = 1 }
                },
                SpreadMethod = GradientSpreadMethod.Repeat
            };
            
            var testName =
                $"{nameof(LinearGradientBrushIsProperlyMapped)}_{brush.StartPoint.Unit}";
            await RenderToFile(new RelativePointTestPrimitivesHelper(brush), testName);
            CompareImages(testName);
        }
    }
}
