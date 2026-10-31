using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Media.Fonts.Tables;
using Avalonia.Media.Fonts.Tables.Glyf;
using Avalonia.Media.Fonts.Tables.Variation;
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

        // --- glyf builders ------------------------------------------------------------------------

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
    }
}
