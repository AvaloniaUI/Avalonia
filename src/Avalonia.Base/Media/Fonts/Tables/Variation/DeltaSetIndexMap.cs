using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Media.Fonts.Tables.Variation
{
    /// <summary>
    /// Parses an OpenType DeltaSetIndexMap. Maps glyph IDs (or other indexable
    /// items) to <c>(outerIndex, innerIndex)</c> coordinates in an
    /// <see cref="ItemVariationStore"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// HVAR / VVAR / MVAR each use one of these to map their per-glyph (or per-metric)
    /// queries onto the shared store's delta sets. The format packs both indices into
    /// a single fixed-width entry (1-4 bytes), with a configurable number of bits for
    /// the inner index so a font with thousands of distinct delta sets can be
    /// indexed compactly.
    /// </para>
    /// <para>
    /// Two formats: format 0 (16-bit mapCount) for smaller fonts and format 1 (32-bit
    /// mapCount) for fonts with more than 65535 entries. The entry-format byte is
    /// identical between the two; only the count's width differs.
    /// </para>
    /// <para>
    /// Reference: <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#associating-target-items-to-variation-data"/>.
    /// </para>
    /// </remarks>
    internal sealed class DeltaSetIndexMap
    {
        // entryFormat byte layout:
        //   bits 0-3: innerIndexBitCount - 1   (1..16 bits for the inner index)
        //   bits 4-5: mapEntrySize - 1         (1..4 bytes per entry)
        //   bits 6-7: reserved (0)
        private const byte InnerBitCountMask = 0x0F;
        private const byte EntrySizeMask = 0x30;
        private const int EntrySizeShift = 4;

        private readonly ReadOnlyMemory<byte> _data;
        private readonly int _mapCount;
        private readonly int _entrySize;
        private readonly int _innerBitCount;
        private readonly int _innerMask;
        private readonly int _entriesStart;

        private DeltaSetIndexMap(
            ReadOnlyMemory<byte> data,
            int mapCount,
            int entrySize,
            int innerBitCount,
            int entriesStart)
        {
            _data = data;
            _mapCount = mapCount;
            _entrySize = entrySize;
            _innerBitCount = innerBitCount;
            _innerMask = (1 << innerBitCount) - 1;
            _entriesStart = entriesStart;
        }

        public int MapCount => _mapCount;

        public static bool TryLoad(ReadOnlyMemory<byte> data, [NotNullWhen(true)] out DeltaSetIndexMap? map)
        {
            map = null;

            var span = data.Span;
            if (span.Length < 4)
            {
                return false;
            }

            var format = span[0];
            var entryFormat = span[1];

            int mapCount;
            int entriesStart;
            if (format == 0)
            {
                mapCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2));
                entriesStart = 4;
            }
            else if (format == 1)
            {
                if (span.Length < 6)
                {
                    return false;
                }
                mapCount = (int)BinaryPrimitives.ReadUInt32BigEndian(span.Slice(2));
                entriesStart = 6;
            }
            else
            {
                return false;
            }

            var innerBitCount = (entryFormat & InnerBitCountMask) + 1;
            var entrySize = ((entryFormat & EntrySizeMask) >> EntrySizeShift) + 1;

            if (entriesStart + (long)mapCount * entrySize > span.Length)
            {
                return false;
            }

            map = new DeltaSetIndexMap(data, mapCount, entrySize, innerBitCount, entriesStart);
            return true;
        }

        /// <summary>
        /// Reads the entry for the specified glyph index. Glyph indices beyond the
        /// map's <see cref="MapCount"/> are clamped to the last entry, per spec —
        /// fonts can use this to provide indices only for the first N glyphs and
        /// have the remainder default to a shared delta set.
        /// </summary>
        public bool TryGetIndices(int glyphIndex, out int outerIndex, out int innerIndex)
        {
            outerIndex = 0;
            innerIndex = 0;

            if (glyphIndex < 0 || _mapCount == 0)
            {
                return false;
            }

            // Clamp beyond mapCount to the last available entry, matching the spec.
            var effectiveIndex = glyphIndex >= _mapCount ? _mapCount - 1 : glyphIndex;
            var pos = _entriesStart + effectiveIndex * _entrySize;
            var span = _data.Span;

            // Read the variable-width entry as a big-endian unsigned integer.
            int rawEntry = 0;
            for (var b = 0; b < _entrySize; b++)
            {
                rawEntry = (rawEntry << 8) | span[pos + b];
            }

            innerIndex = rawEntry & _innerMask;
            outerIndex = rawEntry >> _innerBitCount;
            return true;
        }
    }
}
