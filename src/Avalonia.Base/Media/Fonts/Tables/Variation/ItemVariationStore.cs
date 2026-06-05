using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Media.Fonts.Tables.Variation
{
    /// <summary>
    /// Parses an OpenType ItemVariationStore. Provides delta lookups by
    /// (outerIndex, innerIndex) at a given active variation point.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The ItemVariationStore is shared infrastructure: HVAR, VVAR, and MVAR each
    /// embed one to carry per-glyph or font-wide deltas; COLR v1 also uses one for
    /// variable paint records. The store consists of a list of <i>variation regions</i>
    /// (each specifying a per-axis ramp through axis space) and one or more
    /// <i>ItemVariationData</i> subtables grouping deltas by associated regions.
    /// </para>
    /// <para>
    /// To resolve a delta the caller supplies an <c>outerIndex</c> selecting which
    /// subtable to use and an <c>innerIndex</c> selecting the item within that
    /// subtable. The store walks the subtable's region indexes, computes each
    /// region's scaler against the active variation, multiplies by the corresponding
    /// delta, and sums.
    /// </para>
    /// <para>
    /// Reference: <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#item-variation-store-header-and-item-variation-subtables"/>.
    /// </para>
    /// </remarks>
    internal sealed class ItemVariationStore
    {
        private const ushort SupportedFormat = 1;
        private const ushort LongWordsFlag = 0x8000;
        private const ushort WordDeltaCountMask = 0x7FFF;

        // Raw bytes of the ItemVariationStore, sliced so offsets are relative to 0.
        private readonly ReadOnlyMemory<byte> _data;

        private readonly int _axisCount;
        private readonly int _regionCount;
        private readonly int _regionListStart;    // offset of the region records (after axisCount/regionCount header)
        private readonly int _ivdCount;
        // Per-subtable absolute byte offsets from _data start. Indexed by outerIndex.
        private readonly int[] _ivdOffsets;

        private ItemVariationStore(
            ReadOnlyMemory<byte> data,
            int axisCount,
            int regionCount,
            int regionListStart,
            int ivdCount,
            int[] ivdOffsets)
        {
            _data = data;
            _axisCount = axisCount;
            _regionCount = regionCount;
            _regionListStart = regionListStart;
            _ivdCount = ivdCount;
            _ivdOffsets = ivdOffsets;
        }

        public int AxisCount => _axisCount;

        public int RegionCount => _regionCount;

        public int ItemVariationDataCount => _ivdCount;

        /// <summary>
        /// Loads an ItemVariationStore from the given bytes. The data must start at the
        /// store's own header (the caller should pre-slice the containing table).
        /// </summary>
        public static bool TryLoad(
            ReadOnlyMemory<byte> data,
            int expectedAxisCount,
            [NotNullWhen(true)] out ItemVariationStore? store)
        {
            store = null;

            var span = data.Span;
            if (span.Length < 8)
            {
                return false;
            }

            var format = BinaryPrimitives.ReadUInt16BigEndian(span);
            if (format != SupportedFormat)
            {
                return false;
            }

            var regionListOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(span.Slice(2));
            var ivdCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(6));

            if (regionListOffset + 4 > span.Length)
            {
                return false;
            }

            var axisCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(regionListOffset));
            var regionCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(regionListOffset + 2));

            if (axisCount != expectedAxisCount)
            {
                // Spec invariant: ItemVariationStore axes must match the host font's
                // fvar. If they don't, the offsets into the regions array won't line up.
                return false;
            }

            var regionListStart = regionListOffset + 4;
            var regionListEnd = regionListStart + regionCount * axisCount * 6; // each axis has start/peak/end as int16

            if (regionListEnd > span.Length)
            {
                return false;
            }

            // ItemVariationData offsets array follows the store header.
            var ivdOffsetsStart = 8;
            if (ivdOffsetsStart + ivdCount * 4 > span.Length)
            {
                return false;
            }

            var ivdOffsets = new int[ivdCount];
            for (var i = 0; i < ivdCount; i++)
            {
                ivdOffsets[i] = (int)BinaryPrimitives.ReadUInt32BigEndian(
                    span.Slice(ivdOffsetsStart + i * 4, 4));

                if (ivdOffsets[i] + 6 > span.Length)
                {
                    return false;
                }
            }

            store = new ItemVariationStore(
                data, axisCount, regionCount, regionListStart, ivdCount, ivdOffsets);
            return true;
        }

        /// <summary>
        /// Computes the variation delta for a given <c>(outerIndex, innerIndex)</c>
        /// addressing pair at the supplied active variation coordinates.
        /// </summary>
        /// <param name="outerIndex">Selects the ItemVariationData subtable.</param>
        /// <param name="innerIndex">Selects the item (row) within the subtable.</param>
        /// <param name="activeCoords">Normalized active coords in fvar axis order.</param>
        /// <param name="delta">
        /// On success, the computed delta as a float (an int-valued result for HVAR /
        /// MVAR deltas, but kept as float because the scaler multiplication is the
        /// natural type).
        /// </param>
        /// <returns>
        /// <c>true</c> when the indices and the active coords are valid and the lookup
        /// succeeded; <c>false</c> when an index is out of range, the data is
        /// malformed, or the active coords' axis count doesn't match.
        /// </returns>
        public bool TryGetDelta(int outerIndex, int innerIndex, ReadOnlySpan<float> activeCoords, out float delta)
        {
            delta = 0f;

            if ((uint)outerIndex >= (uint)_ivdCount)
            {
                return false;
            }

            if (activeCoords.Length < _axisCount)
            {
                return false;
            }

            var span = _data.Span;
            var subtableStart = _ivdOffsets[outerIndex];

            var itemCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(subtableStart, 2));
            var wordDeltaCountRaw = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(subtableStart + 2, 2));
            var regionIndexCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(subtableStart + 4, 2));

            if ((uint)innerIndex >= (uint)itemCount)
            {
                return false;
            }

            var useLongFormat = (wordDeltaCountRaw & LongWordsFlag) != 0;
            var wordDeltaCount = wordDeltaCountRaw & WordDeltaCountMask;

            if (wordDeltaCount > regionIndexCount)
            {
                return false;
            }

            // The "wide" delta width is 4 bytes in the long format and 2 bytes
            // otherwise; the "narrow" width is 2 bytes in the long format and 1
            // byte otherwise.
            var wideBytes = useLongFormat ? 4 : 2;
            var narrowBytes = useLongFormat ? 2 : 1;
            var rowBytes = wordDeltaCount * wideBytes + (regionIndexCount - wordDeltaCount) * narrowBytes;

            var regionIndexesStart = subtableStart + 6;
            var deltaDataStart = regionIndexesStart + regionIndexCount * 2;
            var rowStart = deltaDataStart + innerIndex * rowBytes;

            if (rowStart + rowBytes > span.Length)
            {
                return false;
            }

            // Walk the row's deltas, accumulating scaler × delta per associated region.
            var sum = 0f;
            var bytePos = rowStart;

            for (var r = 0; r < regionIndexCount; r++)
            {
                var regionIndex = BinaryPrimitives.ReadUInt16BigEndian(
                    span.Slice(regionIndexesStart + r * 2, 2));

                int rawDelta;
                if (r < wordDeltaCount)
                {
                    // Wide format.
                    if (useLongFormat)
                    {
                        rawDelta = BinaryPrimitives.ReadInt32BigEndian(span.Slice(bytePos, 4));
                        bytePos += 4;
                    }
                    else
                    {
                        rawDelta = BinaryPrimitives.ReadInt16BigEndian(span.Slice(bytePos, 2));
                        bytePos += 2;
                    }
                }
                else
                {
                    // Narrow format.
                    if (useLongFormat)
                    {
                        rawDelta = BinaryPrimitives.ReadInt16BigEndian(span.Slice(bytePos, 2));
                        bytePos += 2;
                    }
                    else
                    {
                        rawDelta = (sbyte)span[bytePos];
                        bytePos += 1;
                    }
                }

                if ((uint)regionIndex >= (uint)_regionCount)
                {
                    // Malformed: region index out of range.
                    return false;
                }

                var scaler = ComputeRegionScaler(regionIndex, activeCoords);
                if (scaler != 0f)
                {
                    sum += scaler * rawDelta;
                }
            }

            delta = sum;
            return true;
        }

        /// <summary>
        /// Computes a single region's scaler against the active variation coordinates.
        /// Returns 0 if any axis is outside the region's <c>[start, end]</c> bracket;
        /// 1 if the active position is exactly at the peak on every axis; a linearly
        /// interpolated value otherwise. This is the standard OpenType variation-region
        /// scaling rule, applied per axis and multiplied across axes.
        /// </summary>
        private float ComputeRegionScaler(int regionIndex, ReadOnlySpan<float> active)
        {
            var span = _data.Span;
            var regionStart = _regionListStart + regionIndex * _axisCount * 6;

            var scaler = 1f;

            for (var a = 0; a < _axisCount; a++)
            {
                var axisStart = regionStart + a * 6;
                var start = BinaryPrimitives.ReadInt16BigEndian(span.Slice(axisStart, 2)) / 16384f;
                var peak = BinaryPrimitives.ReadInt16BigEndian(span.Slice(axisStart + 2, 2)) / 16384f;
                var end = BinaryPrimitives.ReadInt16BigEndian(span.Slice(axisStart + 4, 2)) / 16384f;

                var v = active[a];

                if (peak == 0f)
                {
                    // Axis doesn't contribute to this region (peak at default means the
                    // region is independent of this axis).
                    continue;
                }

                if (start > peak || peak > end)
                {
                    // Malformed region — treat as no contribution rather than divide by
                    // zero.
                    return 0f;
                }

                if (start > 0f && start > v)
                {
                    return 0f;
                }
                if (end < 0f && end < v)
                {
                    return 0f;
                }

                if (v < start || v > end)
                {
                    return 0f;
                }

                if (v == peak)
                {
                    continue;
                }

                if (v < peak)
                {
                    scaler *= (v - start) / (peak - start);
                }
                else
                {
                    scaler *= (end - v) / (end - peak);
                }
            }

            return scaler;
        }
    }
}
