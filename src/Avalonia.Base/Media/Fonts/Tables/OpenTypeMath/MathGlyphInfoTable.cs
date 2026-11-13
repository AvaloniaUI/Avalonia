using System;
using System.Buffers.Binary;
using Avalonia.Media.Fonts.Tables.Variation;

namespace Avalonia.Media.Fonts.Tables.OpenTypeMath
{
    /// <summary>
    /// Reads the MathGlyphInfo sub-table: italics corrections, top accent attachment points, the
    /// extended shape coverage and the math kerning of the font's glyphs.
    /// </summary>
    /// <remarks>
    /// Each part is loaded independently; a part whose offset, coverage or record array is out of
    /// range is treated as absent and does not affect the others.
    /// </remarks>
    internal sealed class MathGlyphInfoTable
    {
        private const int HeaderSize = 8;
        private const int KernInfoRecordSize = 8;

        private readonly ValueTable _italicsCorrections;
        private readonly ValueTable _topAccentAttachments;
        private readonly CoverageTable? _extendedShapes;
        private readonly ReadOnlyMemory<byte> _kernInfo;
        private readonly CoverageTable? _kernCoverage;
        private readonly int _kernCount;

        private MathGlyphInfoTable(ReadOnlyMemory<byte> table)
        {
            var span = table.Span;

            _italicsCorrections = ValueTable.Load(table, BinaryPrimitives.ReadUInt16BigEndian(span));
            _topAccentAttachments = ValueTable.Load(table, BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2)));

            int extendedShapeOffset = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(4));

            if (extendedShapeOffset != 0)
            {
                CoverageTable.TryLoad(table, extendedShapeOffset, out _extendedShapes);
            }

            int kernInfoOffset = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(6));

            if (kernInfoOffset != 0 && kernInfoOffset <= table.Length - 4)
            {
                var kernInfo = table.Slice(kernInfoOffset);
                var kernSpan = kernInfo.Span;
                int count = BinaryPrimitives.ReadUInt16BigEndian(kernSpan.Slice(2));

                if (4 + count * KernInfoRecordSize <= kernInfo.Length &&
                    CoverageTable.TryLoad(kernInfo, BinaryPrimitives.ReadUInt16BigEndian(kernSpan), out var coverage))
                {
                    _kernInfo = kernInfo;
                    _kernCoverage = coverage;
                    _kernCount = count;
                }
            }
        }

        /// <summary>
        /// Loads the table from <paramref name="table"/>, the bytes from the start of MathGlyphInfo to
        /// the end of the MATH table.
        /// </summary>
        public static MathGlyphInfoTable? Load(ReadOnlyMemory<byte> table)
            => table.Length < HeaderSize ? null : new MathGlyphInfoTable(table);

        public bool TryGetItalicsCorrection(ushort glyph, ItemVariationStore? store, ReadOnlySpan<float> regionScalers, out int value)
            => _italicsCorrections.TryGetValue(glyph, store, regionScalers, out value);

        public bool TryGetTopAccentAttachment(ushort glyph, ItemVariationStore? store, ReadOnlySpan<float> regionScalers, out int value)
            => _topAccentAttachments.TryGetValue(glyph, store, regionScalers, out value);

        public bool IsExtendedShape(ushort glyph) => _extendedShapes is not null && _extendedShapes.GetCoverageIndex(glyph) >= 0;

        /// <summary>
        /// Gets the kern of <paramref name="glyph"/> at <paramref name="corner"/> for a script whose
        /// attachment lies at <paramref name="correctionHeight"/>, or 0 when the glyph has no kern there.
        /// </summary>
        /// <remarks>
        /// A MathKern lists increasing correction heights and one kern value more than heights. The
        /// value used is the one at the number of heights strictly below
        /// <paramref name="correctionHeight"/>, so a height equal to a correction height takes the
        /// value below it. HarfBuzz's <c>hb_ot_math_get_glyph_kerning</c> draws the same boundary.
        /// </remarks>
        public int GetKerning(
            ushort glyph,
            int corner,
            int correctionHeight,
            ItemVariationStore? store,
            ReadOnlySpan<float> regionScalers)
        {
            if (_kernCoverage is null)
            {
                return 0;
            }

            var index = _kernCoverage.GetCoverageIndex(glyph);

            if ((uint)index >= (uint)_kernCount)
            {
                return 0;
            }

            var kernInfo = _kernInfo.Span;
            int kernOffset = BinaryPrimitives.ReadUInt16BigEndian(kernInfo.Slice(4 + index * KernInfoRecordSize + corner * 2));

            if (kernOffset == 0 || kernOffset > kernInfo.Length - 2)
            {
                return 0;
            }

            var kern = kernInfo.Slice(kernOffset);
            int heightCount = BinaryPrimitives.ReadUInt16BigEndian(kern);

            if (2 + (2 * heightCount + 1) * MathValueRecord.Size > kern.Length)
            {
                return 0;
            }

            var low = 0;
            var remaining = heightCount;

            while (remaining > 0)
            {
                var half = remaining / 2;
                var height = MathValueRecord.Read(kern, 2 + (low + half) * MathValueRecord.Size, store, regionScalers);

                if (height < correctionHeight)
                {
                    low += half + 1;
                    remaining -= half + 1;
                }
                else
                {
                    remaining = half;
                }
            }

            return MathValueRecord.Read(kern, 2 + (heightCount + low) * MathValueRecord.Size, store, regionScalers);
        }

        /// <summary>
        /// A coverage table followed by one MathValueRecord per covered glyph, the layout of
        /// MathItalicsCorrectionInfo and MathTopAccentAttachment.
        /// </summary>
        private readonly struct ValueTable
        {
            private readonly ReadOnlyMemory<byte> _table;
            private readonly CoverageTable? _coverage;
            private readonly int _count;

            private ValueTable(ReadOnlyMemory<byte> table, CoverageTable coverage, int count)
            {
                _table = table;
                _coverage = coverage;
                _count = count;
            }

            public static ValueTable Load(ReadOnlyMemory<byte> parent, int offset)
            {
                if (offset == 0 || offset > parent.Length - 4)
                {
                    return default;
                }

                var table = parent.Slice(offset);
                var span = table.Span;
                int count = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2));

                if (4 + count * MathValueRecord.Size > table.Length ||
                    !CoverageTable.TryLoad(table, BinaryPrimitives.ReadUInt16BigEndian(span), out var coverage))
                {
                    return default;
                }

                return new ValueTable(table, coverage, count);
            }

            public bool TryGetValue(ushort glyph, ItemVariationStore? store, ReadOnlySpan<float> regionScalers, out int value)
            {
                value = 0;

                if (_coverage is null)
                {
                    return false;
                }

                var index = _coverage.GetCoverageIndex(glyph);

                if ((uint)index >= (uint)_count)
                {
                    return false;
                }

                value = MathValueRecord.Read(_table.Span, 4 + index * MathValueRecord.Size, store, regionScalers);

                return true;
            }
        }
    }
}
