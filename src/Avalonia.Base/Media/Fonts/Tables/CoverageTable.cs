using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Media.Fonts.Tables
{
    /// <summary>
    /// Reads an OpenType Coverage table: the set of glyphs a lookup or sub-table applies to, each
    /// mapped to its coverage index into the arrays that follow the coverage in its parent table.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Format 1 lists the covered glyph ids in increasing order; the coverage index is the position
    /// in that list. Format 2 lists ranges of consecutive glyph ids in increasing order, each with
    /// the coverage index of its first glyph.
    /// </para>
    /// <para>
    /// Both formats are looked up by binary search, so <see cref="TryLoad"/> rejects a table whose
    /// glyphs or ranges are not in increasing order or whose ranges overlap: a search over such a
    /// table would miss covered glyphs depending on where they sit.
    /// </para>
    /// <para>
    /// Reference: <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-table"/>.
    /// </para>
    /// </remarks>
    internal sealed class CoverageTable
    {
        private const int HeaderSize = 4;
        private const int RangeRecordSize = 6;

        private readonly ReadOnlyMemory<byte> _records;
        private readonly int _format;
        private readonly int _recordCount;

        private CoverageTable(ReadOnlyMemory<byte> records, int format, int recordCount, int glyphCount)
        {
            _records = records;
            _format = format;
            _recordCount = recordCount;
            GlyphCount = glyphCount;
        }

        /// <summary>
        /// Gets the number of glyphs the table covers.
        /// </summary>
        public int GlyphCount { get; }

        /// <summary>
        /// Loads the coverage table at <paramref name="offset"/> in <paramref name="parent"/>.
        /// </summary>
        /// <returns>
        /// <c>false</c> when the offset or the records fall outside <paramref name="parent"/>, the
        /// format is unknown, or the glyphs or ranges are not in increasing order.
        /// </returns>
        public static bool TryLoad(ReadOnlyMemory<byte> parent, int offset, [NotNullWhen(true)] out CoverageTable? coverage)
        {
            coverage = null;

            if (offset < 0 || offset > parent.Length - HeaderSize)
            {
                return false;
            }

            var span = parent.Span.Slice(offset);
            var format = BinaryPrimitives.ReadUInt16BigEndian(span);
            var count = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2));

            switch (format)
            {
                case 1:
                {
                    var length = count * 2;

                    if (length > span.Length - HeaderSize)
                    {
                        return false;
                    }

                    var glyphs = span.Slice(HeaderSize, length);
                    var previous = -1;

                    for (var i = 0; i < count; i++)
                    {
                        int glyph = BinaryPrimitives.ReadUInt16BigEndian(glyphs.Slice(i * 2));

                        if (glyph <= previous)
                        {
                            return false;
                        }

                        previous = glyph;
                    }

                    coverage = new CoverageTable(parent.Slice(offset + HeaderSize, length), format, count, count);

                    return true;
                }
                case 2:
                {
                    var length = count * RangeRecordSize;

                    if (length > span.Length - HeaderSize)
                    {
                        return false;
                    }

                    var ranges = span.Slice(HeaderSize, length);
                    var previousEnd = -1;
                    var glyphCount = 0;

                    for (var i = 0; i < count; i++)
                    {
                        var record = ranges.Slice(i * RangeRecordSize);
                        int start = BinaryPrimitives.ReadUInt16BigEndian(record);
                        int end = BinaryPrimitives.ReadUInt16BigEndian(record.Slice(2));

                        if (start <= previousEnd || end < start)
                        {
                            return false;
                        }

                        previousEnd = end;
                        glyphCount += end - start + 1;
                    }

                    coverage = new CoverageTable(parent.Slice(offset + HeaderSize, length), format, count, glyphCount);

                    return true;
                }
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets the coverage index of <paramref name="glyph"/>, or -1 when the table does not cover it.
        /// </summary>
        /// <remarks>
        /// A format 2 index is the range's start coverage index plus the glyph's distance from the
        /// range start, as the font declares it. Callers range-check the index against the array it
        /// selects from.
        /// </remarks>
        public int GetCoverageIndex(ushort glyph)
        {
            var span = _records.Span;
            var low = 0;
            var high = _recordCount - 1;

            if (_format == 1)
            {
                while (low <= high)
                {
                    var mid = (low + high) >> 1;
                    var value = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(mid * 2));

                    if (value < glyph)
                    {
                        low = mid + 1;
                    }
                    else if (value > glyph)
                    {
                        high = mid - 1;
                    }
                    else
                    {
                        return mid;
                    }
                }

                return -1;
            }

            while (low <= high)
            {
                var mid = (low + high) >> 1;
                var record = span.Slice(mid * RangeRecordSize);
                var start = BinaryPrimitives.ReadUInt16BigEndian(record);

                if (glyph < start)
                {
                    high = mid - 1;
                    continue;
                }

                var end = BinaryPrimitives.ReadUInt16BigEndian(record.Slice(2));

                if (glyph > end)
                {
                    low = mid + 1;
                    continue;
                }

                return BinaryPrimitives.ReadUInt16BigEndian(record.Slice(4)) + (glyph - start);
            }

            return -1;
        }
    }
}
