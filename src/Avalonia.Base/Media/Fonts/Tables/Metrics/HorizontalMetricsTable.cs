using System;
using System.Buffers.Binary;
using Avalonia.Media.Fonts.Tables.Variation;

namespace Avalonia.Media.Fonts.Tables.Metrics
{
    internal class HorizontalMetricsTable
    {
        public const string TagName = "hmtx";
        public static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TagName);

        private readonly ReadOnlyMemory<byte> _data;
        private readonly ushort _numOfHMetrics;
        private readonly int _numGlyphs;

        private HorizontalMetricsTable(ReadOnlyMemory<byte> data, ushort numOfHMetrics, int numGlyphs)
        {
            _data = data;

            // Clamp to what the table can actually hold: a numberOfHMetrics larger than the hmtx
            // bytes — or an out-of-spec 0 — would otherwise drive negative or out-of-range reads in
            // the accessors. A well-formed table always holds numberOfHMetrics * 4 bytes, so it is
            // unaffected.
            _numOfHMetrics = (ushort)Math.Min((int)numOfHMetrics, data.Length / 4);
            _numGlyphs = numGlyphs;
        }

        internal static HorizontalMetricsTable? Load(GlyphTypeface glyphTypeface, ushort numberOfHMetrics, int glyphCount)
        {
            if (glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var table))
            {
                return new HorizontalMetricsTable(table, numberOfHMetrics, glyphCount);
            }

