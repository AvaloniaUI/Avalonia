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

        public CmapFormat12Table(ReadOnlyMemory<byte> table)
        {
            var reader = new BigEndianBinaryReader(table.Span);

            ushort format = reader.ReadUInt16();
            Debug.Assert(format == 12, "Format must be 12.");

            ushort reserved = reader.ReadUInt16();
            Debug.Assert(reserved == 0, "Reserved field must be 0.");

            uint length = reader.ReadUInt32();

            _table = table.Slice(0, (int)length);

            uint language = reader.ReadUInt32();

            _groupCount = (int)reader.ReadUInt32();

            int groupsOffset = reader.Position;
            int groupsLength = _groupCount * 12;

            Debug.Assert(length >= groupsOffset + groupsLength, "Length must cover all groups.");

            _groups = _table.Slice(groupsOffset, groupsLength);
        }

        private static uint ReadUInt32BE(ReadOnlyMemory<byte> mem, int groupIndex, int fieldOffset)
        {
            var span = mem.Span;
            int byteIndex = groupIndex * 12 + fieldOffset;
            return BinaryPrimitives.ReadUInt32BigEndian(span.Slice(byteIndex, 4));
        }

        // Binary search to find the group containing the code point
        private int FindGroupIndex(int codePoint)
        {
            int lo = 0;
            int hi = _groupCount - 1;

            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                uint start = ReadUInt32BE(_groups, mid, 0);
                uint end = ReadUInt32BE(_groups, mid, 4);

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

                uint start = ReadUInt32BE(_groups, groupIndex, 0);
                uint startGlyph = ReadUInt32BE(_groups, groupIndex, 8);

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
                    uint start = ReadUInt32BE(_groups, g, 0);
                    uint end = ReadUInt32BE(_groups, g, 4);
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
                    uint start = ReadUInt32BE(_groups, g, 0);
                    uint end = ReadUInt32BE(_groups, g, 4);

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
                    uint start = ReadUInt32BE(_groups, g, 0);
                    uint end = ReadUInt32BE(_groups, g, 4);
                    uint startGlyph = ReadUInt32BE(_groups, g, 8);

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
                uint start = ReadUInt32BE(_groups, g, 0);
                uint end = ReadUInt32BE(_groups, g, 4);
                uint startGlyph = ReadUInt32BE(_groups, g, 8);

                for (uint cp = start; cp <= end; cp++)
                {
                    yield return new KeyValuePair<int, ushort>((int)cp, (ushort)(startGlyph + (cp - start)));
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
