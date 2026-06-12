using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Avalonia.Media.Fonts.Tables.Variation
{
    /// <summary>
    /// Decodes a single glyph's TupleVariationStore from <c>gvar</c> and accumulates the
    /// per-point x/y deltas implied by the active variation coordinates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The reader walks the tuple variation headers, computes each tuple's scaler against
    /// the active variation point (peak-only or intermediate-region forms), reads the
    /// packed point numbers and packed deltas for each tuple, and accumulates
    /// <c>scaler * delta</c> into the output spans. For tuples that reference only a
    /// subset of points (private point list or shared point list other than "all
    /// points"), the unreferenced points get IUP-interpolated deltas per the OpenType
    /// spec.
    /// </para>
    /// <para>
    /// Reference: <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats"/>
    /// and <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar"/>.
    /// </para>
    /// </remarks>
    internal static class GlyphVariationReader
    {
        // Bits 12-15 of tupleVariationCount in the TupleVariationStore header.
        private const ushort SharedPointNumbersFlag = 0x8000;
        private const ushort TupleVariationCountMask = 0x0FFF;

        // Bits in tupleIndex (per-tuple header).
        private const ushort EmbeddedPeakTupleFlag = 0x8000;
        private const ushort IntermediateRegionFlag = 0x4000;
        private const ushort PrivatePointNumbersFlag = 0x2000;
        private const ushort TupleIndexMask = 0x0FFF;

        // Packed delta run header bits.
        private const byte DeltasAreZeroFlag = 0x80;
        private const byte DeltasAreWordsFlag = 0x40;
        private const byte DeltaRunCountMask = 0x3F;


        /// <summary>
        /// Applies all tuple variations for the specified glyph to the provided delta
        /// accumulators. <paramref name="xDeltas"/> and <paramref name="yDeltas"/> must
        /// be zero-initialized by the caller; this method only adds.
        /// </summary>
        /// <param name="gvar">The font's gvar table.</param>
        /// <param name="glyphIndex">The glyph to look up.</param>
        /// <param name="activeCoords">
        /// Normalized variation coordinates in axis order. Length must equal
        /// <see cref="GvarTable.AxisCount"/>.
        /// </param>
        /// <param name="endPtsOfContours">Contour endpoints from the parsed simple glyph.</param>
        /// <param name="originalX">Original (pre-variation) x coordinates of the glyph's points.</param>
        /// <param name="originalY">Original y coordinates.</param>
        /// <param name="xDeltas">Output: accumulated x deltas, one per glyph point.</param>
        /// <param name="yDeltas">Output: accumulated y deltas, one per glyph point.</param>
        /// <returns>
        /// <c>true</c> if any deltas were applied; <c>false</c> for glyphs with no gvar
        /// entry, or when every tuple's scaler comes out as zero (variation point doesn't
        /// activate anything for this glyph).
        /// </returns>
        public static bool TryApplyDeltas(
            GvarTable gvar,
            int glyphIndex,
            ReadOnlySpan<float> activeCoords,
            ReadOnlySpan<ushort> endPtsOfContours,
            ReadOnlySpan<short> originalX,
            ReadOnlySpan<short> originalY,
            Span<float> xDeltas,
            Span<float> yDeltas)
        {
            if (!gvar.TryGetGlyphVariationData(glyphIndex, out var entryMem))
            {
                return false;
            }

            var entry = entryMem.Span;
            if (entry.Length < 4)
            {
                return false;
            }

            var contourPointCount = originalX.Length;
            // gvar's point space includes 4 phantom points appended after the contour
            // points (left/right side-bearing + top/bottom advance). We need them to
            // correctly parse packed point numbers (which can reference them) but never
            // apply deltas to them because they don't appear in the geometry.
            var totalPointCount = contourPointCount + 4;

            var tupleVariationCountRaw = BinaryPrimitives.ReadUInt16BigEndian(entry);
            var hasSharedPointNumbers = (tupleVariationCountRaw & SharedPointNumbersFlag) != 0;
            var tupleVariationCount = tupleVariationCountRaw & TupleVariationCountMask;

            var dataOffsetFromEntryStart = BinaryPrimitives.ReadUInt16BigEndian(entry.Slice(2));

            if (tupleVariationCount == 0)
            {
                return false;
            }

            // The serialized-data offset is attacker-controlled; past the entry end there is no
            // tuple data to read, so the glyph renders undeformed.
            if (dataOffsetFromEntryStart > entry.Length)
            {
                return false;
            }

            var axisCount = gvar.AxisCount;
            Span<float> peak = stackalloc float[axisCount];
            Span<float> intermediateStart = stackalloc float[axisCount];
            Span<float> intermediateEnd = stackalloc float[axisCount];

            // The tuple variation headers run from offset 4 (after tvCount + dataOffset)
            // up to dataOffsetFromEntryStart. The serialized per-tuple data follows
            // contiguously starting at dataOffsetFromEntryStart.
            // Both positions are int-typed so subsequent additions of ushort values
            // (variationDataSize, run lengths) widen without a narrowing cast.
            int headerPos = 4;
            int dataPos = dataOffsetFromEntryStart;

            // Shared point numbers list (when used by any tuple). Read once before the
            // per-tuple loop. ReadPackedPointNumbers advances dataPos past the list.
            int[]? sharedPointsRented = null;
            int sharedPointsCount = 0;
            var sharedPointsIsAll = false;

            if (hasSharedPointNumbers)
            {
                if (!TryReadPackedPointNumbers(
                        entry,
                        ref dataPos,
                        entry.Length,
                        out sharedPointsRented,
                        out sharedPointsCount,
                        out sharedPointsIsAll,
                        totalPointCount))
                {
                    // The shared point list precedes every tuple's serialized data, so when it
                    // over-runs there is no way to locate any tuple data. Render undeformed.
                    return false;
                }
            }

            // Per-tuple scratch buffers for the actual point indices the tuple operates
            // on and the raw deltas (before scaler is applied). These are rented per
            // call and pooled — gvar entries can address hundreds of points per tuple
            // so stackalloc would risk overflow.
            int[]? tuplePointsRented = null;
            float[]? tupleDeltasXRented = null;
            float[]? tupleDeltasYRented = null;
            float[]? iupBufferXRented = null;
            float[]? iupBufferYRented = null;
            bool[]? referencedRented = null;

            try
            {
                var appliedAny = false;

                for (var t = 0; t < tupleVariationCount; t++)
                {
                    // The tuple headers are structural — later headers and all data offsets
                    // derive from them — so when one is truncated there is nothing safe left
                    // to read; keep whatever earlier tuples contributed.
                    if (headerPos + 4 > entry.Length)
                    {
                        break;
                    }

                    var tupleDataSize = BinaryPrimitives.ReadUInt16BigEndian(entry.Slice(headerPos));
                    var tupleIndex = BinaryPrimitives.ReadUInt16BigEndian(entry.Slice(headerPos + 2));
                    headerPos += 4;

                    var hasEmbeddedPeak = (tupleIndex & EmbeddedPeakTupleFlag) != 0;
                    var hasIntermediateRegion = (tupleIndex & IntermediateRegionFlag) != 0;
                    var hasPrivatePoints = (tupleIndex & PrivatePointNumbersFlag) != 0;
                    var sharedTupleIndex = tupleIndex & TupleIndexMask;

                    var embeddedCoordinateCount = (hasEmbeddedPeak ? axisCount : 0)
                        + (hasIntermediateRegion ? axisCount * 2 : 0);

                    if (headerPos + embeddedCoordinateCount * 2 > entry.Length)
                    {
                        break;
                    }

                    if (hasEmbeddedPeak)
                    {
                        ReadF2dot14Array(entry, ref headerPos, peak);
                    }
                    else
                    {
                        if (!gvar.TryGetSharedTuple(sharedTupleIndex, peak))
                        {
                            // Malformed: index out of range. Skip this tuple's data.
                            dataPos += tupleDataSize;
                            continue;
                        }
                    }

                    if (hasIntermediateRegion)
                    {
                        ReadF2dot14Array(entry, ref headerPos, intermediateStart);
                        ReadF2dot14Array(entry, ref headerPos, intermediateEnd);
                    }

                    // Compute scaler against the active variation point.
                    var scaler = hasIntermediateRegion
                        ? ComputeScalerIntermediate(activeCoords, peak, intermediateStart, intermediateEnd)
                        : ComputeScalerPeak(activeCoords, peak);

                    // The serialized data for this tuple is at [dataPos, dataPos + tupleDataSize).
                    // Note: we must always advance dataPos by tupleDataSize regardless of
                    // whether we apply the scaler — the next tuple's serialized data
                    // starts where this one ends. The end is clamped to the entry: a hostile
                    // tupleDataSize must not let reads (or later tuples) escape the entry, and
                    // every read below is bounded by this end so a lying size corrupts at most
                    // this tuple, never its neighbors.
                    var tupleDataEnd = Math.Min(dataPos + tupleDataSize, entry.Length);

                    if (scaler == 0f)
                    {
                        // Tuple makes no contribution. Skip to next.
                        dataPos = tupleDataEnd;
                        continue;
                    }

                    // Resolve which points this tuple operates on.
                    int[]? tuplePoints;
                    int tuplePointCount;
                    bool tuplePointsIsAll;

                    if (hasPrivatePoints)
                    {
                        if (tuplePointsRented != null)
                        {
                            ArrayPool<int>.Shared.Return(tuplePointsRented);
                            tuplePointsRented = null;
                        }

                        if (!TryReadPackedPointNumbers(
                                entry,
                                ref dataPos,
                                tupleDataEnd,
                                out tuplePointsRented,
                                out tuplePointCount,
                                out tuplePointsIsAll,
                                totalPointCount))
                        {
                            // Over-reading point list — this tuple's data is unusable; skip it
                            // and keep processing the remaining (independent) tuples.
                            dataPos = tupleDataEnd;
                            continue;
                        }

                        tuplePoints = tuplePointsRented;
                    }
                    else if (hasSharedPointNumbers)
                    {
                        tuplePoints = sharedPointsRented;
                        tuplePointCount = sharedPointsCount;
                        tuplePointsIsAll = sharedPointsIsAll;
                    }
                    else
                    {
                        // No shared and no private — implicit "all points".
                        tuplePoints = null;
                        tuplePointCount = totalPointCount;
                        tuplePointsIsAll = true;
                    }

                    // Read packed x and y deltas. There are tuplePointCount values for
                    // each axis. We rent buffers sized for the worst case (totalPointCount)
                    // and reuse across tuples.
                    if (tupleDeltasXRented == null || tupleDeltasXRented.Length < tuplePointCount)
                    {
                        if (tupleDeltasXRented != null)
                        {
                            ArrayPool<float>.Shared.Return(tupleDeltasXRented);
                            ArrayPool<float>.Shared.Return(tupleDeltasYRented!);
                        }
                        tupleDeltasXRented = ArrayPool<float>.Shared.Rent(tuplePointCount);
                        tupleDeltasYRented = ArrayPool<float>.Shared.Rent(tuplePointCount);
                    }

                    var tupleDeltasX = tupleDeltasXRented.AsSpan(0, tuplePointCount);
                    var tupleDeltasY = tupleDeltasYRented!.AsSpan(0, tuplePointCount);

                    if (!TryReadPackedDeltas(entry, ref dataPos, tupleDataEnd, tupleDeltasX) ||
                        !TryReadPackedDeltas(entry, ref dataPos, tupleDataEnd, tupleDeltasY))
                    {
                        // Delta streams over-run the tuple's data region: skip this tuple
                        // without applying the partial values; later tuples are unaffected.
                        dataPos = tupleDataEnd;
                        continue;
                    }

                    // Apply deltas to the accumulators.
                    if (tuplePointsIsAll)
                    {
                        // tupleDeltasX/Y are indexed 0..totalPointCount-1 (including
                        // phantom points at the tail). Apply only to the contour points.
                        var n = Math.Min(contourPointCount, tupleDeltasX.Length);
                        for (var i = 0; i < n; i++)
                        {
                            xDeltas[i] += scaler * tupleDeltasX[i];
                            yDeltas[i] += scaler * tupleDeltasY[i];
                        }
                    }
                    else
                    {
                        // Subset of points referenced. Apply at the referenced indices
                        // directly, then IUP-interpolate deltas for unreferenced contour
                        // points and apply those too.
                        if (iupBufferXRented == null)
                        {
                            iupBufferXRented = ArrayPool<float>.Shared.Rent(contourPointCount);
                            iupBufferYRented = ArrayPool<float>.Shared.Rent(contourPointCount);
                        }

                        var iupX = iupBufferXRented.AsSpan(0, contourPointCount);
                        var iupY = iupBufferYRented!.AsSpan(0, contourPointCount);
                        iupX.Clear();
                        iupY.Clear();

                        // referenced[i] = true means point i has an explicit delta in
                        // this tuple. False means IUP should interpolate. Pool the array
                        // across tuples — re-clearing is cheaper than re-renting.
                        if (referencedRented == null)
                        {
                            referencedRented = ArrayPool<bool>.Shared.Rent(contourPointCount);
                        }
                        var referenced = referencedRented.AsSpan(0, contourPointCount);
                        referenced.Clear();

                        for (var i = 0; i < tuplePointCount; i++)
                        {
                            var pointIndex = tuplePoints![i];

                            // Phantom points (index >= contourPointCount) — record their
                            // deltas would be needed for HVAR/VVAR but not for geometry,
                            // so skip.
                            if ((uint)pointIndex >= (uint)contourPointCount)
                            {
                                continue;
                            }

                            iupX[pointIndex] = tupleDeltasX[i];
                            iupY[pointIndex] = tupleDeltasY[i];
                            referenced[pointIndex] = true;
                        }

                        ApplyIup(referenced, originalX, originalY, endPtsOfContours, iupX, iupY);

                        for (var i = 0; i < contourPointCount; i++)
                        {
                            xDeltas[i] += scaler * iupX[i];
                            yDeltas[i] += scaler * iupY[i];
                        }
                    }

                    appliedAny = true;
                    dataPos = tupleDataEnd;
                }

                return appliedAny;
            }
            finally
            {
                if (sharedPointsRented != null)
                {
                    ArrayPool<int>.Shared.Return(sharedPointsRented);
                }
                if (tuplePointsRented != null)
                {
                    ArrayPool<int>.Shared.Return(tuplePointsRented);
                }
                if (tupleDeltasXRented != null)
                {
                    ArrayPool<float>.Shared.Return(tupleDeltasXRented);
                }
                if (tupleDeltasYRented != null)
                {
                    ArrayPool<float>.Shared.Return(tupleDeltasYRented);
                }
                if (iupBufferXRented != null)
                {
                    ArrayPool<float>.Shared.Return(iupBufferXRented);
                }
                if (iupBufferYRented != null)
                {
                    ArrayPool<float>.Shared.Return(iupBufferYRented);
                }
                if (referencedRented != null)
                {
                    ArrayPool<bool>.Shared.Return(referencedRented);
                }
            }
        }

        // ----- Scaler -----

        /// <summary>
        /// Computes the tuple scaler for the non-intermediate case (peak coordinates only).
        /// Returns 0 if the tuple makes no contribution at the active variation point.
        /// </summary>
        internal static float ComputeScalerPeak(
            ReadOnlySpan<float> active,
            ReadOnlySpan<float> peak)
        {
            var scaler = 1f;

            for (var i = 0; i < peak.Length; i++)
            {
                var p = peak[i];
                var a = active[i];

                if (p == 0f)
                {
                    // Axis doesn't contribute to this tuple — multiplicative identity.
                    continue;
                }

                if (a == 0f)
                {
                    // Active at default but tuple peaks elsewhere — no contribution.
                    return 0f;
                }

                if ((a > 0f) != (p > 0f))
                {
                    // Opposite sides of the default — no contribution.
                    return 0f;
                }

                var absA = a < 0f ? -a : a;
                var absP = p < 0f ? -p : p;

                if (absA >= absP)
                {
                    // At or past peak — fully active (clamped at 1).
                    continue;
                }

                scaler *= a / p;
            }

            return scaler;
        }

        /// <summary>
        /// Computes the tuple scaler for the intermediate-region case. The tuple ramps
        /// linearly from 0 at <paramref name="intermediateStart"/> up to 1 at
        /// <paramref name="peak"/> and back down to 0 at <paramref name="intermediateEnd"/>.
        /// </summary>
        internal static float ComputeScalerIntermediate(
            ReadOnlySpan<float> active,
            ReadOnlySpan<float> peak,
            ReadOnlySpan<float> intermediateStart,
            ReadOnlySpan<float> intermediateEnd)
        {
            var scaler = 1f;

            for (var i = 0; i < peak.Length; i++)
            {
                var p = peak[i];
                var a = active[i];

                if (p == 0f)
                {
                    continue;
                }

                if (a == 0f || (a > 0f) != (p > 0f))
                {
                    return 0f;
                }

                if (a == p)
                {
                    continue;
                }

                var s = intermediateStart[i];
                var e = intermediateEnd[i];

                if (a < s || a > e)
                {
                    return 0f;
                }

                if (a < p)
                {
                    scaler *= (a - s) / (p - s);
                }
                else
                {
                    scaler *= (e - a) / (e - p);
                }
            }

            return scaler;
        }

        // ----- Helpers -----

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadF2dot14Array(ReadOnlySpan<byte> data, ref int pos, Span<float> output)
        {
            for (var i = 0; i < output.Length; i++)
            {
                var raw = BinaryPrimitives.ReadInt16BigEndian(data.Slice(pos, 2));
                output[i] = raw / 16384f;
                pos += 2;
            }
        }

        /// <summary>
        /// Reads a packed point-numbers list, bounded by <paramref name="end"/>. On success,
        /// <paramref name="rented"/> is an array rented from <see cref="ArrayPool{T}.Shared"/>
        /// (caller is responsible for return) or <c>null</c> when the list means "all points";
        /// the point count is written to <paramref name="count"/>. Returns <c>false</c> (with
        /// <paramref name="rented"/> <c>null</c>, nothing left to return) when the list would
        /// read past <paramref name="end"/> — the data is attacker-controlled and an over-run
        /// must corrupt at most the current tuple.
        /// </summary>
        private static bool TryReadPackedPointNumbers(
            ReadOnlySpan<byte> data,
            ref int pos,
            int end,
            out int[]? rented,
            out int count,
            out bool isAllPoints,
            int totalPointCount)
        {
            rented = null;
            count = 0;
            isAllPoints = false;

            // First the count: 1 byte if < 0x80, else 2 bytes with high bit set.
            if (pos >= end)
            {
                return false;
            }

            var first = data[pos++];
            if (first == 0)
            {
                count = totalPointCount;
                isAllPoints = true;
                return true;
            }

            int countValue;
            if ((first & 0x80) != 0)
            {
                if (pos >= end)
                {
                    return false;
                }

                countValue = ((first & 0x7F) << 8) | data[pos++];
            }
            else
            {
                countValue = first;
            }

            var result = ArrayPool<int>.Shared.Rent(countValue);
            var pointNumber = 0;
            var written = 0;

            while (written < countValue)
            {
                if (pos >= end)
                {
                    ArrayPool<int>.Shared.Return(result);
                    return false;
                }

                var control = data[pos++];
                var runIsWords = (control & 0x80) != 0;
                var runCount = (control & 0x7F) + 1;

                for (var i = 0; i < runCount && written < countValue; i++)
                {
                    int delta;
                    if (runIsWords)
                    {
                        if (pos + 2 > end)
                        {
                            ArrayPool<int>.Shared.Return(result);
                            return false;
                        }

                        delta = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(pos, 2));
                        pos += 2;
                    }
                    else
                    {
                        if (pos >= end)
                        {
                            ArrayPool<int>.Shared.Return(result);
                            return false;
                        }

                        delta = data[pos++];
                    }

                    pointNumber += delta;
                    result[written++] = pointNumber;
                }
            }

            rented = result;
            count = countValue;
            return true;
        }

        /// <summary>
        /// Reads <paramref name="output"/>.Length packed deltas (one axis at a time), bounded
        /// by <paramref name="end"/>. Returns <c>false</c> when the delta stream would read
        /// past <paramref name="end"/>; the output contents are then undefined and the caller
        /// must skip the tuple.
        /// </summary>
        private static bool TryReadPackedDeltas(
            ReadOnlySpan<byte> data,
            ref int pos,
            int end,
            Span<float> output)
        {
            var written = 0;

            while (written < output.Length)
            {
                if (pos >= end)
                {
                    return false;
                }

                var control = data[pos++];
                var allZero = (control & DeltasAreZeroFlag) != 0;
                var areWords = (control & DeltasAreWordsFlag) != 0;
                var runCount = (control & DeltaRunCountMask) + 1;

                if (allZero)
                {
                    var runEnd = Math.Min(written + runCount, output.Length);
                    for (var i = written; i < runEnd; i++)
                    {
                        output[i] = 0f;
                    }
                    written = runEnd;
                }
                else if (areWords)
                {
                    var runEnd = Math.Min(written + runCount, output.Length);

                    if (pos + (runEnd - written) * 2 > end)
                    {
                        return false;
                    }

                    for (var i = written; i < runEnd; i++)
                    {
                        output[i] = BinaryPrimitives.ReadInt16BigEndian(data.Slice(pos, 2));
                        pos += 2;
                    }
                    written = runEnd;
                }
                else
                {
                    var runEnd = Math.Min(written + runCount, output.Length);

                    if (pos + (runEnd - written) > end)
                    {
                        return false;
                    }

                    for (var i = written; i < runEnd; i++)
                    {
                        output[i] = (sbyte)data[pos++];
                    }
                    written = runEnd;
                }
            }

            return true;
        }

        // ----- IUP -----

        /// <summary>
        /// Applies Inferred Unreferenced-Point interpolation per the OpenType spec for
        /// the deltas in <paramref name="deltaX"/> / <paramref name="deltaY"/>. Points
        /// flagged <c>true</c> in <paramref name="referenced"/> keep their explicit
        /// deltas; unreferenced points are interpolated from neighboring referenced
        /// points along their contour.
        /// </summary>
        internal static void ApplyIup(
            ReadOnlySpan<bool> referenced,
            ReadOnlySpan<short> originalX,
            ReadOnlySpan<short> originalY,
            ReadOnlySpan<ushort> endPtsOfContours,
            Span<float> deltaX,
            Span<float> deltaY)
        {
            var contourStart = 0;
            for (var c = 0; c < endPtsOfContours.Length; c++)
            {
                var contourEnd = endPtsOfContours[c];
                if (contourEnd >= referenced.Length)
                {
                    // Defensive: a malformed font could declare an out-of-range contour
                    // endpoint. Skip the remainder rather than risk an out-of-bounds read.
                    break;
                }

                InterpolateContour(
                    referenced.Slice(contourStart, contourEnd - contourStart + 1),
                    originalX.Slice(contourStart, contourEnd - contourStart + 1),
                    deltaX.Slice(contourStart, contourEnd - contourStart + 1));

                InterpolateContour(
                    referenced.Slice(contourStart, contourEnd - contourStart + 1),
                    originalY.Slice(contourStart, contourEnd - contourStart + 1),
                    deltaY.Slice(contourStart, contourEnd - contourStart + 1));

                contourStart = contourEnd + 1;
            }
        }

        /// <summary>
        /// Interpolates deltas along a single contour for one axis. Mutates the
        /// <paramref name="deltas"/> span: referenced positions are unchanged, runs of
        /// unreferenced positions get deltas linearly interpolated from the surrounding
        /// references (or clamped to the nearest reference when outside the bracket).
        /// </summary>
        private static void InterpolateContour(
            ReadOnlySpan<bool> referenced,
            ReadOnlySpan<short> original,
            Span<float> deltas)
        {
            var n = referenced.Length;
            if (n == 0)
            {
                return;
            }

            // Count references. 0 refs = no interpolation possible (all stay 0). 1 ref =
            // all unreferenced points get the same delta as that single reference.
            var refCount = 0;
            var soleRefIndex = -1;
            for (var i = 0; i < n; i++)
            {
                if (referenced[i])
                {
                    refCount++;
                    soleRefIndex = i;
                    if (refCount > 1) break;
                }
            }

            if (refCount == 0)
            {
                return;
            }

            if (refCount == 1)
            {
                var d = deltas[soleRefIndex];
                for (var i = 0; i < n; i++)
                {
                    if (!referenced[i])
                    {
                        deltas[i] = d;
                    }
                }
                return;
            }

            // Walk the contour cyclically, finding each run of unreferenced points
            // bracketed by two references (possibly wrapping around the contour).
            //
            // Locate the first referenced point; from there, iterate around the
            // contour once, accumulating unreferenced runs.
            var firstRef = -1;
            for (var i = 0; i < n; i++)
            {
                if (referenced[i])
                {
                    firstRef = i;
                    break;
                }
            }

            // Walk from firstRef around to firstRef again, processing each run of
            // unreferenced points between two refs.
            var cur = firstRef;
            do
            {
                // Find next reference after cur.
                var next = cur + 1;
                while (next != cur)
                {
                    if (next >= n) next = 0;
                    if (referenced[next]) break;
                    next++;
                    if (next == n) next = 0;
                }

                if (next == cur)
                {
                    // Only one reference total — handled by the refCount==1 branch above.
                    break;
                }

                // Interpolate the unreferenced points between cur and next (exclusive
                // of both endpoints).
                var startPt = original[cur];
                var endPt = original[next];
                var startDelta = deltas[cur];
                var endDelta = deltas[next];

                var i = cur + 1;
                while (i != next)
                {
                    if (i >= n) i = 0;
                    if (i == next) break;

                    var pt = original[i];

                    // Standard IUP rule per OpenType spec:
                    //   if startPt == endPt:
                    //     if startDelta == endDelta: delta[i] = startDelta
                    //     else: delta[i] = 0
                    //   else:
                    //     if pt is between startPt and endPt:
                    //       linear interpolation
                    //     else:
                    //       clamp to nearest reference's delta (with axis shift)
                    if (startPt == endPt)
                    {
                        deltas[i] = startDelta == endDelta ? startDelta : 0f;
                    }
                    else
                    {
                        var lo = startPt < endPt ? startPt : endPt;
                        var hi = startPt < endPt ? endPt : startPt;
                        var loDelta = startPt < endPt ? startDelta : endDelta;
                        var hiDelta = startPt < endPt ? endDelta : startDelta;

                        if (pt < lo)
                        {
                            deltas[i] = loDelta;
                        }
                        else if (pt > hi)
                        {
                            deltas[i] = hiDelta;
                        }
                        else
                        {
                            // Linear interpolation between lo and hi.
                            var t = (float)(pt - lo) / (hi - lo);
                            deltas[i] = loDelta + t * (hiDelta - loDelta);
                        }
                    }

                    i++;
                    if (i == n) i = 0;
                }

                cur = next;
            } while (cur != firstRef);
        }

    }
}
