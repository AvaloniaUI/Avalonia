using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Avalonia.Media.Fonts.Tables.Cmap
{
    internal sealed class CmapFormat4Table
    {
        private readonly ReadOnlyMemory<byte> _table;

        private readonly int _segCount;
        private readonly ReadOnlyMemory<byte> _endCodes;
        private readonly ReadOnlyMemory<byte> _startCodes;
        private readonly ReadOnlyMemory<byte> _idDeltas;
        private readonly ReadOnlyMemory<byte> _idRangeOffsets;
        private readonly ReadOnlyMemory<byte> _glyphIdArray;

        /// <summary>
        /// Gets the language code for the cmap subtable.
        /// For non-language-specific tables, this value is 0.
        /// </summary>
        public ushort Language { get; }

        public CmapFormat4Table(ReadOnlyMemory<byte> table)
        {
            var reader = new BigEndianBinaryReader(table.Span);

            ushort format = reader.ReadUInt16(); // must be 4

            Debug.Assert(format == 4, "Format must be 4.");

            ushort length = reader.ReadUInt16(); // length in bytes of this subtable

            _table = table.Slice(0, length);

            Language = reader.ReadUInt16(); // language code, 0 for non-language-specific

            ushort segCountX2 = reader.ReadUInt16(); // 2 * segCount
            _segCount = segCountX2 / 2;

            ushort searchRange = reader.ReadUInt16(); // searchRange = 2 * (2^floor(log2(segCount)))
            ushort entrySelector = reader.ReadUInt16(); // entrySelector = log2(searchRange/2)
            ushort rangeShift = reader.ReadUInt16(); // rangeShift = segCountX2 - searchRange

            // Spec sanity checks (warn in debug builds instead of asserting)
#if DEBUG
            var expectedSearchRange = (ushort)(2 * (1 << (int)Math.Floor(Math.Log(_segCount, 2))));
            if (searchRange != expectedSearchRange)
            {
                Debug.WriteLine($"CMAP format 4: unexpected searchRange {searchRange}, expected {expectedSearchRange} for segCount {_segCount}.");
            }

            var expectedEntrySelector = (ushort)Math.Floor(Math.Log(_segCount, 2));
            if (entrySelector != expectedEntrySelector)
            {
                Debug.WriteLine($"CMAP format 4: unexpected entrySelector {entrySelector}, expected {expectedEntrySelector} for segCount {_segCount}.");
            }

            var expectedRangeShift = (ushort)(segCountX2 - searchRange);
            if (rangeShift != expectedRangeShift)
            {
                Debug.WriteLine($"CMAP format 4: unexpected rangeShift {rangeShift}, expected {expectedRangeShift} for segCountX2 {segCountX2} and searchRange {searchRange}.");
            }
#endif

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetGlyph(int codePoint) => this[codePoint];

        public bool ContainsGlyph(int codePoint)
        {
            int seg = FindSegmentIndex(codePoint);

            if ((uint)seg >= (uint)_segCount)
            {
                return false;
            }

            ushort idRangeOffset = ReadUInt16BE(_idRangeOffsets.Span, seg);
            ushort idDelta = ReadUInt16BE(_idDeltas.Span, seg);

            if (idRangeOffset == 0)
            {
                // Always maps to something (possibly .notdef via delta)
                return ((codePoint + idDelta) & 0xFFFF) != 0;
            }

            int start = ReadUInt16BE(_startCodes.Span, seg);
            int ro = idRangeOffset >> 1;
            int idx = (codePoint - start) + ro - (_segCount - seg);

            if ((uint)idx >= (uint)(_glyphIdArray.Length >> 1))
            {
                return false;
            }

            ushort glyphId = ReadUInt16BE(_glyphIdArray.Span, idx);

            return glyphId != 0;
        }

        /// <summary>
        /// Maps multiple Unicode code points to glyph indices in a single operation.
        /// </summary>
        /// <param name="codePoints">Read-only span of code points to map.</param>
        /// <param name="glyphIds">Output span to write glyph IDs. Must be at least as long as <paramref name="codePoints"/>.</param>
        /// <remarks>
        /// This method is significantly more efficient than calling the indexer multiple times as it:
        /// - Reuses span references (no repeated .Span property access)
        /// - Caches segment data for sequential lookups
        /// - Optimizes for locality of code points (common in text runs)
        /// This is the preferred method for batch character-to-glyph mapping in text shaping.
        /// </remarks>
        public void GetGlyphs(ReadOnlySpan<int> codePoints, Span<ushort> glyphIds)
        {
            if (glyphIds.Length < codePoints.Length)
            {
                throw new ArgumentException("Output span must be at least as long as input span", nameof(glyphIds));
            }

            // Cache all spans once
            var startCodes = _startCodes.Span;
            var endCodes = _endCodes.Span;
            var idDeltas = _idDeltas.Span;
            var idRangeOffsets = _idRangeOffsets.Span;
            var glyphIdArray = _glyphIdArray.Span;
            int glyphArrayWords = glyphIdArray.Length / 2;

            // Track last segment for locality optimization
            int lastSegment = -1;

            for (int i = 0; i < codePoints.Length; i++)
            {
                int codePoint = codePoints[i];
                int segmentIndex;

                // Optimization: check if codepoint is in the same segment as previous
                if (lastSegment >= 0 && lastSegment < _segCount)
                {
                    int lastStart = ReadUInt16BE(startCodes, lastSegment);
                    int lastEnd = ReadUInt16BE(endCodes, lastSegment);

                    if (codePoint >= lastStart && codePoint <= lastEnd)
                    {
                        segmentIndex = lastSegment;
                        goto MapGlyph;
                    }
                }

                // Binary search for segment
                segmentIndex = FindSegmentIndexOptimized(codePoint, startCodes, endCodes);

                if (segmentIndex < 0)
                {
                    glyphIds[i] = 0;
                    continue;
                }

                lastSegment = segmentIndex;

            MapGlyph:
                ushort idRangeOffset = ReadUInt16BE(idRangeOffsets, segmentIndex);
                ushort idDelta = ReadUInt16BE(idDeltas, segmentIndex);

                if (idRangeOffset == 0)
                {
                    glyphIds[i] = (ushort)((codePoint + idDelta) & 0xFFFF);
                }
                else
                {
                    int start = ReadUInt16BE(startCodes, segmentIndex);
                    int ro = idRangeOffset / 2;
                    int idx = (codePoint - start) + ro - (_segCount - segmentIndex);

                    if ((uint)idx < (uint)glyphArrayWords)
                    {
                        ushort glyphId = ReadUInt16BE(glyphIdArray, idx);

                        if (glyphId != 0)
                        {
                            glyphId = (ushort)((glyphId + idDelta) & 0xFFFF);
                        }

                        glyphIds[i] = glyphId;
                    }
                    else
                    {
                        glyphIds[i] = 0;
                    }
                }
            }
        }

        public bool TryGetGlyph(int codePoint, out ushort glyphId)
        {
            int seg = FindSegmentIndex(codePoint);

            if ((uint)seg >= (uint)_segCount)
            {
                glyphId = 0;

                return false;
            }

            ushort idRangeOffset = ReadUInt16BE(_idRangeOffsets.Span, seg);
            ushort idDelta = ReadUInt16BE(_idDeltas.Span, seg);

            if (idRangeOffset == 0)
            {
                glyphId = (ushort)((codePoint + idDelta) & 0xFFFF);

                return glyphId != 0;
            }

            int start = ReadUInt16BE(_startCodes.Span, seg);
            int ro = idRangeOffset >> 1;
            int idx = (codePoint - start) + ro - (_segCount - seg);

            if ((uint)idx >= (uint)(_glyphIdArray.Length >> 1))
            {
                glyphId = 0;

                return false;
            }

            glyphId = ReadUInt16BE(_glyphIdArray.Span, idx);

            if (glyphId != 0)
            {
                glyphId = (ushort)((glyphId + idDelta) & 0xFFFF);
            }

            return glyphId != 0;
        }

        internal bool TryGetRange(int index, out CodepointRange range)
        {
            if ((uint)index >= (uint)_segCount)
            {
                range = default;
                return false;
            }

            int start = ReadUInt16BE(_startCodes.Span, index);
            int end = ReadUInt16BE(_endCodes.Span, index);

            // Skip sentinel segment (0xFFFF)
            if (start == 0xFFFF && end == 0xFFFF)
            {
                range = default;

                return false;
            }

            range = new CodepointRange(start, end);

            return true;
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

                ushort idRangeOffset = ReadUInt16BE(_idRangeOffsets.Span, segmentIndex);
                ushort idDelta = ReadUInt16BE(_idDeltas.Span, segmentIndex);

                // If idRangeOffset is 0, glyphId = (codePoint + idDelta) % 65536
                if (idRangeOffset == 0)
                {
                    return (ushort)((codePoint + idDelta) & 0xFFFF);
                }
                else
                {
                    int start = ReadUInt16BE(_startCodes.Span, segmentIndex);
                    int ro = idRangeOffset / 2; // words
                    // The index into the glyphIdArray
                    int idx = (codePoint - start) + ro - (_segCount - segmentIndex);

                    // Ensure index is within bounds of glyphIdArray
                    int glyphArrayWords = _glyphIdArray.Length / 2;

                    if ((uint)idx < (uint)glyphArrayWords)
                    {
                        ushort glyphId = ReadUInt16BE(_glyphIdArray.Span, idx);

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

        // Resolves the glyph ID for a given code point within a specific segment
        private ushort ResolveGlyph(int segmentIndex, int codePoint)
        {
            ushort idRangeOffset = ReadUInt16BE(_idRangeOffsets.Span, segmentIndex);
            ushort idDelta = ReadUInt16BE(_idDeltas.Span, segmentIndex);

            if (idRangeOffset == 0)
            {
                return (ushort)((codePoint + idDelta) & 0xFFFF);
            }
            else
            {
                int start = ReadUInt16BE(_startCodes.Span, segmentIndex);
                int ro = idRangeOffset / 2; // words
                int idx = (codePoint - start) + ro - (_segCount - segmentIndex);
                int glyphArrayWords = _glyphIdArray.Length / 2;

                if ((uint)idx < (uint)glyphArrayWords)
                {
                    ushort glyphId = ReadUInt16BE(_glyphIdArray.Span, idx);

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

            var startCodes = _startCodes.Span;
            var endCodes = _endCodes.Span;

            // Binary search over endCodes (sorted ascending)
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int end = ReadUInt16BE(endCodes, mid);

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
                int start = ReadUInt16BE(startCodes, lo);

                if (codePoint >= start)
                {
                    return lo;
                }
            }

            return -1; // not found
        }

        // Optimized binary search that works directly with cached spans
        private int FindSegmentIndexOptimized(int codePoint, ReadOnlySpan<byte> startCodes, ReadOnlySpan<byte> endCodes)
        {
            int lo = 0;
            int hi = _segCount - 1;

            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int end = ReadUInt16BE(endCodes, mid);

                if (codePoint > end)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            if (lo < _segCount)
            {
                int start = ReadUInt16BE(startCodes, lo);

                if (codePoint >= start)
                {
                    return lo;
                }
            }

            return -1;
        }

        // Reads a big-endian UInt16 from the specified word index in the given memory
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort ReadUInt16BE(ReadOnlySpan<byte> span, int wordIndex)
        {
            int byteIndex = wordIndex * 2;

            // Ensure we don't go out of bounds
            return BinaryPrimitives.ReadUInt16BigEndian(span.Slice(byteIndex, 2));
        }
    }
}
