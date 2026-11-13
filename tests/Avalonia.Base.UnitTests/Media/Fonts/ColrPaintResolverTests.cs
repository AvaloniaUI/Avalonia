using Avalonia.Media;
using Avalonia.Media.Fonts.Tables.Colr;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts
{
    /// <summary>
    /// The transforms COLR v1 transform paints resolve to, checked against fontTools'
    /// <c>Paint.getTransform</c> (font space, y-up, counter-clockwise angles).
    /// </summary>
    public class ColrPaintResolverTests
    {
        [Fact]
        public void PaintSkew_X_Angle_Leans_The_Y_Axis_Counter_Clockwise()
        {
            // xSkewAngle 0.25 (45 degrees): fontTools gives skew(-45deg, 0) = (1, 0, tan(-45deg), 1),
            // so x' = x - y and the point (0, 100) moves to (-100, 100).
            var transform = ResolveTransform(ColrPaint.Skew(0.25, 0, ColrPaint.Solid(0)));

            Assert.Equal(new Point(-100, 100), Round(transform.Transform(new Point(0, 100))));
        }

        [Fact]
        public void PaintSkew_Y_Angle_Leans_The_X_Axis_Counter_Clockwise()
        {
            // ySkewAngle 0.25: y' = y + x, so (100, 0) moves to (100, 100).
            var transform = ResolveTransform(ColrPaint.Skew(0, 0.25, ColrPaint.Solid(0)));

            Assert.Equal(new Point(100, 100), Round(transform.Transform(new Point(100, 0))));
        }

        private static Point Round(Point p) => new(System.Math.Round(p.X, 6), System.Math.Round(p.Y, 6));

        private static Matrix ResolveTransform(ColrPaint paint)
        {
            var colr = new ColrV1Builder().BaseGlyph(3, paint).Build();
            var typeface = ColrTestFont
                .Graft(SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular), colr)
                .TryCreateGlyphTypeface()!;

            Assert.True(ColrTable.TryLoad(typeface, out var colrTable));
            Assert.True(CpalTable.TryLoad(typeface, out var cpalTable));
            Assert.True(colrTable!.TryGetBaseGlyphV1Record(3, out var record));

            var context = new ColrContext(typeface, colrTable, cpalTable!, 0);
            var decycler = new PaintDecycler();

            Assert.True(PaintParser.TryParse(colrTable.ColrData.Span, colrTable.GetAbsolutePaintOffset(record.PaintOffset),
                in context, in decycler, out var parsed));

            return Assert.IsType<ResolvedTransform>(PaintResolver.ResolvePaint(parsed!, in context)).Matrix;
        }
    }
}
