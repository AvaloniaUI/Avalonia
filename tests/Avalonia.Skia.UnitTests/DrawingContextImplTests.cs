using Avalonia.Media;
using Avalonia.Skia.Helpers;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.UnitTests
{
    public class DrawingContextImplTests
    {
        [Fact]
        public void DrawLine_With_Zero_Thickness_Pen_Does_Not_Throw()
        {
            var target = CreateTarget();
            target.DrawLine(new Pen(Brushes.Black, 0), new Point(0, 0), new Point(10, 10));
        }

        [Fact]
        public void DrawRectangle_With_Zero_Thickness_Pen_Does_Not_Throw()
        {
            var target = CreateTarget();
            target.DrawRectangle(Brushes.Black, new Pen(Brushes.Black, 0), new RoundedRect(new Rect(0, 0, 100, 100), new CornerRadius(4)));
        }

        public static TheoryData<RoundedRect> RoundedClips => new()
        {
            new RoundedRect(new Rect(10.5, 7.25, 60, 40)),
            new RoundedRect(new Rect(10.5, 7.25, 60, 40), new CornerRadius(8)),
            new RoundedRect(new Rect(10.5, 7.25, 60, 40), new CornerRadius(3, 6, 9, 12)),
            new RoundedRect(new Rect(10.5, 7.25, 60, 40), new Vector(12, 4)),
            new RoundedRect(new Rect(10.5, 7.25, 60, 40),
                new Vector(2, 5), new Vector(7, 3), new Vector(11, 8), new Vector(4, 10)),
            new RoundedRect(new Rect(10.5, 7.25, 40, 30), new Vector(60, 60)),
            new RoundedRect(new Rect(10.5, 7.25, 0, 30), new CornerRadius(5))
        };

        [Theory]
        [MemberData(nameof(RoundedClips))]
        public void PushClip_RoundedRect_Clips_Like_The_Radii_Array_Reference(RoundedRect clip)
        {
            using var actual = new SKBitmap(100, 100);
            using (var canvas = new SKCanvas(actual))
            {
                var target = (DrawingContextImpl)DrawingContextHelper.WrapSkiaCanvas(canvas, new Vector(96, 96));
                target.PushClip(clip);
                target.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 100, 100)));
                target.PopClip();
            }

            using var expected = new SKBitmap(100, 100);
            using (var canvas = new SKCanvas(expected))
            using (var roundRect = new SKRoundRect())
            using (var paint = new SKPaint { Color = SKColors.Black })
            {
                roundRect.SetRectRadii(
                    new SKRect((float)clip.Rect.Left, (float)clip.Rect.Top,
                        (float)clip.Rect.Right, (float)clip.Rect.Bottom),
                    new[]
                    {
                        new SKPoint((float)clip.RadiiTopLeft.X, (float)clip.RadiiTopLeft.Y),
                        new SKPoint((float)clip.RadiiTopRight.X, (float)clip.RadiiTopRight.Y),
                        new SKPoint((float)clip.RadiiBottomRight.X, (float)clip.RadiiBottomRight.Y),
                        new SKPoint((float)clip.RadiiBottomLeft.X, (float)clip.RadiiBottomLeft.Y),
                    });
                canvas.ClipRoundRect(roundRect, antialias: true);
                canvas.DrawRect(0, 0, 100, 100, paint);
            }

            // The clip must cut something, or the comparison proves nothing.
            Assert.Contains(actual.Bytes, b => b == 0);
            Assert.Equal(expected.Bytes, actual.Bytes);
        }

        private static DrawingContextImpl CreateTarget()
        {
            var canvas = new SKCanvas(new SKBitmap(100, 100));
            return (DrawingContextImpl)DrawingContextHelper.WrapSkiaCanvas(canvas, new Vector(96, 96));
        }
    }
}