            return null;
        }

        /// <summary>
        /// Attempts to retrieve the horizontal glyph metrics for the specified glyph index.
        /// </summary>
        /// <param name="glyphIndex">The index of the glyph for which to retrieve metrics.</param>
        /// <param name="metric">When this method returns, contains the horizontal glyph metric if the glyph index is valid; otherwise, the default value.</param>
        /// <returns><c>true</c> if the glyph index is valid and metrics were retrieved; otherwise, <c>false</c>.</returns>
        public bool TryGetMetrics(ushort glyphIndex, out HorizontalGlyphMetric metric)
        {
            metric = default;

            if (glyphIndex >= _numGlyphs || _numOfHMetrics == 0)
            {
                return false;
            }

            var reader = new BigEndianBinaryReader(_data.Span);

            if (glyphIndex < _numOfHMetrics)
            {
                reader.Seek(glyphIndex * 4);

                ushort advanceWidth = reader.ReadUInt16();
                short leftSideBearing = reader.ReadInt16();

                metric = new HorizontalGlyphMetric(advanceWidth, leftSideBearing);
            }
            else
            {
                reader.Seek((_numOfHMetrics - 1) * 4);

                ushort lastAdvanceWidth = reader.ReadUInt16();

                int lsbIndex = glyphIndex - _numOfHMetrics;
                int lsbOffset = _numOfHMetrics * 4 + lsbIndex * 2;

                // A truncated table may omit some or all of the trailing leftSideBearing array;
                // treat a missing entry as zero rather than reading past the end.
                short leftSideBearing = 0;
                if (lsbOffset + 2 <= _data.Length)
                {
                    reader.Seek(lsbOffset);
                    leftSideBearing = reader.ReadInt16();
                }

                metric = new HorizontalGlyphMetric(lastAdvanceWidth, leftSideBearing);
            }

            return true;
        }

        /// <summary>
        /// Attempts to retrieve the advance width for a single glyph.
        /// </summary>
        /// <param name="glyphIndex">Glyph index to query.</param>
        /// <param name="advance">When this method returns, contains the advance width if the glyph index is valid; otherwise, zero.</param>
        /// <returns><c>true</c> if the glyph index is valid and the advance was retrieved; otherwise, <c>false</c>.</returns>
        public bool TryGetAdvance(ushort glyphIndex, out ushort advance)
        {
            advance = 0;

            if (glyphIndex >= _numGlyphs || _numOfHMetrics == 0)
            {
                return false;
            }

            var reader = new BigEndianBinaryReader(_data.Span);

            if (glyphIndex < _numOfHMetrics)
            {
                reader.Seek(glyphIndex * 4);

                advance = reader.ReadUInt16();
            }
            else
            {
                reader.Seek((_numOfHMetrics - 1) * 4);

                advance = reader.ReadUInt16();
            }

            return true;
        }

        /// <summary>
        /// Attempts to retrieve advance widths for multiple glyphs in a single operation.
        /// When <paramref name="hvar"/> and <paramref name="hvarRegionScalers"/> are both
        /// supplied, HVAR's per-glyph delta is applied to each advance in the same
        /// pass — fused so <paramref name="advances"/> is written exactly once per
        /// glyph instead of once by hmtx and again by an HVAR post-pass.
        /// </summary>
        /// <param name="glyphIndices">Read-only span of glyph indices to query.</param>
        /// <param name="advances">Output span; must be at least as long as <paramref name="glyphIndices"/>.</param>
        /// <param name="hvar">Optional HVAR table. <c>null</c> means no variation adjustment.</param>
        /// <param name="hvarRegionScalers">
        /// Pre-computed per-region scalers for HVAR's <see cref="ItemVariationStore"/>,
        /// produced once at clone time by
        /// <see cref="ItemVariationStore.ComputeRegionScalers"/>. Ignored when
        /// <paramref name="hvar"/> is <c>null</c>; otherwise length must equal
        /// HVAR's region count. Replaces the activeCoords parameter so the per-glyph
        /// loop never recomputes a scaler — see the planning doc's O-1 hypothesis.
        /// </param>
        /// <returns><c>true</c> if all glyph indices are valid and advances were retrieved; otherwise, <c>false</c>.</returns>
        public bool TryGetAdvances(
            ReadOnlySpan<ushort> glyphIndices,
            Span<ushort> advances,
            HvarTable? hvar = null,
            ReadOnlySpan<float> hvarRegionScalers = default)
        {
            if (advances.Length < glyphIndices.Length)
            {
                return false;
            }

            if (_numOfHMetrics == 0)
            {
                return false;
            }

            var data = _data.Span;

            // Cache the last advance width for glyphs beyond numOfHMetrics
            ushort? lastAdvanceWidth = null;
            var hasHvar = hvar is not null && !hvarRegionScalers.IsEmpty;

            for (int i = 0; i < glyphIndices.Length; i++)
            {
                ushort glyphIndex = glyphIndices[i];

                if (glyphIndex >= _numGlyphs)
                {
                    return false;
                }

                ushort raw;
                if (glyphIndex < _numOfHMetrics)
                {
                    raw = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(glyphIndex * 4, 2));
                }
                else
                {
                    if (!lastAdvanceWidth.HasValue)
                    {
                        lastAdvanceWidth = BinaryPrimitives.ReadUInt16BigEndian(
                            data.Slice((_numOfHMetrics - 1) * 4, 2));
                    }
                    raw = lastAdvanceWidth.Value;
                }

                if (hasHvar && hvar!.TryGetAdvanceDeltaWithScalers(glyphIndex, hvarRegionScalers, out var delta) && delta != 0f)
                {
                    var adjusted = raw + (int)MathF.Round(delta);
                    advances[i] = adjusted < 0
                        ? (ushort)0
                        : (ushort)Math.Min(adjusted, ushort.MaxValue);
                }
                else
                {
                    advances[i] = raw;
                }
            }

            return true;
        }

        /// <summary>
        /// Attempts to retrieve horizontal glyph metrics for multiple glyphs in a single
        /// operation, optionally applying HVAR variation deltas in the same pass.
        /// </summary>
        /// <param name="glyphIndices">Read-only span of glyph indices to query.</param>
        /// <param name="metrics">Output span; must be at least as long as <paramref name="glyphIndices"/>.</param>
        /// <param name="hvar">Optional HVAR table for variation-adjusted advances + LSBs.</param>
        /// <param name="hvarRegionScalers">Pre-computed per-region scalers for HVAR's ItemVariationStore.</param>
        /// <returns><c>true</c> if all glyph indices are valid and metrics were retrieved; otherwise, <c>false</c>.</returns>
        public bool TryGetMetrics(
            ReadOnlySpan<ushort> glyphIndices,
            Span<HorizontalGlyphMetric> metrics,
            HvarTable? hvar = null,
            ReadOnlySpan<float> hvarRegionScalers = default)
        {
            if (metrics.Length < glyphIndices.Length)
            {
                return false;
            }

            if (_numOfHMetrics == 0)
            {
                return false;
            }

            var data = _data.Span;

            // Cache the last advance width for glyphs beyond numOfHMetrics
            ushort? lastAdvanceWidth = null;
            var hasHvar = hvar is not null && !hvarRegionScalers.IsEmpty;

            for (int i = 0; i < glyphIndices.Length; i++)
            {
                ushort glyphIndex = glyphIndices[i];

                if (glyphIndex >= _numGlyphs)
                {
                    return false;
                }

                ushort advanceWidth;
                short leftSideBearing;

                if (glyphIndex < _numOfHMetrics)
                {
                    var entryOffset = glyphIndex * 4;
                    advanceWidth = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(entryOffset, 2));
                    leftSideBearing = BinaryPrimitives.ReadInt16BigEndian(data.Slice(entryOffset + 2, 2));
                }
                else
                {
                    if (!lastAdvanceWidth.HasValue)
                    {
                        lastAdvanceWidth = BinaryPrimitives.ReadUInt16BigEndian(
                            data.Slice((_numOfHMetrics - 1) * 4, 2));
                    }
                    advanceWidth = lastAdvanceWidth.Value;

                    var lsbIndex = glyphIndex - _numOfHMetrics;
                    var lsbOffset = _numOfHMetrics * 4 + lsbIndex * 2;

                    // A truncated table may omit some or all of the trailing leftSideBearing array;
                    // treat a missing entry as zero rather than reading past the end.
                    leftSideBearing = lsbOffset + 2 <= data.Length
                        ? BinaryPrimitives.ReadInt16BigEndian(data.Slice(lsbOffset, 2))
                        : (short)0;
                }

                if (hasHvar)
                {
                    if (hvar!.TryGetAdvanceDeltaWithScalers(glyphIndex, hvarRegionScalers, out var advDelta) && advDelta != 0f)
                    {
                        var adjusted = advanceWidth + (int)MathF.Round(advDelta);
                        advanceWidth = adjusted < 0
                            ? (ushort)0
                            : (ushort)Math.Min(adjusted, ushort.MaxValue);
                    }

                    if (hvar.TryGetLeftSideBearingDeltaWithScalers(glyphIndex, hvarRegionScalers, out var lsbDelta) && lsbDelta != 0f)
                    {
                        var adjusted = leftSideBearing + (int)MathF.Round(lsbDelta);
                        leftSideBearing = (short)Math.Clamp(adjusted, short.MinValue, short.MaxValue);
                    }
                }

                metrics[i] = new HorizontalGlyphMetric(advanceWidth, leftSideBearing);
            }

            return true;
        }
    }
}
