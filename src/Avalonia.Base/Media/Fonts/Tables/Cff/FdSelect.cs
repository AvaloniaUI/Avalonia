using System;
using System.Buffers.Binary;

namespace Avalonia.Media.Fonts.Tables.Cff
{
    /// <summary>
    /// CFF FDSelect — maps a glyph index to a Font DICT index in a CID-keyed font, so the interpreter
    /// can pick the right per-FD Local Subr INDEX and private state for that glyph. Formats 0 (a flat
    /// per-glyph array) and 3 (sorted ranges) are the only ones the spec defines.
    /// </summary>
    internal sealed class FdSelect
    {
        // Format 0: one fd byte per glyph.
        private readonly byte[]? _perGlyph;

        // Format 3: parallel arrays — _rangeFirst has nRanges+1 entries (the last is the sentinel
        // first-glyph just past the end); _rangeFd[i] is the fd for glyphs [_rangeFirst[i], _rangeFirst[i+1]).
        private readonly int[]? _rangeFirst;
        private readonly byte[]? _rangeFd;

        private FdSelect(byte[] perGlyph) => _perGlyph = perGlyph;

        private FdSelect(int[] rangeFirst, byte[] rangeFd)
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
                int p = offset + 1;

                // format(1) + nRanges(2) + nRanges*(first uint16 + fd byte) + sentinel(2).
                if (p + 2 > table.Length)
                {
                    return null;
                }

                int nRanges = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(p));
                p += 2;

                if (p + nRanges * 3 + 2 > table.Length)
                {
                    return null;
                }

                var first = new int[nRanges + 1];
                var fd = new byte[nRanges];

                for (int i = 0; i < nRanges; i++)
                {
                    first[i] = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(p));
                    p += 2;
                    fd[i] = span[p];
                    p += 1;

                    // GetFd binary-searches assuming ascending range starts; a non-monotonic
                    // array would return a wrong (but in-bounds) FD → wrong Local Subrs →
                    // silent CID misrendering. Reject the malformed table instead.
                    if (i > 0 && first[i] < first[i - 1])
                    {
                        return null;
                    }
                }

                first[nRanges] = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(p)); // sentinel

                // The sentinel is the glyph count; it must not precede the last range start, or
                // the final range would be inverted.
                if (nRanges > 0 && first[nRanges] < first[nRanges - 1])
                {
                    return null;
                }

                return new FdSelect(first, fd);
            }

            return null;
        }

        /// <summary>Gets the Font DICT index for <paramref name="glyphIndex"/> (0 if out of range).</summary>
        public int GetFd(int glyphIndex)
        {
            if (_perGlyph is not null)
            {
                return (uint)glyphIndex < (uint)_perGlyph.Length ? _perGlyph[glyphIndex] : 0;
            }

            // Format 3: binary search for the range whose [first, next) contains the glyph.
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
