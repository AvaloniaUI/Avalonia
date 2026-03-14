using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Skia.RenderTests;

public class ChildClipGeometryTests : TestBase
{
    public ChildClipGeometryTests()
        : base(@"Controls\ChildClip")
    {
    }

    [Fact]
    public async Task GeometryChildClip_Clips_Child()
    {
        // Child-only clip; decorator background remains visible.
        Decorator target = new Decorator
        {
            Padding = new Thickness(8),
            Width = 320,
            Height = 220,
            Child = new StarClipDecorator
            {
                Width = 200,
                Height = 200,
                ClipToBounds = true,
                Child = new Border
                {
                    Background = Brushes.DodgerBlue
                }
            }
        };

        await RenderToFile(target);
        CompareImages();
    }

    [Fact]
    public async Task GeometryChildClip_Clips_When_ClipToBounds_False()
    {
        // Layout clip applies even when ClipToBounds is false.
        Decorator target = new Decorator
        {
            Padding = new Thickness(8),
            Width = 320,
            Height = 220,
            Child = new StarLayoutClipDecorator
            {
                Width = 200,
                Height = 200,
                ClipToBounds = false,
                Child = new Border
                {
                    Background = Brushes.DodgerBlue
                }
            }
        };

        await RenderToFile(target);
        CompareImages();
    }

    [Fact]
    public async Task GeometryChildClip_Uses_LayoutSlotSize()
    {
        // Clip geometry uses the arranged layout slot size, not the final bounds.
        Decorator target = new Decorator
        {
            Padding = new Thickness(8),
            Width = 320,
            Height = 220,
            Child = new SlotSizeClipDecorator
            {
                Width = 100,
                Height = 100,
                ClipToBounds = true,
                Child = new Border
                {
                    Background = Brushes.DodgerBlue
                }
            }
        };

        await RenderToFile(target);
        CompareImages();
    }

    private sealed class StarClipDecorator : Decorator
    {
        private Geometry? _clipGeometry;
        private Size _clipGeometrySize;

        public override void Render(DrawingContext context)
        {
            context.FillRectangle(Brushes.LightGray, new Rect(Bounds.Size));
        }

        protected override Geometry? GetLayoutClip(Size layoutSlotSize)
        {
            return GetClipGeometry(layoutSlotSize);
        }

        private Geometry GetClipGeometry(Size size)
        {
            if (_clipGeometry != null && _clipGeometrySize == size)
                return _clipGeometry;

            _clipGeometrySize = size;
            _clipGeometry = BuildStarGeometry(size);
            return _clipGeometry;
        }

    }

    private sealed class StarLayoutClipDecorator : Decorator
    {
        private Geometry? _clipGeometry;
        private Size _clipGeometrySize;

        protected override Geometry? GetLayoutClip(Size layoutSlotSize)
        {
            return GetClipGeometry(layoutSlotSize);
        }

        public override void Render(DrawingContext context)
        {
            context.FillRectangle(Brushes.LightGray, new Rect(Bounds.Size));
        }

        private Geometry GetClipGeometry(Size size)
        {
            if (_clipGeometry != null && _clipGeometrySize == size)
                return _clipGeometry;

            _clipGeometrySize = size;
            _clipGeometry = BuildStarGeometry(size);
            return _clipGeometry;
        }
    }

    private sealed class SlotSizeClipDecorator : Decorator
    {
        protected override Geometry? GetLayoutClip(Size layoutSlotSize)
        {
            var width = Math.Max(0, layoutSlotSize.Width * 0.4);
            var height = Math.Max(0, layoutSlotSize.Height * 0.4);
            var origin = new Point(
                (layoutSlotSize.Width - width) * 0.5,
                (layoutSlotSize.Height - height) * 0.5);
            return new RectangleGeometry(new Rect(origin, new Size(width, height)));
        }

        public override void Render(DrawingContext context)
        {
            context.FillRectangle(Brushes.LightGray, new Rect(Bounds.Size));
        }
    }

    private static Geometry BuildStarGeometry(Size size)
    {
        var geometry = new StreamGeometry();
        var center = new Point(size.Width * 0.5, size.Height * 0.5);
        var outerRadius = Math.Min(size.Width, size.Height) * 0.45;
        var innerRadius = outerRadius * 0.45;

        using (var ctx = geometry.Open())
        {
            var angleStep = Math.PI / 5.0;
            for (var i = 0; i < 10; i++)
            {
                var radius = (i % 2 == 0) ? outerRadius : innerRadius;
                var angle = -Math.PI * 0.5 + angleStep * i;
                var point = new Point(
                    center.X + Math.Cos(angle) * radius,
                    center.Y + Math.Sin(angle) * radius);

                if (i == 0)
                    ctx.BeginFigure(point, true);
                else
                    ctx.LineTo(point);
            }

            ctx.EndFigure(true);
        }

        return geometry;
    }
}
