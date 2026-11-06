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

            // Keep offsets unsigned and compare against a non-overflowing bound: an additive check
            // like `offset + n > length` lets a high-bit (negative-when-cast) or near-int.MaxValue
            // offset slip through and then slice out of range.
            var regionListOffset = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(2));
            var ivdCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(6));

            // axisCount + regionCount (4 bytes) live at regionListOffset.
            if (regionListOffset > (uint)(span.Length - 4))
            {
                return false;
            }

            var regionListOffsetInt = (int)regionListOffset;
            var axisCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(regionListOffsetInt));
            var regionCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(regionListOffsetInt + 2));

            if (axisCount != expectedAxisCount)
            {
                // Spec invariant: ItemVariationStore axes must match the host font's
                // fvar. If they don't, the offsets into the regions array won't line up.
                return false;
            }

            var regionListStart = regionListOffsetInt + 4;

            // Widen to long: regionCount (up to 65535) × axisCount × 6 can exceed int range.
            var regionListEnd = (long)regionListStart + (long)regionCount * axisCount * 6; // start/peak/end int16 per axis
            if (regionListEnd > span.Length)
            {
                return false;
            }

            // ItemVariationData offsets array follows the store header.
            var ivdOffsetsStart = 8;
            if (ivdOffsetsStart + (long)ivdCount * 4 > span.Length)
            {
                return false;
            }

            var ivdOffsets = new int[ivdCount];
            for (var i = 0; i < ivdCount; i++)
            {
                var ivdOffset = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(ivdOffsetsStart + i * 4, 4));

                // Each ItemVariationData subtable header is at least 6 bytes; reject out-of-range
                // offsets (including high-bit values) before storing them as int.
                if (ivdOffset > (uint)(span.Length - 6))
                {
                    return false;
                }

                ivdOffsets[i] = (int)ivdOffset;
            }

            store = new ItemVariationStore(
                data, axisCount, regionCount, regionListStart, ivdCount, ivdOffsets);
            return true;
        }

        /// <summary>
        /// Pre-computes the per-region scaler for every region declared by the store
        /// against the supplied active coordinates. Fills <paramref name="output"/>
        /// with one scaler per region (length must be at least <see cref="RegionCount"/>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// The store's regions are constant for the lifetime of the font, and the
        /// active coordinates are constant for the lifetime of a varied
        /// <see cref="GlyphTypeface"/> clone — so the resulting scaler vector is
        /// constant per clone. Pre-computing once at clone time saves the per-axis
        /// region-scaler computation inside <see cref="TryGetDelta(int,int,ReadOnlySpan{float},out float)"/>
        /// on every per-glyph lookup. The
        /// <see cref="TryGetDelta(int,int,ReadOnlySpan{float},out float)"/> overload
        /// then becomes a straight index lookup.
        /// </para>
        /// <para>
        /// Measured win on <c>AdvanceLookupBenchmark.VariableVaried_Batch200</c>:
        /// per-glyph HVAR lookup drops dramatically because the inner loop's
        /// <c>ComputeRegionScaler</c> call — which read <c>regionIndexCount × axisCount</c>
        /// F2DOT14 values plus per-axis branches — is replaced by an array index.
        /// </para>
        /// </remarks>
        public void ComputeRegionScalers(ReadOnlySpan<float> activeCoords, Span<float> output)
        {
            for (var r = 0; r < _regionCount; r++)
            {
                output[r] = ComputeRegionScaler(r, activeCoords);
            }
        }

        /// <summary>
        /// Computes the per-region scalers for the regions referenced by ItemVariationData
        /// <paramref name="ivdIndex"/> — a CFF2 <c>vsindex</c> — in subtable order, against the active
        /// coordinates. The result aligns with the delta columns a CFF2 <c>blend</c> at this vsindex
        /// consumes per value. Fills <paramref name="output"/> and returns the region (delta) count,
        /// or <c>-1</c> if the index is invalid, the axes don't match, or <paramref name="output"/>
        /// is too small.
        /// </summary>
        public int ComputeBlendScalers(int ivdIndex, ReadOnlySpan<float> activeCoords, Span<float> output)
        {
            if ((uint)ivdIndex >= (uint)_ivdCount || activeCoords.Length < _axisCount)
            {
                return -1;
            }

            var span = _data.Span;
            var subtableStart = _ivdOffsets[ivdIndex];
            var regionIndexCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(subtableStart + 4, 2));

            if (output.Length < regionIndexCount)
            {
                return -1;
            }

            var regionIndexesStart = subtableStart + 6;

            // TryLoad only validated the 6-byte subtable header fits; the region-index array that
            // follows is read here, so bound it explicitly (TryGetDelta gets this for free via its
            // delta-row check). A truncated array degrades to "no blend" rather than throwing.
            if (regionIndexesStart + regionIndexCount * 2 > span.Length)
            {
                return -1;
            }

            for (var r = 0; r < regionIndexCount; r++)
            {
                var regionIndex = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(regionIndexesStart + r * 2, 2));
                if ((uint)regionIndex >= (uint)_regionCount)
                {
                    return -1;
                }

                output[r] = ComputeRegionScaler(regionIndex, activeCoords);
            }

            return regionIndexCount;
        }

        /// <summary>
        /// Computes the variation delta using a pre-computed per-region scaler array.
        /// Same contract as <see cref="TryGetDelta(int,int,ReadOnlySpan{float},out float)"/>
        /// but with the active-coords → scaler projection lifted out into
        /// <see cref="ComputeRegionScalers"/>.
        /// </summary>
        public bool TryGetDeltaWithScalers(int outerIndex, int innerIndex, ReadOnlySpan<float> regionScalers, out float delta)
        {
            delta = 0f;

            if ((uint)outerIndex >= (uint)_ivdCount)
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

            var wideBytes = useLongFormat ? 4 : 2;
            var narrowBytes = useLongFormat ? 2 : 1;
            var rowBytes = wordDeltaCount * wideBytes + (regionIndexCount - wordDeltaCount) * narrowBytes;

            var regionIndexesStart = subtableStart + 6;
            var deltaDataStart = regionIndexesStart + regionIndexCount * 2;

            // Widen to long before the bounds check: innerIndex × rowBytes overflows int (see
            // TryGetDelta) — the scaler-cached path is reachable from the same public metrics
            // APIs on a varied clone, so it needs the identical guard.
            var rowStart = deltaDataStart + innerIndex * (long)rowBytes;

            if (rowStart + rowBytes > span.Length)
            {
                return false;
            }

            // Same row walk as TryGetDelta, but the per-region scaler comes from the
            // precomputed array instead of being computed from activeCoords.
            var sum = 0f;
            var bytePos = (int)rowStart;

            for (var r = 0; r < regionIndexCount; r++)
            {
                var regionIndex = BinaryPrimitives.ReadUInt16BigEndian(
                    span.Slice(regionIndexesStart + r * 2, 2));

                int rawDelta;
                if (r < wordDeltaCount)
                {
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

                if ((uint)regionIndex >= (uint)regionScalers.Length)
                {
                    return false;
                }

                var scaler = regionScalers[regionIndex];
                if (scaler != 0f)
                {
                    sum += scaler * rawDelta;
                }
            }

            delta = sum;
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

            // Widen to long: innerIndex (≤ ~65534) × rowBytes (≤ ~262140) overflows int, and a
            // negative rowStart would pass the bounds check below only to throw on the slice —
            // reachable from the public metrics APIs with a ~30-byte crafted store.
            var rowStart = deltaDataStart + innerIndex * (long)rowBytes;

            if (rowStart + rowBytes > span.Length)
            {
                return false;
            }

            // Walk the row's deltas, accumulating scaler × delta per associated region.
            var sum = 0f;
            var bytePos = (int)rowStart;

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
