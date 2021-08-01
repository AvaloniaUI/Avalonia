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
    public class RadialGradientBrushTests : TestBase
    {
        public RadialGradientBrushTests() : base(@"Media\RadialGradientBrush")
        {
        }

        [Fact]
        public async Task RadialGradientBrush_RedBlue()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = new RadialGradientBrush
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

        /// <summary>
        /// Tests using a GradientOrigin that falls inside of the circle described by Center/Radius.
        /// </summary>
        [Fact]
        public async Task RadialGradientBrush_RedBlue_Offset_Inside()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = new RadialGradientBrush
                    {
                        GradientStops =
                        {
                            new GradientStop { Color = Colors.Red, Offset = 0 },
                            new GradientStop { Color = Colors.Blue, Offset = 1 }
                        },
                        GradientOrigin = new RelativePoint(0.25, 0.25, RelativeUnit.Relative)
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        /// <summary>
        /// Tests using a GradientOrigin that falls outside of the circle described by Center/Radius.
        /// </summary>
        [Fact]
        public async Task RadialGradientBrush_RedBlue_Offset_Outside()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = new RadialGradientBrush
                    {
                        GradientStops =
                        {
                            new GradientStop { Color = Colors.Red, Offset = 0 },
                            new GradientStop { Color = Colors.Blue, Offset = 1 }
                        },
                        GradientOrigin = new RelativePoint(0.1, 0.1, RelativeUnit.Relative)
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        /// <summary>
        /// Tests using a GradientOrigin that falls inside of the circle described by Center/Radius.
        /// </summary>
        [Fact]
        public async Task RadialGradientBrush_RedGreenBlue_Offset_Inside()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = new RadialGradientBrush
                    {
                        GradientStops =
                        {
                            new GradientStop { Color = Colors.Red, Offset = 0 },
                            new GradientStop { Color = Colors.Green, Offset = 0.5 },
                            new GradientStop { Color = Colors.Blue, Offset = 1 }
                        },
                        GradientOrigin = new RelativePoint(0.25, 0.25, RelativeUnit.Relative),
                        Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                        Radius = 0.5                        
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        /// <summary>
        /// Tests using a GradientOrigin that falls outside of the circle described by Center/Radius.
        /// </summary>
        [Fact]
        public async Task RadialGradientBrush_RedGreenBlue_Offset_Outside()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = new RadialGradientBrush
                    {
                        GradientStops =
                        {
                            new GradientStop { Color = Colors.Red, Offset = 0 },
                            new GradientStop { Color = Colors.Green, Offset = 0.25 },
                            new GradientStop { Color = Colors.Blue, Offset = 1 }
                        },
                        GradientOrigin = new RelativePoint(0.1, 0.1, RelativeUnit.Relative),
                        Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                        Radius = 0.5
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task RadialGradientBrush_DrawingContext()
        {
            var brush = new RadialGradientBrush
            {
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
                    c.DrawRectangle(brush, null, new Rect(100, 100, 100, 100));
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
    }
}
