using System;
using System.Buffers.Binary;

namespace Avalonia.Media.Fonts.Tables.Cff
{
    /// <summary>
    /// CFF / CFF2 FDSelect: maps a glyph index to a Font DICT index in a CID-keyed CFF or a CFF2 font,
    /// so the interpreter can pick the right per-FD Local Subr INDEX and private state for that glyph.
    /// Format 0 is a flat per-glyph array and format 3 a sorted run of ranges; CFF2 adds format 4,
    /// the same ranges with 32-bit glyph indices and 16-bit Font DICT indices. Those are the only
    /// formats the specs define.
    /// </summary>
    internal sealed class FdSelect
    {
        // Format 0: one fd byte per glyph.
        private readonly byte[]? _perGlyph;

        // Formats 3 and 4: parallel arrays. _rangeFirst has nRanges+1 entries (the last is the sentinel
        // first-glyph just past the end); _rangeFd[i] is the fd for glyphs [_rangeFirst[i], _rangeFirst[i+1]).
        private readonly int[]? _rangeFirst;
        private readonly ushort[]? _rangeFd;

        private FdSelect(byte[] perGlyph) => _perGlyph = perGlyph;

        private FdSelect(int[] rangeFirst, ushort[] rangeFd)
        {
            _rangeFirst = rangeFirst;
            _rangeFd = rangeFd;
        }

        public static FdSelect? Parse(ReadOnlyMemory<byte> table, int offset, int glyphCount)
        {
            var span = table.Span;
            byte format = span[offset];

            if (format == 0)
            {
                if (offset + 1 + glyphCount > table.Length)
                {
                    return null;
                }

                return new FdSelect(table.Slice(offset + 1, glyphCount).ToArray());
            }

            if (format == 3)
            {
                // format(1) + nRanges(2) + nRanges*(first uint16 + fd uint8) + sentinel(2).
                return ParseRanges(span, offset + 1, wide: false);
            }

            if (format == 4)
            {
                // format(1) + nRanges(4) + nRanges*(first uint32 + fd uint16) + sentinel(4).
                return ParseRanges(span, offset + 1, wide: true);
            }

            return null;
        }

        private static FdSelect? ParseRanges(ReadOnlySpan<byte> span, int p, bool wide)
        {
            int countSize = wide ? 4 : 2;
            int rangeSize = wide ? 6 : 3;

            if ((long)p + countSize > span.Length)
            {
                return null;
            }

            long nRanges = wide
                ? BinaryPrimitives.ReadUInt32BigEndian(span.Slice(p))
                : BinaryPrimitives.ReadUInt16BigEndian(span.Slice(p));
            p += countSize;

            // Bound the declared range count by the bytes actually present before allocating.
            if (p + (nRanges * rangeSize) + countSize > span.Length)
            {
                return null;
            }

            var first = new int[nRanges + 1];
            var fd = new ushort[nRanges];

            for (int i = 0; i < nRanges; i++)
            {
                if (!TryReadGlyph(span, ref p, wide, out first[i]))
                {
                    return null;
                }

                fd[i] = wide ? BinaryPrimitives.ReadUInt16BigEndian(span.Slice(p)) : span[p];
                p += wide ? 2 : 1;

                // GetFd binary-searches assuming ascending range starts; a non-monotonic array
                // would return a wrong (but in-bounds) FD, so the glyph would be drawn with another
                // Font DICT's Local Subrs. Reject the malformed table instead.
                if (i > 0 && first[i] < first[i - 1])
                {
                    return null;
                }
            }

            // The sentinel is the glyph count; it must not precede the last range start, or the
            // final range would be inverted.
            if (!TryReadGlyph(span, ref p, wide, out first[nRanges]) ||
                (nRanges > 0 && first[nRanges] < first[nRanges - 1]))
            {
                return null;
            }

            return new FdSelect(first, fd);
        }

        private static bool TryReadGlyph(ReadOnlySpan<byte> span, ref int p, bool wide, out int glyph)
        {
            if (wide)
            {
                uint value = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(p));
                p += 4;
                glyph = (int)value;
                return value <= int.MaxValue;
            }

            glyph = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(p));
            p += 2;
            return true;
        }

        /// <summary>Gets the Font DICT index for <paramref name="glyphIndex"/> (0 if out of range).</summary>
        public int GetFd(int glyphIndex)
        {
            if (_perGlyph is not null)
            {
                return (uint)glyphIndex < (uint)_perGlyph.Length ? _perGlyph[glyphIndex] : 0;
            }

            // Formats 3 and 4: binary search for the range whose [first, next) contains the glyph.
            var first = _rangeFirst!;
            int lo = 0;
            int hi = _rangeFd!.Length - 1;

            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                if (glyphIndex < first[mid])
                {
                    hi = mid - 1;
                }
                else if (glyphIndex >= first[mid + 1])
                {
                    lo = mid + 1;
                }
                else
                {
                    return _rangeFd[mid];
                }
            }

            return 0;
        }
    }
}
