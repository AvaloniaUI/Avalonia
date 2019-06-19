// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Xunit;

#if AVALONIA_SKIA
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
                        new Rect(control.Bounds.Size),
                        4);

                    using (context.PushClip(new Rect(control.Bounds.Size).Deflate(10)))
                    {
                        context.FillRectangle(
                            Brushes.Blue,
                            new Rect(control.Bounds.Size),
                            4);
                    }
                }),
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task GeometryClip()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new CustomRenderer((control, context) =>
                {
                    var clip = new EllipseGeometry(new Rect(control.Bounds.Size));

                    context.FillRectangle(
                        Brushes.Red,
                        new Rect(control.Bounds.Size),
                        4);

                    using (context.PushGeometryClip(clip))
                    {
                        context.FillRectangle(
                            Brushes.Blue,
                            new Rect(control.Bounds.Size),
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
                        new Rect(control.Bounds.Size),
                        4);

                    using (context.PushOpacity(0.5))
                    {
                        context.FillRectangle(
                            Brushes.Blue,
                            new Rect(control.Bounds.Size).Deflate(10),
                            4);
                    }
                }),
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task OpacityMask()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new CustomRenderer((control, context) =>
                {
                    var mask = new LinearGradientBrush
                    {
                        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                        GradientStops =
                        {
                            new GradientStop(Color.FromUInt32(0xffffffff), 0),
                            new GradientStop(Color.FromUInt32(0x00ffffff), 1)
                        },
                    };

                    context.FillRectangle(
                        Brushes.Red,
                        new Rect(control.Bounds.Size),
                        4);

                    using (context.PushOpacityMask(mask, new Rect(control.Bounds.Size)))
                    {
                        context.FillRectangle(
                            Brushes.Blue,
                            new Rect(control.Bounds.Size).Deflate(10),
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
