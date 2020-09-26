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
                        GradientOrigin = new RelativePoint(0.25, 0.25, RelativeUnit.Relative)
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

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
                            new GradientStop { Color = Colors.Green, Offset = 0.5 },
                            new GradientStop { Color = Colors.Blue, Offset = 1 }
                        },
                        GradientOrigin = new RelativePoint(0.1, 0.1, RelativeUnit.Relative)
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
    }
}
