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
    /// Exercises the composite point-matching path (ARGS_ARE_XY_VALUES clear) using a hand-built
    /// 'glyf'/'loca' blob. Real-world fonts that use point matching are rare, so the synthetic
    /// fixture is the only reliable way to cover this branch.
    /// </summary>
    public class GlyfTablePointMatchingTests
    {
        // Glyph layout in the synthetic font:
        //   glyph 0: 100x100 square at the origin            -> the "base"
        //   glyph 1: 10x10   square at the origin            -> the point-matched "accent"
        //   glyph 2: composite { base by x/y offset (0,0), accent by point matching }
        private const int BaseGlyph = 0;
        private const int AccentGlyph = 1;
        private const int CompositeGlyph = 2;

        [Fact]
        public void PointMatchedComposite_Aligns_Component_Point_Onto_Parent_Point()
        {
            // Match the accent's point 0 (its local origin) onto the base's point 2 (100, 100).
            var glyf = BuildGlyf(parentPoint: 2, componentPoint: 0);

            var context = new FigureRecordingContext();

            Assert.True(glyf.TryBuildGlyphGeometry(CompositeGlyph, Matrix.Identity, context));

            // One contour from the base, one from the accent.
            Assert.Equal(2, context.Figures.Count);
            Assert.True(context.AllClosed);

            // The base contour is emitted first, unmoved.
            Assert.True(Contains(context.Figures[0], 0, 0));
            Assert.True(Contains(context.Figures[0], 100, 100));

            // The accent contour is translated by (parentPoint - componentPoint) = (100,100) - (0,0).
            // Its figure therefore starts where the matched parent point is.
            Assert.True(Close(context.Figures[1][0], 100, 100));

            // The translated accent reaches (110, 110); those coordinates are impossible for the
            // base (which spans 0..100) so they prove the component actually moved.
            Assert.True(Contains(context.AllPoints, 110, 110));
            Assert.True(Contains(context.AllPoints, 110, 100));
            Assert.True(Contains(context.AllPoints, 100, 110));

            // If point matching were (incorrectly) treated as a zero offset, the accent would stay
            // at the origin and its far corner (10, 10) would appear instead.
            Assert.False(Contains(context.AllPoints, 10, 10));
        }

        [Fact]
        public void PointMatchedComposite_Matching_Different_Parent_Point_Moves_Accent_There()
        {
            // Match the accent's origin onto the base's point 1 (100, 0) instead.
            var glyf = BuildGlyf(parentPoint: 1, componentPoint: 0);

            var context = new FigureRecordingContext();

            Assert.True(glyf.TryBuildGlyphGeometry(CompositeGlyph, Matrix.Identity, context));

            Assert.Equal(2, context.Figures.Count);
            Assert.True(Close(context.Figures[1][0], 100, 0));
            Assert.True(Contains(context.AllPoints, 110, 10));
        }

        [Fact]
        public void PointMatchedComposite_Out_Of_Range_Component_Point_Bails_Out()
        {
            // The accent only has 4 points (0..3); point 99 is out of range.
            var glyf = BuildGlyf(parentPoint: 2, componentPoint: 99);

            var context = new FigureRecordingContext();

            Assert.False(glyf.TryBuildGlyphGeometry(CompositeGlyph, Matrix.Identity, context));

            // Nothing is emitted: the materialised outline is discarded before reaching the context.
            Assert.Empty(context.Figures);
        }

        [Fact]
        public void PointMatchedComposite_Out_Of_Range_Parent_Point_Bails_Out()
        {
            // The base contributes 4 parent points (0..3); point 50 is out of range.
            var glyf = BuildGlyf(parentPoint: 50, componentPoint: 0);

            var context = new FigureRecordingContext();

            Assert.False(glyf.TryBuildGlyphGeometry(CompositeGlyph, Matrix.Identity, context));
            Assert.Empty(context.Figures);
        }

        // --- synthetic font construction -------------------------------------------------------

        private static GlyfTable BuildGlyf(byte parentPoint, byte componentPoint)
        {
            var glyph0 = PadToEven(BuildSimpleSquare(100));
            var glyph1 = PadToEven(BuildSimpleSquare(10));
            var glyph2 = PadToEven(BuildPointMatchedComposite(parentPoint, componentPoint));

            var glyf = new List<byte>();
            glyf.AddRange(glyph0);
            glyf.AddRange(glyph1);
            glyf.AddRange(glyph2);

            // Short 'loca' stores offset / 2 (which is why glyphs are padded to an even length).
            var offsets = new[]
            {
                0,
                glyph0.Length,
                glyph0.Length + glyph1.Length,
                glyph0.Length + glyph1.Length + glyph2.Length
            };

            var loca = new List<byte>();

            foreach (var offset in offsets)
            {
                WriteU16(loca, (ushort)(offset / 2));
            }

            return CreateGlyfTable(glyf.ToArray(), loca.ToArray(), glyphCount: 3);
        }

        /// <summary>
        /// A simple, single-contour square with on-curve corners (0,0), (size,0), (size,size),
        /// (0,size), encoded with int16 coordinate deltas.
        /// </summary>
        private static byte[] BuildSimpleSquare(short size)
        {
            var data = new List<byte>();

            // Glyph header.
            WriteI16(data, 1);      // numberOfContours (> 0 => simple)
            WriteI16(data, 0);      // xMin
            WriteI16(data, 0);      // yMin
            WriteI16(data, size);   // xMax
            WriteI16(data, size);   // yMax

            // Simple glyph body.
            WriteU16(data, 3);      // endPtsOfContours[0] => 4 points
            WriteU16(data, 0);      // instructionLength

            // Flags: 4 on-curve points, coordinates encoded as int16 deltas
            // (XShortVector/YShortVector clear, *IsSame* clear).
            for (var i = 0; i < 4; i++)
            {
                data.Add((byte)GlyphFlag.OnCurvePoint);
            }

            // X deltas: 0, +size, 0, -size.
            WriteI16(data, 0);
            WriteI16(data, size);
            WriteI16(data, 0);
            WriteI16(data, (short)-size);

            // Y deltas: 0, 0, +size, 0.
            WriteI16(data, 0);
            WriteI16(data, 0);
            WriteI16(data, size);
            WriteI16(data, 0);

            return data.ToArray();
        }

        /// <summary>
        /// A composite glyph with two components: the base placed by an x/y offset of (0,0), and the
        /// accent placed by point matching (ARGS_ARE_XY_VALUES clear).
        /// </summary>
        private static byte[] BuildPointMatchedComposite(byte parentPoint, byte componentPoint)
        {
            var data = new List<byte>();

            // Glyph header (numberOfContours < 0 => composite).
            WriteI16(data, -1);
            WriteI16(data, 0);
            WriteI16(data, 0);
            WriteI16(data, 110);
            WriteI16(data, 110);

            // Component 0: the base, placed by x/y offset (0,0). Byte args.
            WriteU16(data, (ushort)(CompositeFlags.ArgsAreXYValues | CompositeFlags.MoreComponents));
            WriteU16(data, BaseGlyph);
            data.Add(0); // arg1 = x offset 0
            data.Add(0); // arg2 = y offset 0

            // Component 1: the accent, placed by point matching. Byte args, no more components.
            WriteU16(data, 0); // flags: ArgsAreWords clear, ArgsAreXYValues clear, MoreComponents clear
            WriteU16(data, AccentGlyph);
            data.Add(parentPoint);    // arg1 = point of the already-assembled glyph
            data.Add(componentPoint); // arg2 = point of this component

            return data.ToArray();
        }

        private static byte[] PadToEven(byte[] data)
        {
            if ((data.Length & 1) == 0)
            {
                return data;
            }

            var padded = new byte[data.Length + 1];
            Array.Copy(data, padded, data.Length);
            return padded;
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

        private static bool Close(Point point, double x, double y)
            => Math.Abs(point.X - x) < 0.001 && Math.Abs(point.Y - y) < 0.001;

        private static bool Contains(IEnumerable<Point> points, double x, double y)
        {
            foreach (var point in points)
            {
                if (Close(point, x, y))
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class FigureRecordingContext : IGeometryContext
        {
            private List<Point>? _current;

            public List<List<Point>> Figures { get; } = new();

            public List<Point> AllPoints { get; } = new();

            public bool AllClosed { get; private set; } = true;

            public void BeginFigure(Point startPoint, bool isFilled = true)
            {
                _current = new List<Point> { startPoint };
                Figures.Add(_current);
                AllPoints.Add(startPoint);
            }

            public void LineTo(Point point, bool isStroked = true)
            {
                _current?.Add(point);
                AllPoints.Add(point);
            }

            public void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked = true)
            {
                _current?.Add(controlPoint);
                _current?.Add(endPoint);
                AllPoints.Add(controlPoint);
                AllPoints.Add(endPoint);
            }

            public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked = true)
            {
                _current?.Add(controlPoint1);
                _current?.Add(controlPoint2);
                _current?.Add(endPoint);
                AllPoints.Add(controlPoint1);
                AllPoints.Add(controlPoint2);
                AllPoints.Add(endPoint);
            }

            public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc,
                SweepDirection sweepDirection, bool isStroked = true)
            {
                _current?.Add(point);
                AllPoints.Add(point);
            }

            public void EndFigure(bool isClosed)
            {
                if (!isClosed)
                {
                    AllClosed = false;
                }

                _current = null;
            }

            public void SetFillRule(FillRule fillRule) { }

            public void Dispose() { }
        }
    }
}
