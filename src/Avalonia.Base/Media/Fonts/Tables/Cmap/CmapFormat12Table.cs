using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Avalonia.Media.Fonts.Tables.Cmap
{
    internal sealed class CmapFormat12Table
    {
        private readonly ReadOnlyMemory<byte> _table;
        private readonly int _groupCount;
        private readonly ReadOnlyMemory<byte> _groups;

        /// <summary>
        /// Gets the language code for the cmap subtable.
        /// For non-language-specific tables, this value is 0.
        /// </summary>
        public uint Language { get; }

        public CmapFormat12Table(ReadOnlyMemory<byte> table)
        {
            var reader = new BigEndianBinaryReader(table.Span);

            ushort format = reader.ReadUInt16();
            Debug.Assert(format == 12, "Format must be 12.");

            ushort reserved = reader.ReadUInt16();
            Debug.Assert(reserved == 0, "Reserved field must be 0.");

            uint length = reader.ReadUInt32();

            _table = table.Slice(0, (int)length);

            Language = reader.ReadUInt32();

            _groupCount = (int)reader.ReadUInt32();

            int groupsOffset = reader.Position;
            int groupsLength = _groupCount * 12;

            Debug.Assert(length >= groupsOffset + groupsLength, "Length must cover all groups.");

            _groups = _table.Slice(groupsOffset, groupsLength);
        }

        /// <summary>
        /// Retrieves the glyph index corresponding to the specified Unicode code point.
        /// </summary>
        /// <param name="codePoint">The Unicode code point for which to obtain the glyph index. Must be a valid code point supported by the
        /// font.</param>
        /// <returns>The glyph index as an unsigned 16-bit integer for the specified code point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetGlyph(int codePoint) => this[codePoint];

        /// <summary>
        /// Determines whether the specified Unicode code point is present in the glyph set.
        /// </summary>
        /// <param name="codePoint">The Unicode code point to check for presence in the glyph set. Must be a valid integer representing a
        /// Unicode character.</param>
        /// <returns>true if the glyph set contains the specified code point; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsGlyph(int codePoint)
        {
            return FindGroupIndex(codePoint) >= 0;
        }

        /// <summary>
        /// Maps multiple Unicode code points to glyph indices in a single operation.
        /// </summary>
        /// <param name="codePoints">Read-only span of code points to map.</param>
        /// <param name="glyphIds">Output span to write glyph IDs. Must be at least as long as <paramref name="codePoints"/>.</param>
        /// <remarks>
        /// This method is significantly more efficient than calling the indexer multiple times as it:
        /// - Reuses span references (no repeated memory access)
        /// - Caches group data for sequential lookups
        /// - Optimizes for locality of code points (common in text runs)
        /// Format 12 is commonly used for fonts with large character sets (CJK, emoji, etc.)
        /// This is the preferred method for batch character-to-glyph mapping in text shaping.
        /// </remarks>
        public void GetGlyphs(ReadOnlySpan<int> codePoints, Span<ushort> glyphIds)
        {
            if (glyphIds.Length < codePoints.Length)
            {
                throw new ArgumentException("Output span must be at least as long as input span", nameof(glyphIds));
            }

            var groups = _groups.Span;

            // Track last group for locality optimization
            int lastGroup = -1;
            uint lastStart = 0;
            uint lastEnd = 0;
            uint lastStartGlyph = 0;

            for (int i = 0; i < codePoints.Length; i++)
            {
                int codePoint = codePoints[i];

                // Optimization: check if codepoint is in the same group as previous
                if (lastGroup >= 0 && codePoint >= lastStart && codePoint <= lastEnd)
                {
                    glyphIds[i] = (ushort)(lastStartGlyph + (codePoint - lastStart));
                    continue;
                }

                // Binary search for group
                int groupIndex = FindGroupIndexOptimized(codePoint, groups);

                if (groupIndex < 0)
                {
                    glyphIds[i] = 0;
                    lastGroup = -1;

                    continue;
                }

                // Cache group data
                lastGroup = groupIndex;
                lastStart = ReadUInt32BE(groups, groupIndex, 0);
                lastEnd = ReadUInt32BE(groups, groupIndex, 4);
                lastStartGlyph = ReadUInt32BE(groups, groupIndex, 8);

                glyphIds[i] = (ushort)(lastStartGlyph + (codePoint - lastStart));
            }
        }

        public bool TryGetGlyph(int codePoint, out ushort glyphId)
        {
            int groupIndex = FindGroupIndex(codePoint);

            if (groupIndex < 0)
            {
                glyphId = 0;
                return false;
            }

            var groups = _groups.Span;

            uint start = ReadUInt32BE(groups, groupIndex, 0);
            uint startGlyph = ReadUInt32BE(groups, groupIndex, 8);

            glyphId = (ushort)(startGlyph + (codePoint - start));

            return glyphId != 0;
        }

        internal bool TryGetRange(int index, out CodepointRange range)
        {
            if ((uint)index >= (uint)_groupCount)
            {
                range = default;

                return false;
            }

            var groups = _groups.Span;

            int start = (int)ReadUInt32BE(groups, index, 0);
            int end = (int)ReadUInt32BE(groups, index, 4);

            range = new CodepointRange(start, end);

            return true;
        }

        public ushort this[int codePoint]
        {
            get
            {
                int groupIndex = FindGroupIndex(codePoint);

                if (groupIndex < 0)
                {
                    return 0;
                }

                var groups = _groups.Span;

                uint start = ReadUInt32BE(groups, groupIndex, 0);
                uint startGlyph = ReadUInt32BE(groups, groupIndex, 8);

                // Calculate glyph index
                return (ushort)(startGlyph + (codePoint - start));
            }
        }

        // Optimized binary search that works directly with cached span
        private int FindGroupIndexOptimized(int codePoint, ReadOnlySpan<byte> groups)
        {
            int lo = 0;
            int hi = _groupCount - 1;

            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                uint start = ReadUInt32BE(groups, mid, 0);
                uint end = ReadUInt32BE(groups, mid, 4);

                if (codePoint < start)
                {
                    hi = mid - 1;
                }
                else if (codePoint > end)
                {
                    lo = mid + 1;
                }
                else
                {
                    return mid;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ReadUInt32BE(ReadOnlySpan<byte> span, int groupIndex, int fieldOffset)
        {
            int byteIndex = groupIndex * 12 + fieldOffset;

            return BinaryPrimitives.ReadUInt32BigEndian(span.Slice(byteIndex, 4));
        }

        // Binary search to find the group containing the code point
        private int FindGroupIndex(int codePoint)
        {
            int lo = 0;
            int hi = _groupCount - 1;

            var groups = _groups.Span;

            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                uint start = ReadUInt32BE(groups, mid, 0);
                uint end = ReadUInt32BE(groups, mid, 4);

                if (codePoint < start)
                {
                    hi = mid - 1;
                }
                else if (codePoint > end)
                {
                    lo = mid + 1;
                }
                else
                {
                    return mid;
                }
            }

            // Not found
            return -1;
        }
    }
}
