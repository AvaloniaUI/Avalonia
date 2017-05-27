// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Xunit;

#if AVALONIA_CAIRO
namespace Avalonia.Cairo.RenderTests.Controls
#elif AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Controls
#endif
{
    public class CustomRenderTests : TestBase
    {
        public CustomRenderTests()
            : base(@"Controls\CustomRender")
        {
        }

        [Fact]
        public async Task Opacity()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new OpacityRenderer(),
            };

            await RenderToFile(target);
            CompareImages();
        }

        class OpacityRenderer : Control
        {
            public override void Render(DrawingContext context)
            {
                context.FillRectangle(
                    Brushes.Red,
                    Bounds,
                    4);

                using (context.PushOpacity(0.5))
                {
                    context.FillRectangle(
                        Brushes.Blue,
                        Bounds.Deflate(20),
                        4);
                }
            }
        }
    }
}
