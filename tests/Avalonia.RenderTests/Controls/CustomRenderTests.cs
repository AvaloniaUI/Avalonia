// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
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
        public async Task Clip()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new CustomRenderer((control, context) =>
                {
                    context.FillRectangle(
                        Brushes.Red,
                        control.Bounds,
                        4);

                    using (context.PushClip(control.Bounds.Deflate(20)))
                    {
                        context.FillRectangle(
                            Brushes.Blue,
                            control.Bounds,
                            4);
                    }
                }),
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task Opacity()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new CustomRenderer((control, context) =>
                {
                    context.FillRectangle(
                        Brushes.Red,
                        control.Bounds,
                        4);

                    using (context.PushOpacity(0.5))
                    {
                        context.FillRectangle(
                            Brushes.Blue,
                            control.Bounds.Deflate(20),
                            4);
                    }
                }),
            };

            await RenderToFile(target);
            CompareImages();
        }

        class CustomRenderer : Control
        {
            private Action<CustomRenderer, DrawingContext> _render;
            public CustomRenderer(Action<CustomRenderer, DrawingContext> render) => _render = render;
            public override void Render(DrawingContext context) => _render(this, context);
        }
    }
}
