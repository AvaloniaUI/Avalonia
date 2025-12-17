using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Avalonia.Media.Fonts.Tables
{
    /// <summary>
    /// Provides efficient access to the 'loca' (Index to Location) table without pre-allocating an array.
    /// The loca table stores offsets into the glyf table for each glyph.
    /// </summary>
    internal sealed class LocaTable
    {
        internal const string TableName = "loca";
        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        private readonly ReadOnlyMemory<byte> _data;
        private readonly int _glyphCount;
        private readonly bool _isShortFormat;

        private LocaTable(ReadOnlyMemory<byte> data, int glyphCount, bool isShortFormat)
        {
            _data = data;
            _glyphCount = glyphCount;
            _isShortFormat = isShortFormat;
        }

        /// <summary>
        /// Gets the number of glyphs in the font.
        /// </summary>
        public int GlyphCount => _glyphCount;

        /// <summary>
        /// Loads the loca table from the specified typeface.
        /// </summary>
        /// <param name="glyphTypeface">The glyph typeface to load from.</param>
        /// <param name="head">The head table containing the index format.</param>
        /// <param name="maxp">The maxp table containing the glyph count.</param>
        /// <returns>A LocaTable instance, or null if the table cannot be loaded.</returns>
        public static LocaTable? Load(IGlyphTypeface glyphTypeface, HeadTable head, MaxpTable maxp)
        {
            if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var locaData))
            {
                return null;
            }

            var isShortFormat = head.IndexToLocFormat == 0;
            var glyphCount = maxp.NumGlyphs;

            // Validate table size
            var expectedSize = isShortFormat ? (glyphCount + 1) * 2 : (glyphCount + 1) * 4;
            
            if (locaData.Length < expectedSize)
            {
                // Table is shorter than expected, but we can still work with what we have
                // The GetOffset method will handle out-of-bounds access
            }

            return new LocaTable(locaData, glyphCount, isShortFormat);
        }

        /// <summary>
        /// Gets the start and end offsets for the specified glyph index.
        /// </summary>
        /// <param name="glyphIndex">The glyph index (0-based).</param>
        /// <param name="start">The start offset into the glyf table.</param>
        /// <param name="end">The end offset into the glyf table.</param>
        /// <returns>True if the offsets were retrieved successfully; otherwise, false.</returns>
        public bool TryGetOffsets(int glyphIndex, out int start, out int end)
        {
            if ((uint)glyphIndex >= (uint)_glyphCount)
            {
                start = 0;
                end = 0;
                return false;
            }

            start = GetOffset(glyphIndex);
            end = GetOffset(glyphIndex + 1);

            return true;
        }

        /// <summary>
        /// Gets the offset for the specified glyph index into the glyf table.
        /// </summary>
        /// <param name="glyphIndex">The glyph index (0-based).</param>
        /// <returns>The offset into the glyf table, or 0 if the index is out of range.</returns>
        private int GetOffset(int glyphIndex)
        {
            if ((uint)glyphIndex > (uint)_glyphCount) // Note: allows glyphCount for the end offset
            {
                return 0;
            }

            var span = _data.Span;

            if (_isShortFormat)
            {
                var byteOffset = glyphIndex * 2;

                if (byteOffset + 2 > span.Length)
                {
                    return 0;
                }

                // Short format: uint16 values stored divided by 2
                var value = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(byteOffset, 2));
                return value * 2;
            }
            else
            {
                var byteOffset = glyphIndex * 4;

                if (byteOffset + 4 > span.Length)
                {
                    return 0;
                }

                // Long format: uint32 values
                var value = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(byteOffset, 4));

                // Clamp to int.MaxValue to avoid overflow
                return value > int.MaxValue ? int.MaxValue : (int)value;
            }
        }
    }
}
