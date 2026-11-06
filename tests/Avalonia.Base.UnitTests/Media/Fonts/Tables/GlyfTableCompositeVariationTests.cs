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
    /// A composite glyph's own gvar entry carries one delta per component, which moves that
    /// component's x/y offset, followed by the four phantom points. These tests cover that
    /// composite-level variation on top of the per-component outline deltas.
    /// </summary>
    public class GlyfTableCompositeVariationTests
    {
        private const string InterVariableAsset =
            "resm:Avalonia.Base.UnitTests.Assets.InterVariable.ttf?assembly=Avalonia.Base.UnitTests";

        // Inter Variable declares opsz then wght; wght = 900 normalizes (through avar) to 1.
        private static readonly float[] s_interBlack = { 0f, 1f };

        // Offsets are the component placements stored in Inter's glyf table plus the component
        // deltas of the composite's own gvar entry at wght = 900, as computed by fontTools:
        // Aacute places acutecomb at (459, 372) with a +47 x delta, ccedilla places uni0327 at
        // (347, 0) with a -155 x delta.
        [Theory]
        [InlineData('Á', 'A', '́', 459 + 47, 372)]
        [InlineData('ç', 'c', '̧', 347 - 155, 0)]
        public void Composite_Component_Offset_Follows_Composite_Gvar_Deltas(
            char composite, char baseChar, char markChar, double offsetX, double offsetY)
        {
            var typeface = LoadTypeface(InterVariableAsset);
            var glyf = LoadGlyf(typeface);
            Assert.True(GvarTable.TryLoad(typeface, 2, typeface.GlyphCount, out var gvar));

            var map = typeface.CharacterToGlyphMap;

            var baseOutline = new FigureRecordingContext();
            Assert.True(glyf.TryBuildGlyphGeometry(map[baseChar], Matrix.Identity, baseOutline, gvar, s_interBlack));

            var markOutline = new FigureRecordingContext();
            Assert.True(glyf.TryBuildGlyphGeometry(map[markChar], Matrix.Identity, markOutline, gvar, s_interBlack));

            var compositeOutline = new FigureRecordingContext();
            Assert.True(glyf.TryBuildGlyphGeometry(map[composite], Matrix.Identity, compositeOutline, gvar, s_interBlack));

            // The base component sits at (0, 0) with no delta; the mark's figures follow it.
            Assert.Equal(baseOutline.Figures.Count + markOutline.Figures.Count, compositeOutline.Figures.Count);
            AssertClose(baseOutline.Figures[0][0], compositeOutline.Figures[0][0]);

            var markStart = markOutline.Figures[0][0];
            AssertClose(
                new Point(markStart.X + offsetX, markStart.Y + offsetY),
                compositeOutline.Figures[baseOutline.Figures.Count][0]);
        }

        [Fact]
        public void PointMatched_Composite_Applies_Component_And_Composite_Gvar_Deltas()
        {
            // The base square's point 2 (100, 100) moves by (+20, +10), and the composite's own
            // entry moves the base component's x/y offset by (+5, 0). The accent is matched onto
            // the base's point 2, so it lands on the fully varied point at (125, 110).
            var glyf = BuildPointMatchedGlyf();
            var gvar = BuildGvar(
                glyphCount: 3,
                (BaseGlyph, new short[] { 0, 0, 20, 0, 0, 0, 0, 0 }, new short[] { 0, 0, 10, 0, 0, 0, 0, 0 }),
                (CompositeGlyph, new short[] { 5, 0, 0, 0, 0, 0 }, new short[] { 0, 0, 0, 0, 0, 0 }));

            var context = new FigureRecordingContext();

            Assert.True(glyf.TryBuildGlyphGeometry(CompositeGlyph, Matrix.Identity, context, gvar, new[] { 1f }));

            Assert.Equal(2, context.Figures.Count);

            // Base: (0,0) shifted by the offset delta, and the varied corner.
            AssertClose(new Point(5, 0), context.Figures[0][0]);
            Assert.Contains(context.Figures[0], p => IsClose(p, new Point(125, 110)));

            // Accent: its origin is matched onto the varied corner, so its far corner is 10 beyond it.
            AssertClose(new Point(125, 110), context.Figures[1][0]);
            Assert.Contains(context.Figures[1], p => IsClose(p, new Point(135, 120)));
        }

        // --- Inter --------------------------------------------------------------------------------

        private static GlyphTypeface LoadTypeface(string assetUri)
        {
            var assetLoader = new StandardAssetLoader();
            using var stream = assetLoader.Open(new Uri(assetUri));
            return new GlyphTypeface(new CustomPlatformTypeface(stream));
        }

        private static GlyfTable LoadGlyf(GlyphTypeface typeface)
        {
            Assert.True(HeadTable.TryLoad(typeface, out var head));
            var maxp = MaxpTable.Load(typeface);
            Assert.True(GlyfTable.TryLoad(typeface, head!, maxp, out var glyf));
            return glyf!;
        }

        // --- synthetic point-matched composite ---------------------------------------------------

        // glyph 0: 100x100 square, glyph 1: 10x10 square, glyph 2: composite of the base by x/y
        // offset (0, 0) and the accent with its point 0 matched onto the base's point 2.
        private const int BaseGlyph = 0;
        private const int AccentGlyph = 1;
        private const int CompositeGlyph = 2;

        private static GlyfTable BuildPointMatchedGlyf()
        {
            var glyph0 = PadToEven(BuildSimpleSquare(100));
            var glyph1 = PadToEven(BuildSimpleSquare(10));
            var glyph2 = PadToEven(BuildPointMatchedComposite());

            var glyf = new List<byte>();
            glyf.AddRange(glyph0);
            glyf.AddRange(glyph1);
            glyf.AddRange(glyph2);

            var loca = new List<byte>();
            WriteU16(loca, 0);
            WriteU16(loca, (ushort)(glyph0.Length / 2));
            WriteU16(loca, (ushort)((glyph0.Length + glyph1.Length) / 2));
            WriteU16(loca, (ushort)((glyph0.Length + glyph1.Length + glyph2.Length) / 2));

            var locaTable = Construct<LocaTable>(
                new[] { typeof(ReadOnlyMemory<byte>), typeof(int), typeof(bool) },
                (ReadOnlyMemory<byte>)loca.ToArray(), 3, true);

            return Construct<GlyfTable>(
                new[] { typeof(ReadOnlyMemory<byte>), typeof(LocaTable) },
                (ReadOnlyMemory<byte>)glyf.ToArray(), locaTable);
        }

        private static byte[] BuildSimpleSquare(short size)
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

            // Points (0,0), (size,0), (size,size), (0,size) as int16 deltas.
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

        private static byte[] BuildPointMatchedComposite()
        {
            var data = new List<byte>();

            WriteI16(data, -1);
            WriteI16(data, 0);
            WriteI16(data, 0);
            WriteI16(data, 110);
            WriteI16(data, 110);

            WriteU16(data, (ushort)(CompositeFlags.ArgsAreXYValues | CompositeFlags.MoreComponents));
            WriteU16(data, BaseGlyph);
            data.Add(0);
            data.Add(0);

            WriteU16(data, 0);
            WriteU16(data, AccentGlyph);
            data.Add(2); // parent point
            data.Add(0); // component point

            return data.ToArray();
        }

        /// <summary>
        /// Builds a single-axis gvar table in which each listed glyph has one tuple with an embedded
        /// peak at +1 that applies to all points, carrying the given x and y deltas (contour points
        /// or components first, then the four phantom points).
        /// </summary>
        private static GvarTable BuildGvar(int glyphCount, params (int Glyph, short[] X, short[] Y)[] entries)
        {
            var entryData = new byte[glyphCount][];

            foreach (var (glyph, x, y) in entries)
            {
                var serialized = new List<byte>();
                WritePackedWordDeltas(serialized, x);
                WritePackedWordDeltas(serialized, y);

                var entry = new List<byte>();
                WriteU16(entry, 1);                         // tupleVariationCount, no shared points
                WriteU16(entry, 10);                        // offset to serialized data
                WriteU16(entry, (ushort)serialized.Count);  // variationDataSize
                WriteU16(entry, 0x8000);                    // tupleIndex: embedded peak
                WriteU16(entry, 0x4000);                    // peak = 1.0 (F2Dot14)
                entry.AddRange(serialized);

                entryData[glyph] = PadToEven(entry.ToArray());
            }

            const int headerSize = 20;
            var offsetArraySize = (glyphCount + 1) * 2;
            var dataArrayStart = headerSize + offsetArraySize;

            var table = new List<byte>();
            WriteU16(table, 1);                                 // majorVersion
            WriteU16(table, 0);                                 // minorVersion
            WriteU16(table, 1);                                 // axisCount
            WriteU16(table, 0);                                 // sharedTupleCount
            WriteU32(table, (uint)dataArrayStart);              // sharedTuplesOffset
            WriteU16(table, (ushort)glyphCount);
            WriteU16(table, 0);                                 // flags: short offsets
            WriteU32(table, (uint)dataArrayStart);              // glyphVariationDataArrayOffset

            var offset = 0;
            for (var i = 0; i <= glyphCount; i++)
            {
                WriteU16(table, (ushort)(offset / 2));
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
                (ReadOnlyMemory<byte>)table.ToArray(), 1, glyphCount, false,
                headerSize, dataArrayStart, dataArrayStart, 0);
        }

        private static void WritePackedWordDeltas(List<byte> data, short[] deltas)
        {
            data.Add((byte)(0x40 | (deltas.Length - 1)));   // DELTAS_ARE_WORDS, run of all values

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

        private static void WriteU32(List<byte> data, uint value)
        {
            WriteU16(data, (ushort)(value >> 16));
            WriteU16(data, (ushort)(value & 0xFFFF));
        }

        private static bool IsClose(Point actual, Point expected)
            => Math.Abs(actual.X - expected.X) < 0.001 && Math.Abs(actual.Y - expected.Y) < 0.001;

        private static void AssertClose(Point expected, Point actual)
            => Assert.True(IsClose(actual, expected), $"Expected {expected}, got {actual}.");

        private sealed class FigureRecordingContext : IGeometryContext
        {
            private List<Point>? _current;

            public List<List<Point>> Figures { get; } = new();

            public void BeginFigure(Point startPoint, bool isFilled = true)
            {
                _current = new List<Point> { startPoint };
                Figures.Add(_current);
            }

            public void LineTo(Point point, bool isStroked = true) => _current?.Add(point);

            public void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked = true)
            {
                _current?.Add(controlPoint);
                _current?.Add(endPoint);
            }

            public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked = true)
            {
                _current?.Add(controlPoint1);
                _current?.Add(controlPoint2);
                _current?.Add(endPoint);
            }

            public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc,
                SweepDirection sweepDirection, bool isStroked = true) => _current?.Add(point);

            public void EndFigure(bool isClosed) => _current = null;

            public void SetFillRule(FillRule fillRule) { }

            public void Dispose() { }
        }
    }
}
