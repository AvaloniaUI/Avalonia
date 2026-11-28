using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Avalonia.UnitTests
{
    /// <summary>
    /// <see cref="SyntheticFont"/> edits that move a TrueType glyph's left phantom point, which sits
    /// at <c>xMin - lsb</c> plus its gvar delta and decides where rasterizers place the outline.
    /// </summary>
    public static class PhantomPointFixtures
    {
        /// <summary>
        /// Adds <paramref name="amount"/> to a glyph's hmtx left side bearing. The glyph must lie
        /// within numberOfHMetrics, where the bearing follows the advance.
        /// </summary>
        public static SyntheticFont AddToLeftSideBearing(this SyntheticFont font, ushort glyph, short amount)
            => font.Mutate("hmtx", hmtx =>
            {
                var offset = glyph * 4 + 2;
                var lsb = BinaryPrimitives.ReadInt16BigEndian(hmtx.AsSpan(offset));
                BinaryPrimitives.WriteInt16BigEndian(hmtx.AsSpan(offset), (short)(lsb + amount));
            });

        /// <summary>
        /// Replaces the font's gvar with one in which only <paramref name="glyph"/>, a simple glyph,
        /// has an entry: one tuple peaking at +1 on <paramref name="axis"/> whose private point list
        /// references just the left phantom point, moving it by <paramref name="delta"/> in x.
        /// </summary>
        public static SyntheticFont WithLeftPhantomGvar(this SyntheticFont font, ushort glyph, int axis, short delta)
        {
            var axisCount = BinaryPrimitives.ReadUInt16BigEndian(font.GetTable("fvar").AsSpan(8));
            var glyphCount = BinaryPrimitives.ReadUInt16BigEndian(font.GetTable("maxp").AsSpan(4));
            var pointCount = CountSimpleGlyphPoints(font, glyph);

            var serialized = new List<byte>();
            serialized.Add(1);                                      // private point count
            serialized.Add(0x80);                                   // one point number, as a word
            WriteU16(serialized, (ushort)pointCount);               // the left phantom point
            serialized.Add(0x40);                                   // x deltas: one word
            WriteU16(serialized, (ushort)delta);
            serialized.Add(0x80);                                   // y deltas: one zero

            var entry = new List<byte>();
            WriteU16(entry, 1);                                     // tupleVariationCount
            WriteU16(entry, (ushort)(8 + axisCount * 2));           // offset to serialized data
            WriteU16(entry, (ushort)serialized.Count);              // variationDataSize
            WriteU16(entry, 0x8000 | 0x2000);                       // embedded peak, private points
            for (var i = 0; i < axisCount; i++)
            {
                WriteU16(entry, i == axis ? (ushort)0x4000 : (ushort)0);
            }
            entry.AddRange(serialized);

            const int headerSize = 20;
            var dataArrayStart = headerSize + (glyphCount + 1) * 4;

            var table = new List<byte>();
            WriteU16(table, 1);                                     // majorVersion
            WriteU16(table, 0);                                     // minorVersion
            WriteU16(table, axisCount);
            WriteU16(table, 0);                                     // sharedTupleCount
            WriteU32(table, (uint)dataArrayStart);                  // sharedTuplesOffset
            WriteU16(table, glyphCount);
            WriteU16(table, 1);                                     // flags: long offsets
            WriteU32(table, (uint)dataArrayStart);                  // glyphVariationDataArrayOffset

            for (var i = 0; i <= glyphCount; i++)
            {
                WriteU32(table, i > glyph ? (uint)entry.Count : 0);
            }

            table.AddRange(entry);

            return font.Replace("gvar", table.ToArray());
        }

        /// <summary>
        /// Replaces the glyf data of the font's last glyph with <paramref name="glyphData"/>, appended
        /// at the end of glyf. Only the last glyph can change length without moving the others, as
        /// its loca end offset is not another glyph's start.
        /// </summary>
        public static SyntheticFont WithLastGlyphData(this SyntheticFont font, byte[] glyphData, out ushort glyph)
        {
            var glyphCount = BinaryPrimitives.ReadUInt16BigEndian(font.GetTable("maxp").AsSpan(4));
            var shortLoca = BinaryPrimitives.ReadInt16BigEndian(font.GetTable("head").AsSpan(50)) == 0;
            var oldGlyf = font.GetTable("glyf");
            var loca = font.GetTable("loca");

            glyph = (ushort)(glyphCount - 1);

            var start = (oldGlyf.Length + 3) & ~3;
            var glyf = new byte[start + ((glyphData.Length + 3) & ~3)];
            oldGlyf.CopyTo(glyf, 0);
            glyphData.CopyTo(glyf, start);

            var end = start + glyphData.Length;

            if (shortLoca)
            {
                if ((end & 1) != 0 || end / 2 > ushort.MaxValue)
                {
                    throw new ArgumentException("The glyph data does not fit a short loca table.", nameof(glyphData));
                }

                BinaryPrimitives.WriteUInt16BigEndian(loca.AsSpan(glyph * 2), (ushort)(start / 2));
                BinaryPrimitives.WriteUInt16BigEndian(loca.AsSpan(glyph * 2 + 2), (ushort)(end / 2));
            }
            else
            {
                BinaryPrimitives.WriteUInt32BigEndian(loca.AsSpan(glyph * 4), (uint)start);
                BinaryPrimitives.WriteUInt32BigEndian(loca.AsSpan(glyph * 4 + 4), (uint)end);
            }

            return font.Replace("glyf", glyf).Replace("loca", loca);
        }

        /// <summary>
        /// Builds the glyf data of a composite glyph whose single component, with byte arguments,
        /// ends after its glyph index: 14 bytes where the record needs 16.
        /// </summary>
        public static byte[] TruncatedCompositeGlyph(ushort component)
        {
            var data = new List<byte>();
            WriteU16(data, 0xFFFF);                                 // numberOfContours = -1
            WriteU16(data, 0);                                      // xMin
            WriteU16(data, 0);                                      // yMin
            WriteU16(data, 100);                                    // xMax
            WriteU16(data, 100);                                    // yMax
            WriteU16(data, 0x0002);                                 // ARGS_ARE_XY_VALUES, byte args
            WriteU16(data, component);

            return data.ToArray();
        }

        /// <summary>
        /// Builds the glyf data of a composite glyph with one component, placed at (0, 0) and
        /// flagged USE_MY_METRICS.
        /// </summary>
        public static byte[] UseMyMetricsCompositeGlyph(ushort component)
        {
            var data = new List<byte>();
            WriteU16(data, 0xFFFF);                                 // numberOfContours = -1
            WriteU16(data, 0);                                      // xMin
            WriteU16(data, 0);                                      // yMin
            WriteU16(data, 100);                                    // xMax
            WriteU16(data, 100);                                    // yMax
            WriteU16(data, 0x0202);                                 // USE_MY_METRICS, ARGS_ARE_XY_VALUES
            WriteU16(data, component);
            data.Add(0);
            data.Add(0);

            return data.ToArray();
        }

        /// <summary>Reads a simple glyph's point count from its last contour end point.</summary>
        private static int CountSimpleGlyphPoints(SyntheticFont font, ushort glyph)
        {
            var head = font.GetTable("head");
            var loca = font.GetTable("loca");
            var glyf = font.GetTable("glyf");

            var start = BinaryPrimitives.ReadInt16BigEndian(head.AsSpan(50)) == 0
                ? BinaryPrimitives.ReadUInt16BigEndian(loca.AsSpan(glyph * 2)) * 2
                : (int)BinaryPrimitives.ReadUInt32BigEndian(loca.AsSpan(glyph * 4));

            var contours = BinaryPrimitives.ReadInt16BigEndian(glyf.AsSpan(start));

            return contours > 0
                ? BinaryPrimitives.ReadUInt16BigEndian(glyf.AsSpan(start + 10 + (contours - 1) * 2)) + 1
                : throw new ArgumentException($"Glyph {glyph} is not a simple glyph with contours.", nameof(glyph));
        }

        private static void WriteU16(List<byte> data, ushort value)
        {
            data.Add((byte)(value >> 8));
            data.Add((byte)(value & 0xFF));
        }

        private static void WriteU32(List<byte> data, uint value)
        {
            WriteU16(data, (ushort)(value >> 16));
            WriteU16(data, (ushort)(value & 0xFFFF));
        }
    }
}
