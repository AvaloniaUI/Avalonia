using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Media;
using Avalonia.Media.Fonts.Tables;
using Avalonia.Media.Fonts.Tables.Glyf;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    /// <summary>
    /// Exercises the on/off-curve contour walk of the glyph geometry builder against hand-built
    /// simple glyphs, pinning the TrueType decomposition rules: an implied on-curve midpoint
    /// between consecutive off-curve points, and the figure-start selection when the first
    /// point is off-curve (the last point when that one is on-curve, otherwise the implied
    /// midpoint between last and first).
    /// </summary>
    public class GlyfTableContourWalkTests
    {
        [Fact]
        public void All_Off_Curve_Contour_Emits_A_Quadratic_Per_Point()
        {
            // A "quadratic circle": every point is an off-curve control; each consecutive pair
            // implies an on-curve midpoint, so the contour decomposes into exactly one
            // quadratic segment per input point, closing back to the figure start.
            var glyf = BuildSingleGlyphTable(
                (100, 0, false),
                (0, 100, false),
                (-100, 0, false),
                (0, -100, false));

            var context = new SegmentRecordingContext();

            Assert.True(glyf.TryBuildGlyphGeometry(0, Matrix.Identity, context));

            Assert.Equal(new Point(50, -50), context.FigureStart);
            Assert.Empty(context.Lines);
            Assert.Equal(4, context.Quadratics.Count);

            Assert.Equal(new Point(100, 0), context.Quadratics[0].Control);
            Assert.Equal(new Point(50, 50), context.Quadratics[0].End);
            Assert.Equal(new Point(0, 100), context.Quadratics[1].Control);
            Assert.Equal(new Point(-50, 50), context.Quadratics[1].End);
            Assert.Equal(new Point(-100, 0), context.Quadratics[2].Control);
            Assert.Equal(new Point(-50, -50), context.Quadratics[2].End);

            // The trailing off-curve point closes the contour back to the figure start.
            Assert.Equal(new Point(0, -100), context.Quadratics[3].Control);
            Assert.Equal(new Point(50, -50), context.Quadratics[3].End);
        }

        [Fact]
        public void Off_Curve_First_Point_Starts_The_Figure_At_The_On_Curve_Last_Point()
        {
            var glyf = BuildSingleGlyphTable(
                (100, 100, false),
                (0, 0, true),
                (200, 0, true));

            var context = new SegmentRecordingContext();

            Assert.True(glyf.TryBuildGlyphGeometry(0, Matrix.Identity, context));

            // The last point is on-curve, so the contour starts there — an implied midpoint
            // between an off-curve and an on-curve point is not a point of the curve and
            // would add a spurious vertex.
            Assert.Equal(new Point(200, 0), context.FigureStart);

            Assert.Equal(1, context.Quadratics.Count);
            Assert.Equal(new Point(100, 100), context.Quadratics[0].Control);
            Assert.Equal(new Point(0, 0), context.Quadratics[0].End);

            // The two on-curve points connect with the closing line.
            Assert.Single(context.Lines);
            Assert.Equal(new Point(200, 0), context.Lines[0]);
        }

        [Fact]
        public void On_Curve_Start_With_Off_Curve_Run_Splits_At_Implied_Midpoints()
        {
            // Pins the already-correct on-curve-start walk so the unified walker cannot drift.
            var glyf = BuildSingleGlyphTable(
                (0, 0, true),
                (100, 0, false),
                (100, 100, false),
                (0, 100, true));

            var context = new SegmentRecordingContext();

            Assert.True(glyf.TryBuildGlyphGeometry(0, Matrix.Identity, context));

            Assert.Equal(new Point(0, 0), context.FigureStart);
            Assert.Equal(2, context.Quadratics.Count);
            Assert.Equal(new Point(100, 0), context.Quadratics[0].Control);
            Assert.Equal(new Point(100, 50), context.Quadratics[0].End);
            Assert.Equal(new Point(100, 100), context.Quadratics[1].Control);
            Assert.Equal(new Point(0, 100), context.Quadratics[1].End);

            Assert.Single(context.Lines);
            Assert.Equal(new Point(0, 0), context.Lines[0]);
        }

        // --- synthetic font construction -------------------------------------------------------

        private static GlyfTable BuildSingleGlyphTable(params (short X, short Y, bool OnCurve)[] points)
        {
            var data = new List<byte>();

            // Glyph header; the bounding box is not consumed by the walk.
            WriteI16(data, 1);
            WriteI16(data, 0);
            WriteI16(data, 0);
            WriteI16(data, 0);
            WriteI16(data, 0);

            WriteU16(data, (ushort)(points.Length - 1)); // endPtsOfContours[0]
            WriteU16(data, 0);                           // instructionLength

            // One flag per point, coordinates encoded as int16 deltas
            // (XShortVector/YShortVector clear, *IsSame* clear).
            foreach (var point in points)
            {
                data.Add((byte)(point.OnCurve ? GlyphFlag.OnCurvePoint : GlyphFlag.None));
            }

            short previous = 0;

            foreach (var point in points)
            {
                WriteI16(data, (short)(point.X - previous));
                previous = point.X;
            }

            previous = 0;

            foreach (var point in points)
            {
                WriteI16(data, (short)(point.Y - previous));
                previous = point.Y;
            }

            // Short 'loca' stores offset / 2, so pad the glyph to an even length.
            if ((data.Count & 1) != 0)
            {
                data.Add(0);
            }

            var loca = new List<byte>();
            WriteU16(loca, 0);
            WriteU16(loca, (ushort)(data.Count / 2));

            return CreateGlyfTable(data.ToArray(), loca.ToArray(), glyphCount: 1);
        }

        private static void WriteU16(List<byte> data, ushort value)
        {
            data.Add((byte)(value >> 8));
            data.Add((byte)(value & 0xFF));
        }

        private static void WriteI16(List<byte> data, short value) => WriteU16(data, (ushort)value);

        /// <summary>
        /// Builds a <see cref="GlyfTable"/> directly from raw 'glyf'/'loca' bytes via its internal
        /// constructors, bypassing the full font-load path (which would require a complete TTF).
        /// </summary>
        private static GlyfTable CreateGlyfTable(byte[] glyfData, byte[] locaData, int glyphCount)
        {
            var locaCtor = typeof(LocaTable).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                new[] { typeof(ReadOnlyMemory<byte>), typeof(int), typeof(bool) },
                modifiers: null);

            Assert.NotNull(locaCtor);

            var loca = locaCtor!.Invoke(new object[] { (ReadOnlyMemory<byte>)locaData, glyphCount, /* isShortFormat */ true });

            var glyfCtor = typeof(GlyfTable).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                new[] { typeof(ReadOnlyMemory<byte>), typeof(LocaTable) },
                modifiers: null);

            Assert.NotNull(glyfCtor);

            return (GlyfTable)glyfCtor!.Invoke(new[] { (ReadOnlyMemory<byte>)glyfData, loca! });
        }

        private sealed class SegmentRecordingContext : IGeometryContext
        {
            public Point FigureStart { get; private set; }

            public List<Point> Lines { get; } = new();

            public List<(Point Control, Point End)> Quadratics { get; } = new();

            public void BeginFigure(Point startPoint, bool isFilled = true) => FigureStart = startPoint;

            public void LineTo(Point point, bool isStroked = true) => Lines.Add(point);

            public void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked = true)
                => Quadratics.Add((controlPoint, endPoint));

            public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked = true)
            {
            }

            public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc,
                SweepDirection sweepDirection, bool isStroked = true)
            {
            }

            public void EndFigure(bool isClosed)
            {
            }

            public void SetFillRule(FillRule fillRule)
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
