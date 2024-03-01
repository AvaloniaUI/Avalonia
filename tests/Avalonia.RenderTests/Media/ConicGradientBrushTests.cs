using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Threading.Tasks;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Media
#endif
{
    public class ConicGradientBrushTests : TestBase
    {
        public ConicGradientBrushTests() : base(@"Media\ConicGradientBrush")
        {
        }

        [Fact]
        public async Task ConicGradientBrush_RedBlue()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = new ConicGradientBrush
                    {
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
        public async Task ConicGradientBrush_RedBlue_Rotation()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = new ConicGradientBrush
                    {
                        GradientStops =
                        {
                            new GradientStop { Color = Colors.Red, Offset = 0 },
                            new GradientStop { Color = Colors.Blue, Offset = 1 }
                        },
                        Angle = 90
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task ConicGradientBrush_RedBlue_Center()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = new ConicGradientBrush
                    {
                        GradientStops =
                        {
                            new GradientStop { Color = Colors.Red, Offset = 0 },
                            new GradientStop { Color = Colors.Blue, Offset = 1 }
                        },
                        Center = new RelativePoint(0.25, 0.25, RelativeUnit.Relative)
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task ConicGradientBrush_RedBlue_Center_and_Rotation()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = new ConicGradientBrush
                    {
                        GradientStops =
                        {
                            new GradientStop { Color = Colors.Red, Offset = 0 },
                            new GradientStop { Color = Colors.Blue, Offset = 1 }
                        },
                        Center = new RelativePoint(0.25, 0.25, RelativeUnit.Relative),
                        Angle = 90
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task ConicGradientBrush_RedBlue_SoftEdge()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = new ConicGradientBrush
                    {
                        GradientStops =
                        {
                            new GradientStop { Color = Colors.Red, Offset = 0 },
                            new GradientStop { Color = Colors.Blue, Offset = 0.5 },
                            new GradientStop { Color = Colors.Red, Offset = 1 },
                        }
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task ConicGradientBrush_Umbrella()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = new ConicGradientBrush
                    {
                        GradientStops =
                        {
                            new GradientStop { Color = Colors.Red, Offset = 0 },
                            new GradientStop { Color = Colors.Yellow, Offset = 0.1667 },
                            new GradientStop { Color = Colors.Lime, Offset = 0.3333 },
                            new GradientStop { Color = Colors.Aqua, Offset = 0.5000 },
                            new GradientStop { Color = Colors.Blue, Offset = 0.6667 },
                            new GradientStop { Color = Colors.Magenta, Offset = 0.8333 },
                            new GradientStop { Color = Colors.Red, Offset = 1 },
                        }
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task ConicGradientBrush_DrawingContext()
        {
            var brush = new ConicGradientBrush
            {
                GradientStops =
                {
                    new GradientStop { Color = Colors.Red, Offset = 0 },
                    new GradientStop { Color = Colors.Yellow, Offset = 0.1667 },
                    new GradientStop { Color = Colors.Lime, Offset = 0.3333 },
                    new GradientStop { Color = Colors.Aqua, Offset = 0.5000 },
                    new GradientStop { Color = Colors.Blue, Offset = 0.6667 },
                    new GradientStop { Color = Colors.Magenta, Offset = 0.8333 },
                    new GradientStop { Color = Colors.Red, Offset = 1 },
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
        
        
        [Theory(
#if !AVALONIA_SKIA
        Skip = "Direct2D doesn't support conic brushes, why do we even have this file included?"
#endif
        ),
         InlineData(false),
         InlineData(true)
        ]
        public async Task ConicGradientBrushIsProperlyMapped(bool relative)
        {
            var brush = new ConicGradientBrush
            {
                Center = relative ? RelativePoint.Center : new RelativePoint(128,128, RelativeUnit.Absolute),
                GradientStops =
                {
                    new GradientStop { Color = Colors.Red, Offset = 0 },
                    new GradientStop { Color = Colors.GreenYellow, Offset = 0.2 },
                    new GradientStop { Color = Colors.Magenta, Offset = 0.5 },
                    new GradientStop { Color = Colors.Blue, Offset = 0.8 },
                    new GradientStop { Color = Colors.Red, Offset = 1 },
                },
                SpreadMethod = GradientSpreadMethod.Repeat,
                Angle = 270
            };
            
            var testName =
                $"{nameof(ConicGradientBrushIsProperlyMapped)}_{brush.Center.Unit}";
            await RenderToFile(new RelativePointTestPrimitivesHelper(brush, !relative), testName);
            CompareImages(testName);
        }
    }
}
