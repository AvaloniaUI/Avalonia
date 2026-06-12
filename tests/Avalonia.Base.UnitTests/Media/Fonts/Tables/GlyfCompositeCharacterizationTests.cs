using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts.Tables;
using Avalonia.Media.Fonts.Tables.Glyf;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    /// <summary>
    /// On a truncated composite glyph, both <see cref="GlyfTable.TryBuildGlyphGeometry"/> and
    /// <see cref="GlyfTable.TryGetCompositeComponents"/> return <c>false</c> without throwing — the
    /// latter previously lacked the catch the former has.
    /// </summary>
    public class GlyfCompositeCharacterizationTests
    {
        [Fact]
        public void Truncated_Composite_Does_Not_Throw_From_Components_Or_Build()
        {
            var glyf = LoadGlyfWithTruncatedComposite();

            // Glyph 2 is a composite carrying only its 10-byte header — no component records — so
            // CompositeGlyph.Create over-reads when it parses the first component.

            // The geometry build wraps parsing in a catch-all: it returns false, never throwing.
            var context = new NullGeometryContext();
            var built = true;
            var buildException = Record.Exception(() => built = glyf.TryBuildGlyphGeometry(2, Matrix.Identity, context));
            Assert.Null(buildException);
            Assert.False(built);

            // The dependency-recording path now has the same catch as the build path, so the over-read
            // is contained — it returns false rather than throwing out of the table API.
            var gotComponents = true;
            var componentsException = Record.Exception(() => gotComponents = glyf.TryGetCompositeComponents(2, out _));
            Assert.Null(componentsException);
            Assert.False(gotComponents);
        }

        private static GlyfTable LoadGlyfWithTruncatedComposite()
        {
            // A 3-glyph TrueType font grafted onto Inter (which supplies the other required tables).
            // Glyph 2's loca range is a valid 10-byte slice, but the bytes are a composite header
            // with no following component — enough to reach CompositeGlyph.Create and over-read.
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular)
                .Replace("maxp", BuildMaxp(numGlyphs: 3))
                .Replace("head", BuildHead())
                .Replace("loca", BuildLongLoca(0, 0, 0, 10)) // glyph 2 = bytes [0, 10)
                .Replace("glyf", BuildCompositeHeaderOnly());

            var typeface = font.TryCreateGlyphTypeface();
            Assert.NotNull(typeface);

            Assert.True(HeadTable.TryLoad(typeface!, out var head));
            var maxp = MaxpTable.Load(typeface!);
            Assert.True(GlyfTable.TryLoad(typeface!, head!, maxp, out var glyf));

            return glyf!;
        }

        private static byte[] BuildMaxp(ushort numGlyphs)
            => new BigEndianBuffer()
                .UInt32(0x00005000) // version 0.5 (header is just version + numGlyphs)
                .UInt16(numGlyphs)
                .ToArray();

        private static byte[] BuildHead()
        {
            var head = new BigEndianBuffer();
            head.UInt16(1);            // majorVersion
            head.UInt16(0);            // minorVersion
            head.UInt32(0);            // fontRevision
            head.UInt32(0);            // checkSumAdjustment
            head.UInt32(0x5F0F3CF5);   // magicNumber
            head.UInt16(0);            // flags
            head.UInt16(1000);         // unitsPerEm
            head.Zeros(8);             // created
            head.Zeros(8);             // modified
            head.Int16(0).Int16(0).Int16(0).Int16(0); // xMin/yMin/xMax/yMax
            head.UInt16(0);            // macStyle
            head.UInt16(0);            // lowestRecPPEM
            head.Int16(0);             // fontDirectionHint
            head.Int16(1);             // indexToLocFormat = 1 (long offsets)
            head.Int16(0);             // glyphDataFormat
            return head.ToArray();
        }

        private static byte[] BuildLongLoca(params uint[] offsets)
        {
            var loca = new BigEndianBuffer();
            foreach (var offset in offsets)
            {
                loca.UInt32(offset);
            }
            return loca.ToArray();
        }

        private static byte[] BuildCompositeHeaderOnly()
            => new BigEndianBuffer()
                .Int16(-1)                                 // numberOfContours < 0 → composite
                .Int16(0).Int16(0).Int16(100).Int16(100)   // bbox; no component records follow
                .ToArray();

        private sealed class NullGeometryContext : IGeometryContext
        {
            public void BeginFigure(Point startPoint, bool isFilled = true) { }
            public void LineTo(Point point, bool isStroked = true) { }
            public void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked = true) { }
            public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked = true) { }
            public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked = true) { }
            public void EndFigure(bool isClosed) { }
            public void SetFillRule(FillRule fillRule) { }
            public void Dispose() { }
        }
    }
}
