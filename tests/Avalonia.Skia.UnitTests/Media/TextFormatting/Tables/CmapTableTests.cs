using System;
using System.Buffers.Binary;
using Avalonia.Media.Fonts.Tables.Cmap;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting.Tables
{
    public class CmapTableTests
    {
        [Fact]
        public void BuildFormat4Subtable_Should_Map_Range()
        {
            // Build a subtable mapping U+0030–U+0039 (digits 0–9) to glyphs 1–10
            byte[] subtable = CmapTestHelper.BuildFormat4Subtable(0x0030, 0x0039, 1);

            var cmap = new CmapFormat4Table(subtable);

            for (int i = 0; i < 10; i++)
            {
                int cp = 0x30 + i;
                ushort glyph = cmap[cp];
                var expectedGlyph = (ushort)(i + 1);
                Assert.Equal(expectedGlyph, glyph);
            }

            // Outside range should map to 0
            Assert.Equal((ushort)0, cmap[0x0041]); // 'A'
        }
    }

    public static class CmapTestHelper
    {
        /// <summary>
        /// Builds a Format 4 subtable for a TrueType font's 'cmap' table, which maps a range of character codes to
        /// glyph indices.
        /// </summary>
        /// <remarks>The Format 4 subtable is used in TrueType fonts to define mappings from character
        /// codes to glyph indices for a contiguous range of character codes. This method generates a minimal Format 4
        /// subtable with one segment for the specified range and a sentinel segment, as required by the TrueType
        /// specification. <para> The generated subtable includes the necessary header fields, segment arrays, and delta
        /// values to ensure that the specified range of character codes maps correctly to the corresponding glyph
        /// indices. </para> <exception cref="ArgumentException"> Thrown if <paramref name="endCode"/> is less than
        /// <paramref name="startCode"/>. </exception></remarks>
        /// <param name="startCode">The starting character code of the range to map.</param>
        /// <param name="endCode">The ending character code of the range to map.</param>
        /// <param name="firstGlyphId">The glyph index corresponding to the <paramref name="startCode"/>. Subsequent character codes in the range
        /// will map to consecutive glyph indices.</param>
        /// <returns>A byte array representing the Format 4 subtable, which can be embedded in a TrueType font's 'cmap' table.</returns>
        public static byte[] BuildFormat4Subtable(ushort startCode, ushort endCode, ushort firstGlyphId = 1)
        {
            if (endCode < startCode)
                throw new ArgumentException("endCode must be >= startCode");

            // We will build exactly one real segment + sentinel
            ushort segCount = 2; // one real + one sentinel
            ushort segCountX2 = (ushort)(segCount * 2);

            // Correct search parameters (searchRange = 2 * (2^floor(log2(segCount))))
            int highestPowerOfTwo = 1;
            while (highestPowerOfTwo * 2 <= segCount)
                highestPowerOfTwo *= 2;
            ushort searchRange = (ushort)(2 * highestPowerOfTwo);
            ushort entrySelector = (ushort)(Math.Log(highestPowerOfTwo, 2));
            ushort rangeShift = (ushort)(segCountX2 - searchRange);

            // idDelta so that startCode maps to firstGlyphId
            short idDelta = (short)(firstGlyphId - startCode);

            // Calculate length: header (14) + endCode(segCount*2) + reservedPad(2) + startCode(segCount*2)
            // + idDelta(segCount*2) + idRangeOffset(segCount*2) + (no glyphIdArray)
            int headerSize = 14;
            int segArraysSize = segCount * 2 /*endCode*/ + 2 /*reservedPad*/ + segCount * 2 /*startCode*/ + segCount * 2 /*idDelta*/ + segCount * 2 /*idRangeOffset*/;
            int length = headerSize + segArraysSize;

            var buffer = new byte[length];
            int pos = 0;

            void WriteUInt16(ushort v)
            { BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(pos, 2), v); pos += 2; }
            void WriteInt16(short v)
            { BinaryPrimitives.WriteInt16BigEndian(buffer.AsSpan(pos, 2), v); pos += 2; }

            // Header
            WriteUInt16(4);              // format
            WriteUInt16((ushort)length); // length
            WriteUInt16(0);              // language
            WriteUInt16(segCountX2);
            WriteUInt16(searchRange);
            WriteUInt16(entrySelector);
            WriteUInt16(rangeShift);

            // endCode[] (one real segment then sentinel)
            WriteUInt16(endCode);
            WriteUInt16(0xFFFF);

            WriteUInt16(0); // reservedPad

            // startCode[]
            WriteUInt16(startCode);
            WriteUInt16(0xFFFF);

            // idDelta[]
            WriteInt16(idDelta);
            WriteInt16(1); // sentinel delta (commonly 1)

            // idRangeOffset[]
            WriteUInt16(0);
            WriteUInt16(0);

            return buffer;
        }
    }
}
