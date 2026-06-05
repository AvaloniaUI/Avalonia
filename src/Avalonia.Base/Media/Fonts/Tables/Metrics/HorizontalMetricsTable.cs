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
            _numOfHMetrics = numOfHMetrics;
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

            if (glyphIndex >= _numGlyphs)
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

                reader.Seek(lsbOffset);

                short leftSideBearing = reader.ReadInt16();

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

            if (glyphIndex >= _numGlyphs)
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
        /// When <paramref name="hvar"/> and <paramref name="activeCoords"/> are both
        /// supplied, HVAR's per-glyph delta is applied to each advance in the same
        /// pass — fused so <paramref name="advances"/> is written exactly once per
        /// glyph instead of once by hmtx and again by an HVAR post-pass.
        /// </summary>
        /// <param name="glyphIndices">Read-only span of glyph indices to query.</param>
        /// <param name="advances">Output span; must be at least as long as <paramref name="glyphIndices"/>.</param>
        /// <param name="hvar">Optional HVAR table. <c>null</c> means no variation adjustment.</param>
        /// <param name="activeCoords">
        /// Normalized variation coordinates in fvar axis order. Ignored when
        /// <paramref name="hvar"/> is <c>null</c>; otherwise must match the font's axis count.
        /// </param>
        /// <returns><c>true</c> if all glyph indices are valid and advances were retrieved; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Reuses the <c>lastAdvanceWidth</c> cache for monospace-tail glyphs, so a
        /// font where most glyphs share the same advance pays one hmtx read regardless
        /// of batch size. If any glyph index is invalid, the method returns <c>false</c>
        /// and the contents of <paramref name="advances"/> are undefined.
        /// </remarks>
        public bool TryGetAdvances(
            ReadOnlySpan<ushort> glyphIndices,
            Span<ushort> advances,
            HvarTable? hvar = null,
            ReadOnlySpan<float> activeCoords = default)
        {
            if (advances.Length < glyphIndices.Length)
            {
                return false;
            }

            var data = _data.Span;

            // Cache the last advance width for glyphs beyond numOfHMetrics
            ushort? lastAdvanceWidth = null;
            var hasHvar = hvar is not null && !activeCoords.IsEmpty;

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

                if (hasHvar && hvar!.TryGetAdvanceDelta(glyphIndex, activeCoords, out var delta) && delta != 0f)
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
        /// <param name="activeCoords">Normalized variation coordinates in fvar axis order.</param>
        /// <returns><c>true</c> if all glyph indices are valid and metrics were retrieved; otherwise, <c>false</c>.</returns>
        public bool TryGetMetrics(
            ReadOnlySpan<ushort> glyphIndices,
            Span<HorizontalGlyphMetric> metrics,
            HvarTable? hvar = null,
            ReadOnlySpan<float> activeCoords = default)
        {
            if (metrics.Length < glyphIndices.Length)
            {
                return false;
            }

            var data = _data.Span;

            // Cache the last advance width for glyphs beyond numOfHMetrics
            ushort? lastAdvanceWidth = null;
            var hasHvar = hvar is not null && !activeCoords.IsEmpty;

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
                    leftSideBearing = BinaryPrimitives.ReadInt16BigEndian(data.Slice(lsbOffset, 2));
                }

                if (hasHvar)
                {
                    if (hvar!.TryGetAdvanceDelta(glyphIndex, activeCoords, out var advDelta) && advDelta != 0f)
                    {
                        var adjusted = advanceWidth + (int)MathF.Round(advDelta);
                        advanceWidth = adjusted < 0
                            ? (ushort)0
                            : (ushort)Math.Min(adjusted, ushort.MaxValue);
                    }

                    if (hvar.TryGetLeftSideBearingDelta(glyphIndex, activeCoords, out var lsbDelta) && lsbDelta != 0f)
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
