using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Avalonia.Media.Fonts.Tables.Cmap
{
    internal sealed class CmapFormat12Table : IReadOnlyDictionary<int, ushort>
    {
        private readonly ReadOnlyMemory<byte> _table;
        private readonly int _groupCount;
        private readonly ReadOnlyMemory<byte> _groups;

        private int? _count;

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

        public int Count
        {
            get
            {
                if (_count.HasValue)
                {
                    return _count.Value;
                }

                long total = 0;

                for (int g = 0; g < _groupCount; g++)
                {
                    var groups = _groups.Span;

                    uint start = ReadUInt32BE(groups, g, 0);
                    uint end = ReadUInt32BE(groups, g, 4);
                    total += (end - start + 1);
                }

                _count = (int)total;

                return _count.Value;
            }
        }

        public IEnumerable<int> Keys
        {
            get
            {
                for (int g = 0; g < _groupCount; g++)
                {
                    var groups = _groups.Span;

                    uint start = ReadUInt32BE(groups, g, 0);
                    uint end = ReadUInt32BE(groups, g, 4);

                    for (uint cp = start; cp <= end; cp++)
                    {
                        yield return (int)cp;
                    }
                }
            }
        }

        public IEnumerable<ushort> Values
        {
            get
            {
                for (int g = 0; g < _groupCount; g++)
                {
                    var groups = _groups.Span;

                    uint start = ReadUInt32BE(groups, g, 0);
                    uint end = ReadUInt32BE(groups, g, 4);
                    uint startGlyph = ReadUInt32BE(groups, g, 8);

                    for (uint cp = start; cp <= end; cp++)
                    {
                        yield return (ushort)(startGlyph + (cp - start));
                    }
                }
            }
        }

        public bool ContainsKey(int key) => this[key] != 0;

        public bool TryGetValue(int key, out ushort value)
        {
            value = this[key];
            return value != 0;
        }

        public IEnumerator<KeyValuePair<int, ushort>> GetEnumerator()
        {
            for (int g = 0; g < _groupCount; g++)
            {
                var groups = _groups.Span;

                uint start = ReadUInt32BE(groups, g, 0);
                uint end = ReadUInt32BE(groups, g, 4);
                uint startGlyph = ReadUInt32BE(groups, g, 8);

                for (uint cp = start; cp <= end; cp++)
                {
                    yield return new KeyValuePair<int, ushort>((int)cp, (ushort)(startGlyph + (cp - start)));
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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
        public void MapCodePointsToGlyphs(ReadOnlySpan<int> codePoints, Span<ushort> glyphIds)
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
    }
}
