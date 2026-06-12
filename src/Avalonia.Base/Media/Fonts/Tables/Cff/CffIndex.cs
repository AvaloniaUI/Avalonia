using System;
using System.Buffers.Binary;

namespace Avalonia.Media.Fonts.Tables.Cff
{
    /// <summary>
    /// A CFF / CFF2 INDEX: a count-prefixed array of variable-length byte entries addressed by a
    /// trailing offset array. Used for the Name, Top DICT, String, Global/Local Subr and CharStrings
    /// INDEXes. This is a lightweight view over the table memory — offsets are read on demand, so no
    /// per-entry allocation occurs (mirrors the <c>GlyfTable</c> / <c>LocaTable</c> style).
    /// </summary>
    internal readonly struct CffIndex
    {
        private readonly ReadOnlyMemory<byte> _table;
        private readonly int _offsetsStart;
        private readonly int _dataBase;
        private readonly byte _offSize;

        /// <summary>Number of entries in the INDEX.</summary>
        public int Count { get; }

        /// <summary>Absolute offset within the table just past the end of this INDEX, for chained parsing.</summary>
        public int EndOffset { get; }

        private CffIndex(ReadOnlyMemory<byte> table, int count, byte offSize, int offsetsStart, int dataBase, int endOffset)
        {
            _table = table;
            Count = count;
            _offSize = offSize;
            _offsetsStart = offsetsStart;
            _dataBase = dataBase;
            EndOffset = endOffset;
        }

        /// <summary>
        /// Reads the INDEX header at <paramref name="position"/>. The CFF and CFF2 INDEX layouts share
        /// the same body; CFF2 uses a 32-bit count, so the caller selects the header width.
        /// </summary>
        /// <param name="table">The whole CFF / CFF2 table memory.</param>
        /// <param name="position">Offset of the INDEX within the table.</param>
        /// <param name="wideCount"><c>true</c> for a CFF2 32-bit count; <c>false</c> for a CFF 16-bit count.</param>
        public static CffIndex Read(ReadOnlyMemory<byte> table, int position, bool wideCount = false)
        {
            var span = table.Span;

            long count;
            int headerEnd;
            if (wideCount)
            {
                count = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(position));
                headerEnd = position + 4;
            }
            else
            {
                count = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(position));
                headerEnd = position + 2;
            }

            if (count == 0)
            {
                // An empty INDEX is just the count field.
                return new CffIndex(table, 0, 0, headerEnd, headerEnd, headerEnd);
            }

            byte offSize = span[headerEnd];
            if (offSize is < 1 or > 4)
            {
                throw new ArgumentException($"Invalid CFF INDEX offSize {offSize}.");
            }

            int offsetsStart = headerEnd + 1;

            // Compute the offset-array bounds in long and reject an INDEX whose offset array
            // alone can't fit in the table: a CFF2 32-bit count (e.g. 0x3FFFFFFF) otherwise
            // wraps `(count + 1) * offSize` in int, yields a near-1e9 Count, and drives a
            // `new CffIndex[Count]` OOM from a few-hundred-byte font. Past this check Count
            // is bounded by the table length, so the int cast is safe.
            long offsetArrayEndLong = (long)offsetsStart + (count + 1) * offSize;
            if (offsetArrayEndLong > span.Length)
            {
                throw new ArgumentException("Corrupt CFF INDEX: offset array exceeds table.");
            }

            int offsetArrayEnd = (int)offsetArrayEndLong;
            // Offsets are 1-based, relative to the byte preceding the object data.
            int dataBase = offsetArrayEnd - 1;
            int lastOffset = ReadOffset(span, offsetsStart + ((int)count * offSize), offSize);
            int endOffset = dataBase + lastOffset;

            if ((uint)endOffset > (uint)table.Length)
            {
                throw new ArgumentException("Corrupt CFF INDEX: data extends past table.");
            }

            return new CffIndex(table, (int)count, offSize, offsetsStart, dataBase, endOffset);
        }

        /// <summary>Gets the raw bytes of entry <paramref name="i"/>.</summary>
        public ReadOnlyMemory<byte> this[int i]
        {
            get
            {
                if ((uint)i >= (uint)Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(i));
                }

                var span = _table.Span;
                int start = _dataBase + ReadOffset(span, _offsetsStart + (i * _offSize), _offSize);
                int end = _dataBase + ReadOffset(span, _offsetsStart + ((i + 1) * _offSize), _offSize);

                if (end < start || end > _table.Length)
                {
                    throw new ArgumentException("Corrupt CFF INDEX offsets.");
                }

                return _table.Slice(start, end - start);
            }
        }

        private static int ReadOffset(ReadOnlySpan<byte> span, int at, byte offSize)
        {
            int value = 0;
            for (int i = 0; i < offSize; i++)
            {
                value = (value << 8) | span[at + i];
            }

            return value;
        }
    }
}
