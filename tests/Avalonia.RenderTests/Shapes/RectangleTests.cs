// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Shapes
#endif
{
    public class RectangleTests : TestBase
    {
        public RectangleTests()
            : base(@"Shapes\Rectangle")
        {
        }

        [Fact]
        public async Task Rectangle_0px_Stroke()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.Black,
                    StrokeThickness = 0
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Rectangle_1px_Stroke()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Rectangle_2px_Stroke()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Rectangle_Stroke_Fill()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    Fill = Brushes.Red,
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Rectangle_Stroke_Fill_ClipToBounds()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Rectangle
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    Fill = Brushes.Red,
                    ClipToBounds = true,
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
    }
}
