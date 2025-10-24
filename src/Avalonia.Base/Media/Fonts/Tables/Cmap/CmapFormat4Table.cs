using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Avalonia.Media.Fonts.Tables.Cmap
{
    internal sealed class CmapFormat4Table : IReadOnlyDictionary<int, ushort>
    {
        private readonly ReadOnlyMemory<byte> _table;

        private readonly int _segCount;
        private readonly ReadOnlyMemory<byte> _endCodes;
        private readonly ReadOnlyMemory<byte> _startCodes;
        private readonly ReadOnlyMemory<byte> _idDeltas;
        private readonly ReadOnlyMemory<byte> _idRangeOffsets;
        private readonly ReadOnlyMemory<byte> _glyphIdArray;

        private int? _count;

        public CmapFormat4Table(ReadOnlyMemory<byte> table)
        {
            var reader = new BigEndianBinaryReader(table.Span);

            ushort format = reader.ReadUInt16(); // must be 4

            Debug.Assert(format == 4, "Format must be 4.");

            ushort length = reader.ReadUInt16(); // length in bytes of this subtable

            _table = table.Slice(0, length);

            ushort language = reader.ReadUInt16(); // language code, 0 for non-language-specific

            ushort segCountX2 = reader.ReadUInt16(); // 2 * segCount
            _segCount = segCountX2 / 2;

            ushort searchRange = reader.ReadUInt16(); // searchRange = 2 * (2^floor(log2(segCount)))
            ushort entrySelector = reader.ReadUInt16(); // entrySelector = log2(searchRange/2)
            ushort rangeShift = reader.ReadUInt16(); // rangeShift = segCountX2 - searchRange

            // Spec sanity checks
            Debug.Assert(searchRange == (ushort)(2 * (1 << (int)Math.Floor(Math.Log(_segCount, 2)))),
                "searchRange must equal 2 * (2^floor(log2(segCount))).");
            Debug.Assert(entrySelector == (ushort)Math.Floor(Math.Log(_segCount, 2)),
                "entrySelector must equal log2(searchRange/2).");
            Debug.Assert(rangeShift == (ushort)(segCountX2 - searchRange),
                "rangeShift must equal segCountX2 - searchRange.");

            // Compute offsets
            int endCodeOffset = reader.Position;
            int startCodeOffset = endCodeOffset + _segCount * 2 + 2; // + reservedPad
            int idDeltaOffset = startCodeOffset + _segCount * 2; // after startCodes
            int idRangeOffsetOffset = idDeltaOffset + _segCount * 2; // after idDeltas
            int glyphIdArrayOffset = idRangeOffsetOffset + _segCount * 2; // after idRangeOffsets

            // Ensure declared length is consistent
            Debug.Assert(length >= glyphIdArrayOffset,
                "Subtable length must be at least large enough to contain glyphIdArray.");

            // Slice directly
            _endCodes = _table.Slice(endCodeOffset, _segCount * 2);

            _startCodes = _table.Slice(startCodeOffset, _segCount * 2);

            _idDeltas = _table.Slice(idDeltaOffset, _segCount * 2);

            _idRangeOffsets = _table.Slice(idRangeOffsetOffset, _segCount * 2);

            int glyphCount = (length - glyphIdArrayOffset) / 2;

            Debug.Assert(glyphCount >= 0, "GlyphIdArray length must not be negative.");

            _glyphIdArray = _table.Slice(glyphIdArrayOffset, glyphCount * 2);
        }

        // Reads a big-endian UInt16 from the specified word index in the given memory
        private static ushort ReadUInt16BE(ReadOnlyMemory<byte> mem, int wordIndex)
        {
            var span = mem.Span;
            int byteIndex = wordIndex * 2;

            // Ensure we don't go out of bounds
            return BinaryPrimitives.ReadUInt16BigEndian(span.Slice(byteIndex, 2));
        }

        public int Count
        {
            get
            {
                if (_count.HasValue)
                {
                    return _count.Value;
                }

                int count = 0;

                for (int seg = 0; seg < _segCount; seg++)
                {
                    // Get start and end of segment
                    int start = ReadUInt16BE(_startCodes, seg);
                    int end = ReadUInt16BE(_endCodes, seg);

                    for (int cp = start; cp <= end; cp++)
                    {
                        // Only count if maps to non-zero glyph
                        if (this[cp] != 0)
                        {
                            count++;
                        }
                    }
                }

                _count = count;

                return count;
            }
        }

        public ushort this[int codePoint]
        {
            get
            {
                // Find the segment containing the codePoint
                int segmentIndex = FindSegmentIndex(codePoint);

                if (segmentIndex < 0)
                {
                    return 0;
                }

                ushort idRangeOffset = ReadUInt16BE(_idRangeOffsets, segmentIndex);
                ushort idDelta = ReadUInt16BE(_idDeltas, segmentIndex);

                // If idRangeOffset is 0, glyphId = (codePoint + idDelta) % 65536
                if (idRangeOffset == 0)
                {
                    return (ushort)((codePoint + idDelta) & 0xFFFF);
                }
                else
                {
                    int start = ReadUInt16BE(_startCodes, segmentIndex);
                    int ro = idRangeOffset / 2; // words
                    // The index into the glyphIdArray
                    int idx = (codePoint - start) + ro - (_segCount - segmentIndex);

                    // Ensure index is within bounds of glyphIdArray
                    int glyphArrayWords = _glyphIdArray.Length / 2;

                    if ((uint)idx < (uint)glyphArrayWords)
                    {
                        ushort glyphId = ReadUInt16BE(_glyphIdArray, idx);

                        // If glyphId is not 0, apply idDelta
                        if (glyphId != 0)
                        {
                            glyphId = (ushort)((glyphId + idDelta) & 0xFFFF);
                        }

                        return glyphId;
                    }
                }

                // Not found or maps to missing glyph
                return 0;
            }
        }

        public bool ContainsKey(int key) => this[key] != 0;

        public bool TryGetValue(int key, out ushort value)
        {
            value = this[key];

            return value != 0;
        }

        public IEnumerable<int> Keys
        {
            get
            {
                for (int seg = 0; seg < _segCount; seg++)
                {
                    int start = ReadUInt16BE(_startCodes, seg);
                    int end = ReadUInt16BE(_endCodes, seg);

                    for (int cp = start; cp <= end; cp++)
                    {
                        ushort gid = ResolveGlyph(seg, cp);

                        // Only yield code points that map to non-zero glyphs
                        if (gid != 0)
                        {
                            yield return cp;
                        }
                    }
                }
            }
        }

        public IEnumerable<ushort> Values
        {
            get
            {
                for (int seg = 0; seg < _segCount; seg++)
                {
                    int start = ReadUInt16BE(_startCodes, seg);
                    int end = ReadUInt16BE(_endCodes, seg);

                    for (int cp = start; cp <= end; cp++)
                    {
                        ushort gid = ResolveGlyph(seg, cp);

                        // Only yield non-zero glyphs
                        if (gid != 0)
                        {
                            yield return gid;
                        }
                    }
                }
            }
        }

        public IEnumerator<KeyValuePair<int, ushort>> GetEnumerator()
        {
            for (int seg = 0; seg < _segCount; seg++)
            {
                int start = ReadUInt16BE(_startCodes, seg);
                int end = ReadUInt16BE(_endCodes, seg);

                for (int cp = start; cp <= end; cp++)
                {
                    ushort gid = ResolveGlyph(seg, cp);

                    // Only yield mappings to non-zero glyphs
                    if (gid != 0)
                    {
                        yield return new KeyValuePair<int, ushort>(cp, gid);
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // Resolves the glyph ID for a given code point within a specific segment
        private ushort ResolveGlyph(int segmentIndex, int codePoint)
        {
            ushort idRangeOffset = ReadUInt16BE(_idRangeOffsets, segmentIndex);
            ushort idDelta = ReadUInt16BE(_idDeltas, segmentIndex);

            if (idRangeOffset == 0)
            {
                return (ushort)((codePoint + idDelta) & 0xFFFF);
            }
            else
            {
                int start = ReadUInt16BE(_startCodes, segmentIndex);
                int ro = idRangeOffset / 2; // words
                int idx = (codePoint - start) + ro - (_segCount - segmentIndex);
                int glyphArrayWords = _glyphIdArray.Length / 2;

                if ((uint)idx < (uint)glyphArrayWords)
                {
                    ushort glyphId = ReadUInt16BE(_glyphIdArray, idx);

                    if (glyphId != 0)
                    {
                        glyphId = (ushort)((glyphId + idDelta) & 0xFFFF);
                    }

                    return glyphId;
                }
            }

            // Not found or maps to missing glyph
            return 0;
        }

        private int FindSegmentIndex(int codePoint)
        {
            int lo = 0;
            int hi = _segCount - 1;

            // Binary search over endCodes (sorted ascending)
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int end = ReadUInt16BE(_endCodes, mid);

                if (codePoint > end)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            // lo is now the first segment whose endCode >= codePoint
            if (lo < _segCount)
            {
                int start = ReadUInt16BE(_startCodes, lo);

                if (codePoint >= start)
                {
                    return lo;
                }
            }

            return -1; // not found
        }
    }
}
