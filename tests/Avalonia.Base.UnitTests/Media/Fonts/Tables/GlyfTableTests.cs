using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Fonts.Tables;
using Avalonia.Media.Fonts.Tables.Glyf;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    public class GlyfTableTests
    {
        private const string InterFontUri =
            "resm:Avalonia.Base.UnitTests.Assets.Inter-Regular.ttf?assembly=Avalonia.Base.UnitTests";

        private static GlyphTypeface LoadInter()
        {
            var assetLoader = new StandardAssetLoader();
            using var stream = assetLoader.Open(new Uri(InterFontUri));
            return new GlyphTypeface(new CustomPlatformTypeface(stream));
        }

        private static GlyfTable LoadGlyf(GlyphTypeface typeface)
        {
            Assert.True(HeadTable.TryLoad(typeface, out var head));
            var maxp = MaxpTable.Load(typeface);
            Assert.True(GlyfTable.TryLoad(typeface, head!, maxp, out var glyf));
            Assert.NotNull(glyf);
            return glyf!;
        }

        private static ushort GlyphFor(GlyphTypeface typeface, char c)
        {
            var map = typeface.CharacterToGlyphMap;
            Assert.True(map.ContainsGlyph(c));
            return map[c];
        }

        [Fact]
        public void TryLoad_Succeeds_For_Inter()
        {
            var glyf = LoadGlyf(LoadInter());

            Assert.True(glyf.GlyphCount > 0);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        public void TryGetGlyphData_Returns_False_For_Out_Of_Range(int glyphIndex)
        {
            var glyf = LoadGlyf(LoadInter());

            Assert.False(glyf.TryGetGlyphData(glyphIndex, out var data));
            Assert.True(data.IsEmpty);
        }

        [Fact]
        public void TryGetGlyphData_Returns_Empty_For_Space_Glyph()
        {
            var typeface = LoadInter();
            var glyf = LoadGlyf(typeface);

            var spaceGlyph = GlyphFor(typeface, ' ');

            // An empty glyph is a valid glyph with no outline data.
            Assert.True(glyf.TryGetGlyphData(spaceGlyph, out var data));
            Assert.True(data.IsEmpty);
        }

        [Fact]
        public void TryGetGlyphData_Returns_Data_For_Letter()
        {
            var typeface = LoadInter();
            var glyf = LoadGlyf(typeface);

            var letterGlyph = GlyphFor(typeface, 'A');

            Assert.True(glyf.TryGetGlyphData(letterGlyph, out var data));
            Assert.False(data.IsEmpty);

            // At minimum a glyph carries its 10-byte header (numberOfContours + bbox).
            Assert.True(data.Length >= 10);
        }

        [Fact]
        public void TryBuildGlyphGeometry_Builds_Closed_Figures_For_Letter()
        {
            var typeface = LoadInter();
            var glyf = LoadGlyf(typeface);

            var letterGlyph = GlyphFor(typeface, 'A');

            var context = new RecordingGeometryContext();

            Assert.True(glyf.TryBuildGlyphGeometry(letterGlyph, Matrix.Identity, context));

            Assert.True(context.BeginFigureCount >= 1, "Expected at least one contour.");
            Assert.Equal(context.BeginFigureCount, context.EndFigureCount);
            Assert.True(context.AllFiguresClosed, "Glyph contours must be closed.");
            Assert.NotEmpty(context.Points);
        }

        [Fact]
        public void TryBuildGlyphGeometry_Returns_False_For_Empty_Glyph()
        {
            var typeface = LoadInter();
            var glyf = LoadGlyf(typeface);

            var spaceGlyph = GlyphFor(typeface, ' ');

            var context = new RecordingGeometryContext();

            // Nothing to draw for an empty glyph.
            Assert.False(glyf.TryBuildGlyphGeometry(spaceGlyph, Matrix.Identity, context));
            Assert.Equal(0, context.BeginFigureCount);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        public void TryBuildGlyphGeometry_Returns_False_For_Out_Of_Range(int glyphIndex)
        {
            var glyf = LoadGlyf(LoadInter());

            var context = new RecordingGeometryContext();

            Assert.False(glyf.TryBuildGlyphGeometry(glyphIndex, Matrix.Identity, context));
            Assert.Equal(0, context.BeginFigureCount);
        }

        [Fact]
        public void TryBuildGlyphGeometry_Applies_Transform()
        {
            var typeface = LoadInter();
            var glyf = LoadGlyf(typeface);

            var letterGlyph = GlyphFor(typeface, 'A');

            var identity = new RecordingGeometryContext();
            Assert.True(glyf.TryBuildGlyphGeometry(letterGlyph, Matrix.Identity, identity));

            var scaled = new RecordingGeometryContext();
            Assert.True(glyf.TryBuildGlyphGeometry(letterGlyph, Matrix.CreateScale(2, 2), scaled));

            // Same glyph, same decode order: scaling the transform by 2 must scale every
            // emitted point by 2.
            Assert.Equal(identity.Points.Count, scaled.Points.Count);

            var sawNonZero = false;

            for (var i = 0; i < identity.Points.Count; i++)
            {
                var a = identity.Points[i];
                var b = scaled.Points[i];

                Assert.Equal(a.X * 2, b.X, 3);
                Assert.Equal(a.Y * 2, b.Y, 3);

                if (Math.Abs(a.X) > 0.001 || Math.Abs(a.Y) > 0.001)
                {
                    sawNonZero = true;
                }
            }

            Assert.True(sawNonZero, "Expected the glyph to emit non-zero coordinates.");
        }

        [Fact]
        public void TryBuildGlyphGeometry_Builds_Composite_Glyph()
        {
            var typeface = LoadInter();
            var glyf = LoadGlyf(typeface);

            var compositeGlyph = FindCompositeGlyph(glyf);

            // Inter builds its accented characters from components; there should be at
            // least one composite glyph to exercise the recursive build path.
            Assert.True(compositeGlyph >= 0, "Expected Inter to contain at least one composite glyph.");

            var context = new RecordingGeometryContext();

            Assert.True(glyf.TryBuildGlyphGeometry(compositeGlyph, Matrix.Identity, context));

            // A composite expands into its components' contours.
            Assert.True(context.BeginFigureCount >= 1);
            Assert.Equal(context.BeginFigureCount, context.EndFigureCount);
            Assert.True(context.AllFiguresClosed);
        }

        private static int FindCompositeGlyph(GlyfTable glyf)
        {
            for (var i = 0; i < glyf.GlyphCount; i++)
            {
                if (glyf.TryGetGlyphData(i, out var data) && data.Length >= 2)
                {
                    var numberOfContours = BinaryPrimitives.ReadInt16BigEndian(data.Span.Slice(0, 2));

                    // Negative contour count marks a composite glyph.
                    if (numberOfContours < 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private sealed class RecordingGeometryContext : IGeometryContext
        {
            public int BeginFigureCount { get; private set; }
            public int EndFigureCount { get; private set; }
            public bool AllFiguresClosed { get; private set; } = true;
            public List<Point> Points { get; } = new();

            public void BeginFigure(Point startPoint, bool isFilled = true)
            {
                BeginFigureCount++;
                Points.Add(startPoint);
            }

            public void LineTo(Point point, bool isStroked = true) => Points.Add(point);

            public void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked = true)
            {
                Points.Add(controlPoint);
                Points.Add(endPoint);
            }

            public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked = true)
            {
                Points.Add(controlPoint1);
                Points.Add(controlPoint2);
                Points.Add(endPoint);
            }

            public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc,
                SweepDirection sweepDirection, bool isStroked = true) => Points.Add(point);

            public void EndFigure(bool isClosed)
            {
                EndFigureCount++;

                if (!isClosed)
                {
                    AllFiguresClosed = false;
                }
            }

            public void SetFillRule(FillRule fillRule) { }

            public void Dispose() { }
        }
    }
}
