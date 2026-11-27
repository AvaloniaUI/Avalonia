using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Media.Fonts.Tables.Variation
{
    /// <summary>
    /// Parses the OpenType 'gvar' (Glyph Variations) table. Provides on-demand access to
    /// per-glyph TupleVariationStore data and the shared tuple records.
    /// </summary>
    /// <remarks>
    /// <para>
    /// gvar carries the per-glyph point deltas that deform a TrueType outline as the
    /// active variation coordinates move through axis space. Each glyph entry is a
    /// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#tuple-variation-store-header">
    /// TupleVariationStore</see> with one or more tuple variations, each carrying a peak
    /// tuple (axis coordinate vector that fully activates this tuple), an optional
    /// intermediate-region pair (start / end tuples that bound a linear ramp), an optional
    /// private point-number list (which points the deltas apply to), and packed x/y
    /// delta arrays.
    /// </para>
    /// <para>
    /// The actual delta-decoding and per-tuple scaler computation lives in
    /// <see cref="GlyphVariationReader"/>; this class only owns the table-wide entry-point
    /// lookup (header parsing + per-glyph offsets + shared tuple array).
    /// </para>
    /// </remarks>
    internal sealed class GvarTable
    {
        internal const string TableName = "gvar";
        internal static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TableName);

        // Header is fixed at 20 bytes:
        //   uint16 majorVersion           (1)
        //   uint16 minorVersion           (0)
        //   uint16 axisCount
        //   uint16 sharedTupleCount
        //   Offset32 sharedTuplesOffset   (from table start)
        //   uint16 glyphCount
        //   uint16 flags                  (bit 0 = long offset format)
        //   Offset32 glyphVariationDataArrayOffset  (from table start)
        // ... followed by uint16/uint32 offsets[glyphCount + 1] (sentinel end offset).
        private const int HeaderSize = 20;
        private const ushort SupportedMajorVersion = 1;
        private const ushort LongOffsetsFlag = 0x0001;

        private readonly ReadOnlyMemory<byte> _data;
        private readonly int _axisCount;
        private readonly int _glyphCount;
        private readonly bool _longOffsets;

        // Byte offsets relative to the start of _data.
        private readonly int _offsetArrayStart;
        private readonly int _dataArrayStart;
        private readonly int _sharedTuplesStart;
        private readonly int _sharedTupleCount;

        private GvarTable(
            ReadOnlyMemory<byte> data,
            int axisCount,
            int glyphCount,
            bool longOffsets,
            int offsetArrayStart,
            int dataArrayStart,
            int sharedTuplesStart,
            int sharedTupleCount)
        {
            _data = data;
            _axisCount = axisCount;
            _glyphCount = glyphCount;
            _longOffsets = longOffsets;
            _offsetArrayStart = offsetArrayStart;
            _dataArrayStart = dataArrayStart;
            _sharedTuplesStart = sharedTuplesStart;
            _sharedTupleCount = sharedTupleCount;
        }

        /// <summary>
        /// Gets the number of variation axes the gvar table is encoded for. Must match
        /// the font's <c>fvar</c> axis count.
        /// </summary>
        public int AxisCount => _axisCount;

        /// <summary>
        /// Gets the total number of glyphs the table provides variation data for.
        /// Equals <c>maxp.numGlyphs</c>.
        /// </summary>
        public int GlyphCount => _glyphCount;

        /// <summary>
        /// Gets the number of shared peak-tuple records the table declares. Tuples
        /// reference shared records by index (saving a per-tuple peak-tuple payload).
        /// </summary>
        public int SharedTupleCount => _sharedTupleCount;

        public static bool TryLoad(
            GlyphTypeface glyphTypeface,
            int expectedAxisCount,
            int expectedGlyphCount,
            [NotNullWhen(true)] out GvarTable? gvarTable)
        {
            gvarTable = null;

            if (!glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var data))
            {
                return false;
            }

            var span = data.Span;
            if (span.Length < HeaderSize)
            {
                return false;
            }

            var majorVersion = BinaryPrimitives.ReadUInt16BigEndian(span);
            if (majorVersion != SupportedMajorVersion)
            {
                return false;
            }

            var axisCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(4));
            var sharedTupleCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(6));
            var sharedTuplesOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(span.Slice(8));
            var glyphCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(12));
            var flags = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(14));
            var dataArrayOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(span.Slice(16));

            // Spec-imposed cross-table invariants. If the font claims a different axis
            // count than fvar declares, or a different glyph count than maxp says, treat
            // the table as malformed and bail rather than risk reading garbage on a
            // mismatched layout.
            if (axisCount != expectedAxisCount || glyphCount != expectedGlyphCount)
            {
                return false;
            }

            var longOffsets = (flags & LongOffsetsFlag) != 0;

            // Offset table sits right after the fixed header. Each entry is uint16 (short)
            // or uint32 (long), and there are glyphCount + 1 entries (last is sentinel).
            var offsetArrayStart = HeaderSize;
            var offsetEntrySize = longOffsets ? 4 : 2;
            var offsetArrayEnd = offsetArrayStart + (glyphCount + 1) * offsetEntrySize;

            if (offsetArrayEnd > span.Length ||
                sharedTuplesOffset + sharedTupleCount * axisCount * 2 > span.Length ||
                dataArrayOffset > span.Length)
            {
                return false;
            }

            gvarTable = new GvarTable(
                data,
                axisCount,
                glyphCount,
                longOffsets,
                offsetArrayStart,
                dataArrayStart: dataArrayOffset,
                sharedTuplesStart: sharedTuplesOffset,
                sharedTupleCount: sharedTupleCount);
            return true;
        }

        /// <summary>
        /// Retrieves the raw bytes of the TupleVariationStore for the specified glyph.
        /// Returns <c>false</c> when the glyph index is out of range or when the glyph has
        /// no variation data (a glyph whose shape doesn't vary across axis space, encoded
        /// by an empty entry).
        /// </summary>
        public bool TryGetGlyphVariationData(int glyphIndex, out ReadOnlyMemory<byte> data)
        {
            data = default;

            if ((uint)glyphIndex >= (uint)_glyphCount)
            {
                return false;
            }

            var span = _data.Span;
            int start, end;

            if (_longOffsets)
            {
                var basePos = _offsetArrayStart + glyphIndex * 4;
                start = (int)BinaryPrimitives.ReadUInt32BigEndian(span.Slice(basePos, 4));
                end = (int)BinaryPrimitives.ReadUInt32BigEndian(span.Slice(basePos + 4, 4));
            }
            else
            {
                // Short format: stored value is the offset divided by 2.
                var basePos = _offsetArrayStart + glyphIndex * 2;
                start = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(basePos, 2)) * 2;
                end = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(basePos + 2, 2)) * 2;
            }

            if (start == end)
            {
                return false; // Glyph has no variation data.
            }

            var absoluteStart = _dataArrayStart + start;
            var absoluteEnd = _dataArrayStart + end;

            if (absoluteStart < 0 || absoluteEnd > _data.Length || absoluteStart > absoluteEnd)
            {
                return false;
            }

            data = _data.Slice(absoluteStart, absoluteEnd - absoluteStart);
            return true;
        }

        /// <summary>
        /// Reads the shared peak tuple at the specified index into the provided coordinate
        /// span. Returns <c>false</c> when the index is out of range or when the output
        /// span is shorter than <see cref="AxisCount"/>.
        /// </summary>
        /// <param name="index">Index into the shared tuple array, in <c>[0, SharedTupleCount)</c>.</param>
        /// <param name="coordinates">
        /// Destination for the tuple's per-axis F2DOT14 coordinates, decoded to
        /// <see cref="float"/>. Length must be at least <see cref="AxisCount"/>.
        /// </param>
        public bool TryGetSharedTuple(int index, Span<float> coordinates)
        {
            if ((uint)index >= (uint)_sharedTupleCount || coordinates.Length < _axisCount)
            {
                return false;
            }

            var span = _data.Span;
            var tupleStart = _sharedTuplesStart + index * _axisCount * 2;

            for (var i = 0; i < _axisCount; i++)
            {
                var raw = BinaryPrimitives.ReadInt16BigEndian(span.Slice(tupleStart + i * 2, 2));
                coordinates[i] = raw / 16384f;
            }

            return true;
        }
    }
}
