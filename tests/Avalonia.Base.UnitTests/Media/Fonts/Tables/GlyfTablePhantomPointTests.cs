using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Media;
using Avalonia.Media.Fonts.Tables;
using Avalonia.Media.Fonts.Tables.Glyf;
using Avalonia.Media.Fonts.Tables.Variation;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    /// <summary>
    /// Phantom point resolution on synthetic single-axis glyf / gvar tables: the four points gvar
    /// appends after a glyph's contour points (or components), and the USE_MY_METRICS chain that
    /// decides whose phantom points a composite uses.
    /// </summary>
    public class GlyfTablePhantomPointTests
    {
        private static readonly float[] s_atPeak = { 1f };

        [Fact]
        public void Composite_Record_Cut_Off_Before_Its_Arguments_Is_Malformed()
        {
            // numberOfContours = -1, a bounding box, then a component with byte arguments whose
            // record ends after its glyph index.
            var truncated = new List<byte>();
            WriteI16(truncated, -1);
            WriteI16(truncated, 0);
            WriteI16(truncated, 0);
            WriteI16(truncated, 100);
            WriteI16(truncated, 100);
            WriteU16(truncated, (ushort)CompositeFlags.ArgsAreXYValues);
            WriteU16(truncated, 0);

            var glyf = BuildGlyf(Square(100), truncated.ToArray());

            var x = new float[4];
            var y = new float[4];

            Assert.False(glyf.TryGetPhantomDeltas(1, null, default, out _, x, y));
            Assert.False(glyf.TryGetPhantomDeltas(1, BuildGvar(2), s_atPeak, out _, x, y));
        }

        [Fact]
        public void Tuple_Naming_A_Phantom_Point_Applies_To_A_Glyph_Without_Contour_Points()
        {
            // A glyph without contour points has only its phantom points, so point 0 is pp1.
            var gvar = BuildGvar(1, (0, Entry(null, PrivatePointsTuple(new[] { 0 }, new short[] { 5 }, new short[] { 0 }))));

            Assert.True(GlyphVariationReader.TryApplyDeltas(gvar, 0, s_atPeak, default, default, default, default, default));
        }

        [Fact]
        public void PointMatched_Composite_Keeps_Its_Outline_When_A_Component_Without_Contours_Varies_A_Phantom_Point()
        {
            // glyph 0: 100x100 square, glyph 1: no contours, glyph 2: 10x10 square, glyph 3: glyph 0
            // and glyph 1 at (0, 0), then glyph 2 matched by its point 0 onto point 2 of glyph 0.
            var composite = new List<byte>();
            WriteI16(composite, -1);
            WriteI16(composite, 0);
            WriteI16(composite, 0);
            WriteI16(composite, 110);
            WriteI16(composite, 110);
            WriteComponent(composite, CompositeFlags.ArgsAreXYValues | CompositeFlags.MoreComponents, 0, 0, 0);
            WriteComponent(composite, CompositeFlags.ArgsAreXYValues | CompositeFlags.MoreComponents, 1, 0, 0);
            WriteComponent(composite, 0, 2, 2, 0);

            var glyf = BuildGlyf(Square(100), NoContours(), Square(10), composite.ToArray());
            var gvar = BuildGvar(4, (1, Entry(null, PrivatePointsTuple(new[] { 0 }, new short[] { 5 }, new short[] { 0 }))));

            var context = new FigureCountingContext();

            Assert.True(glyf.TryBuildGlyphGeometry(3, Matrix.Identity, context, gvar, s_atPeak));
            Assert.Equal(2, context.FigureCount);
        }

        [Fact]
        public void Shared_Point_Numbers_Carry_Phantom_Deltas()
        {
            // The square has points 0-3, so points 4 and 5 are pp1 and pp2.
            var gvar = BuildGvar(1, (0, Entry(
                new[] { 1, 4, 5 },
                SharedPointsTuple(new short[] { 3, -10, 30 }, new short[] { 9, 0, 7 }))));

            AssertPhantomDeltas(BuildGlyf(Square(100)), gvar, 0, 0, new float[] { -10, 30, 0, 0 }, new float[] { 0, 7, 0, 0 });
        }

        [Fact]
        public void Private_Point_Numbers_Carry_Phantom_Deltas_Without_Interpolation()
        {
            // Contour points 0 and 2 are referenced too; pp1 and pp2 are not and stay at zero, as
            // IUP only interpolates along contours.
            var gvar = BuildGvar(1, (0, Entry(
                null,
                PrivatePointsTuple(new[] { 0, 2, 6, 7 }, new short[] { 1, 2, 3, 4 }, new short[] { 5, 6, 7, 8 }))));

            AssertPhantomDeltas(BuildGlyf(Square(100)), gvar, 0, 0, new float[] { 0, 0, 3, 4 }, new float[] { 0, 0, 7, 8 });
        }

        [Fact]
        public void All_Points_Tuple_Carries_Phantom_Deltas_After_The_Contour_Points()
        {
            var gvar = BuildGvar(1, (0, Entry(
                null,
                SharedPointsTuple(new short[] { 1, 2, 3, 4, -5, 6, 7, 8 }, new short[] { 0, 0, 0, 0, 0, 0, 9, -10 }))));

            AssertPhantomDeltas(BuildGlyf(Square(100)), gvar, 0, 0, new float[] { -5, 6, 7, 8 }, new float[] { 0, 0, 9, -10 });
        }

        [Fact]
        public void Phantom_Deltas_Of_Several_Tuples_Add_Up()
        {
            var gvar = BuildGvar(1, (0, Entry(
                new[] { 5 },
                SharedPointsTuple(new short[] { 30 }, new short[] { 0 }),
                SharedPointsTuple(new short[] { 12 }, new short[] { 0 }))));

            AssertPhantomDeltas(BuildGlyf(Square(100)), gvar, 0, 0, new float[] { 0, 42, 0, 0 }, new float[4]);
        }

        [Fact]
        public void Use_My_Metrics_Chain_Takes_The_Phantom_Deltas_Of_Its_Last_Glyph()
        {
            // glyph 1 takes its metrics from glyph 0, glyph 3 from glyph 1. The composites' own
            // entries move their pp2 (point 3 after two components, point 2 after one) and are
            // ignored.
            var glyf = BuildGlyf(
                Square(100),
                Composite((0, CompositeFlags.UseMyMetrics), (2, 0)),
                Square(10),
                Composite((1, CompositeFlags.UseMyMetrics)));

            var gvar = BuildGvar(
                4,
                (0, Entry(null, PrivatePointsTuple(new[] { 5 }, new short[] { 30 }, new short[] { 0 }))),
                (1, Entry(null, PrivatePointsTuple(new[] { 3 }, new short[] { 99 }, new short[] { 0 }))),
                (3, Entry(null, PrivatePointsTuple(new[] { 2 }, new short[] { 77 }, new short[] { 0 }))));

            var expectedX = new float[] { 0, 30, 0, 0 };

            AssertPhantomDeltas(glyf, gvar, 1, 0, expectedX, new float[4]);
            AssertPhantomDeltas(glyf, gvar, 3, 0, expectedX, new float[4]);
        }

        [Fact]
        public void Use_My_Metrics_Cycle_Is_Malformed()
        {
            var glyf = BuildGlyf(
                Square(100),
                Composite((2, CompositeFlags.UseMyMetrics)),
                Composite((1, CompositeFlags.UseMyMetrics)));

            Assert.False(glyf.TryGetPhantomDeltas(1, null, default, out _, new float[4], new float[4]));
            Assert.False(glyf.TryGetPhantomDeltas(2, BuildGvar(3), s_atPeak, out _, new float[4], new float[4]));
        }

        [Theory]
        [InlineData(GlyphDecycler.MaxTraversalDepth - 1, true)]
        [InlineData(GlyphDecycler.MaxTraversalDepth, false)]
        public void Use_My_Metrics_Chain_Ends_Within_The_Traversal_Depth(int compositeCount, bool resolves)
        {
            // Glyph 0 is a square; glyph i takes its metrics from glyph i - 1, so resolving the last
            // glyph walks compositeCount composites and then the square.
            var glyphs = new byte[compositeCount + 1][];
            glyphs[0] = Square(100);

            for (var i = 1; i <= compositeCount; i++)
            {
                glyphs[i] = Composite(((ushort)(i - 1), CompositeFlags.UseMyMetrics));
            }

            var glyf = BuildGlyf(glyphs);

            Assert.Equal(resolves, glyf.TryGetPhantomDeltas(compositeCount, null, default, out var metricsGlyph, new float[4], new float[4]));

            if (resolves)
            {
                Assert.Equal(0, metricsGlyph);
            }
        }

        private static void AssertPhantomDeltas(
            GlyfTable glyf, GvarTable gvar, int glyph, int expectedMetricsGlyph, float[] expectedX, float[] expectedY)
        {
            var x = new float[4];
            var y = new float[4];

            Assert.True(glyf.TryGetPhantomDeltas(glyph, gvar, s_atPeak, out var metricsGlyph, x, y));
            Assert.Equal(expectedMetricsGlyph, metricsGlyph);
            Assert.Equal(expectedX, x);
            Assert.Equal(expectedY, y);

            if (glyph == 0)
            {
                // The reader on its own, given the square's four contour points.
                var readerX = new float[4];
                var readerY = new float[4];

                GlyphVariationReader.TryGetPhantomDeltas(gvar, glyph, s_atPeak, 4, readerX, readerY);

                Assert.Equal(expectedX, readerX);
                Assert.Equal(expectedY, readerY);
            }
        }

        // --- glyf builders ------------------------------------------------------------------------

        /// <summary>A composite whose components all sit at (0, 0) by x/y offset.</summary>
        private static byte[] Composite(params (ushort Glyph, CompositeFlags Flags)[] components)
        {
            var data = new List<byte>();
            WriteI16(data, -1);
            WriteI16(data, 0);
            WriteI16(data, 0);
            WriteI16(data, 100);
            WriteI16(data, 100);

            for (var i = 0; i < components.Length; i++)
            {
                var flags = components[i].Flags | CompositeFlags.ArgsAreXYValues;

                if (i < components.Length - 1)
                {
                    flags |= CompositeFlags.MoreComponents;
                }

                WriteComponent(data, flags, components[i].Glyph, 0, 0);
            }

            return data.ToArray();
        }

        private static GlyfTable BuildGlyf(params byte[][] glyphs)
        {
            var glyf = new List<byte>();
            var loca = new List<byte>();

            foreach (var glyph in glyphs)
            {
                WriteU32(loca, (uint)glyf.Count);
                glyf.AddRange(glyph);

                if ((glyf.Count & 1) != 0)
                {
                    glyf.Add(0);
                }
            }

            WriteU32(loca, (uint)glyf.Count);

            var locaTable = Construct<LocaTable>(
                new[] { typeof(ReadOnlyMemory<byte>), typeof(int), typeof(bool) },
                (ReadOnlyMemory<byte>)loca.ToArray(), glyphs.Length, false);

            return Construct<GlyfTable>(
                new[] { typeof(ReadOnlyMemory<byte>), typeof(LocaTable) },
                (ReadOnlyMemory<byte>)glyf.ToArray(), locaTable);
        }

        /// <summary>A one-contour square from (0, 0) to (size, size): four contour points.</summary>
        private static byte[] Square(short size)
        {
            var data = new List<byte>();

            WriteI16(data, 1);      // numberOfContours
            WriteI16(data, 0);      // xMin
            WriteI16(data, 0);      // yMin
            WriteI16(data, size);   // xMax
            WriteI16(data, size);   // yMax

            WriteU16(data, 3);      // endPtsOfContours[0]
            WriteU16(data, 0);      // instructionLength

            for (var i = 0; i < 4; i++)
            {
                data.Add((byte)GlyphFlag.OnCurvePoint);
            }

            WriteI16(data, 0);
            WriteI16(data, size);
            WriteI16(data, 0);
            WriteI16(data, (short)-size);

            WriteI16(data, 0);
            WriteI16(data, 0);
            WriteI16(data, size);
            WriteI16(data, 0);

            return data.ToArray();
        }

        /// <summary>A simple glyph header with zero contours and no instructions.</summary>
        private static byte[] NoContours()
        {
            var data = new List<byte>();

            WriteI16(data, 0);      // numberOfContours
            WriteI16(data, 0);
            WriteI16(data, 0);
            WriteI16(data, 0);
            WriteI16(data, 0);
            WriteU16(data, 0);      // instructionLength

            return data.ToArray();
        }

        private static void WriteComponent(List<byte> data, CompositeFlags flags, ushort glyph, byte arg1, byte arg2)
        {
            WriteU16(data, (ushort)flags);
            WriteU16(data, glyph);
            data.Add(arg1);
            data.Add(arg2);
        }

        // --- gvar builders ------------------------------------------------------------------------

        /// <summary>
        /// Builds a single-axis gvar table. <paramref name="entries"/> maps a glyph to its raw glyph
        /// variation data; glyphs without an entry have none.
        /// </summary>
        private static GvarTable BuildGvar(int glyphCount, params (int Glyph, byte[] Entry)[] entries)
        {
            var entryData = new byte[glyphCount][];

            foreach (var (glyph, entry) in entries)
            {
                entryData[glyph] = entry;
            }

            const int headerSize = 20;
            var dataArrayStart = headerSize + (glyphCount + 1) * 4;

            var table = new List<byte>();
            WriteU16(table, 1);                                 // majorVersion
            WriteU16(table, 0);                                 // minorVersion
            WriteU16(table, 1);                                 // axisCount
            WriteU16(table, 0);                                 // sharedTupleCount
            WriteU32(table, (uint)dataArrayStart);              // sharedTuplesOffset
            WriteU16(table, (ushort)glyphCount);
            WriteU16(table, 1);                                 // flags: long offsets
            WriteU32(table, (uint)dataArrayStart);              // glyphVariationDataArrayOffset

            var offset = 0;
            for (var i = 0; i <= glyphCount; i++)
            {
                WriteU32(table, (uint)offset);
                if (i < glyphCount)
                {
                    offset += entryData[i]?.Length ?? 0;
                }
            }

            foreach (var data in entryData)
            {
                if (data != null)
                {
                    table.AddRange(data);
                }
            }

            return Construct<GvarTable>(
                new[]
                {
                    typeof(ReadOnlyMemory<byte>), typeof(int), typeof(int), typeof(bool),
                    typeof(int), typeof(int), typeof(int), typeof(int)
                },
                (ReadOnlyMemory<byte>)table.ToArray(), 1, glyphCount, true,
                headerSize, dataArrayStart, dataArrayStart, 0);
        }

        /// <summary>
        /// Builds one glyph's variation data from tuples that each peak at +1 on the single axis.
        /// <paramref name="sharedPoints"/>, when given, is written as the shared point number list
        /// (empty meaning all points).
        /// </summary>
        private static byte[] Entry(int[]? sharedPoints, params (bool PrivatePoints, byte[] Data)[] tuples)
        {
            var serialized = new List<byte>();

            if (sharedPoints is not null)
            {
                WritePackedPoints(serialized, sharedPoints);
            }

            var headers = new List<byte>();

            foreach (var (privatePoints, data) in tuples)
            {
                WriteU16(headers, (ushort)data.Length);                         // variationDataSize
                WriteU16(headers, (ushort)(0x8000 | (privatePoints ? 0x2000 : 0)));
                WriteU16(headers, 0x4000);                                      // peak = 1.0
                serialized.AddRange(data);
            }

            var entry = new List<byte>();
            WriteU16(entry, (ushort)((sharedPoints is not null ? 0x8000 : 0) | tuples.Length));
            WriteU16(entry, (ushort)(4 + headers.Count));                       // offset to serialized data
            entry.AddRange(headers);
            entry.AddRange(serialized);

            if ((entry.Count & 1) != 0)
            {
                entry.Add(0);
            }

            return entry.ToArray();
        }

        /// <summary>A tuple with its own point number list and one x and y delta per listed point.</summary>
        private static (bool, byte[]) PrivatePointsTuple(int[] points, short[] x, short[] y)
        {
            var data = new List<byte>();
            WritePackedPoints(data, points);
            WritePackedDeltas(data, x);
            WritePackedDeltas(data, y);

            return (true, data.ToArray());
        }

        /// <summary>
        /// A tuple that uses the shared point numbers, or every point when the entry has none, with
        /// one x and y delta per point.
        /// </summary>
        private static (bool, byte[]) SharedPointsTuple(short[] x, short[] y)
        {
            var data = new List<byte>();
            WritePackedDeltas(data, x);
            WritePackedDeltas(data, y);

            return (false, data.ToArray());
        }

        private static void WritePackedPoints(List<byte> data, int[] points)
        {
            data.Add((byte)points.Length);                      // fewer than 128; 0 means all points

            if (points.Length == 0)
            {
                return;
            }

            data.Add((byte)(0x80 | (points.Length - 1)));       // POINTS_ARE_WORDS, one run

            var previous = 0;
            foreach (var point in points)
            {
                WriteU16(data, (ushort)(point - previous));
                previous = point;
            }
        }

        private static void WritePackedDeltas(List<byte> data, short[] deltas)
        {
            data.Add((byte)(0x40 | (deltas.Length - 1)));       // DELTAS_ARE_WORDS, one run

            foreach (var delta in deltas)
            {
                WriteI16(data, delta);
            }
        }

        private static T Construct<T>(Type[] parameterTypes, params object[] arguments)
        {
            var ctor = typeof(T).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic, binder: null, parameterTypes, modifiers: null);

            Assert.NotNull(ctor);

            return (T)ctor!.Invoke(arguments);
        }

        private static void WriteU16(List<byte> data, ushort value)
        {
            data.Add((byte)(value >> 8));
            data.Add((byte)(value & 0xFF));
        }

        private static void WriteI16(List<byte> data, short value) => WriteU16(data, (ushort)value);

        private static void WriteU32(List<byte> data, uint value)
        {
            WriteU16(data, (ushort)(value >> 16));
            WriteU16(data, (ushort)(value & 0xFFFF));
        }

        private sealed class FigureCountingContext : IGeometryContext
        {
            public int FigureCount { get; private set; }

            public void BeginFigure(Point startPoint, bool isFilled = true) => FigureCount++;

            public void LineTo(Point point, bool isStroked = true) { }

            public void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked = true) { }

            public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked = true) { }

            public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc,
                SweepDirection sweepDirection, bool isStroked = true) { }

            public void EndFigure(bool isClosed) { }

            public void SetFillRule(FillRule fillRule) { }

            public void Dispose() { }
        }
    }
}
