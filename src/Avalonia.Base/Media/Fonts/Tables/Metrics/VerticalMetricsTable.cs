using System;
using System.Buffers.Binary;
using Avalonia.Media.Fonts.Tables.Variation;

namespace Avalonia.Media.Fonts.Tables.Metrics
{
    internal class VerticalMetricsTable
    {
        public const string TagName = "vmtx";
        public static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TagName);

        private readonly ReadOnlyMemory<byte> _data;
        private readonly ushort _numOfVMetrics;
        private readonly int _numGlyphs;

        private VerticalMetricsTable(ReadOnlyMemory<byte> data, ushort numOfVMetrics, int numGlyphs)
        {
            _data = data;

            // Clamp to what the table can actually hold: a numberOfVMetrics larger than the vmtx
            // bytes — or an out-of-spec 0 — would otherwise drive negative or out-of-range reads in
            // the accessors. A well-formed table always holds numberOfVMetrics * 4 bytes, so it is
            // unaffected.
            _numOfVMetrics = (ushort)Math.Min((int)numOfVMetrics, data.Length / 4);
            _numGlyphs = numGlyphs;
        }

        public static VerticalMetricsTable? Load(GlyphTypeface glyphTypeface, ushort numberOfVMetrics, int glyphCount)
        {
            if (glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var table))
            {
                return new VerticalMetricsTable(table, numberOfVMetrics, glyphCount);
            }

            return null;
        }

        /// <summary>
        /// Attempts to retrieve the vertical glyph metrics for the specified glyph index.
        /// </summary>
        /// <param name="glyphIndex">The index of the glyph for which to retrieve metrics.</param>
        /// <param name="metric">When this method returns, contains the vertical glyph metric if the glyph index is valid; otherwise, the default value.</param>
        /// <returns><c>true</c> if the glyph index is valid and metrics were retrieved; otherwise, <c>false</c>.</returns>
        public bool TryGetMetrics(ushort glyphIndex, out VerticalGlyphMetric metric)
        {
            metric = default;

            if (glyphIndex >= _numGlyphs || _numOfVMetrics == 0)
            {
                return false;
            }

            var reader = new BigEndianBinaryReader(_data.Span);

            if (glyphIndex < _numOfVMetrics)
            {
                reader.Seek(glyphIndex * 4);

                ushort advanceHeight = reader.ReadUInt16();
                short topSideBearing = reader.ReadInt16();

                metric = new VerticalGlyphMetric(advanceHeight, topSideBearing);
            }
            else
            {
                reader.Seek((_numOfVMetrics - 1) * 4);

                ushort lastAdvanceHeight = reader.ReadUInt16();

                int tsbIndex = glyphIndex - _numOfVMetrics;
                int tsbOffset = _numOfVMetrics * 4 + tsbIndex * 2;

                // A truncated table may omit some or all of the trailing topSideBearing array;
                // treat a missing entry as zero rather than reading past the end.
                short tsb = 0;
                if (tsbOffset + 2 <= _data.Length)
                {
                    reader.Seek(tsbOffset);
                    tsb = reader.ReadInt16();
                }

                metric = new VerticalGlyphMetric(lastAdvanceHeight, tsb);
            }

            return true;
        }

        /// <summary>
        /// Attempts to retrieve the advance height for a single glyph.
        /// </summary>
        /// <param name="glyphIndex">Glyph index to query.</param>
        /// <param name="advance">When this method returns, contains the advance height if the glyph index is valid; otherwise, zero.</param>
        /// <returns><c>true</c> if the glyph index is valid and the advance was retrieved; otherwise, <c>false</c>.</returns>
        public bool TryGetAdvance(ushort glyphIndex, out ushort advance)
        {
            advance = 0;

            if (glyphIndex >= _numGlyphs || _numOfVMetrics == 0)
            {
                return false;
            }

            var reader = new BigEndianBinaryReader(_data.Span);

            if (glyphIndex < _numOfVMetrics)
            {
                reader.Seek(glyphIndex * 4);

                advance = reader.ReadUInt16();
            }
            else
            {
                reader.Seek((_numOfVMetrics - 1) * 4);

                advance = reader.ReadUInt16();
            }

            return true;
        }

        /// <summary>
        /// Attempts to retrieve advance heights for multiple glyphs in a single operation.
        /// When <paramref name="vvar"/> and <paramref name="activeCoords"/> are both
        /// supplied, VVAR's per-glyph delta is applied to each advance in the same pass.
        /// </summary>
        /// <param name="glyphIndices">Read-only span of glyph indices to query.</param>
        /// <param name="advances">Output span; must be at least as long as <paramref name="glyphIndices"/>.</param>
        /// <param name="vvar">Optional VVAR table. <c>null</c> means no variation adjustment.</param>
        /// <param name="activeCoords">
        /// Normalized variation coordinates in fvar axis order. Ignored when
        /// <paramref name="vvar"/> is <c>null</c>; otherwise must match the font's axis count.
        /// </param>
        /// <returns><c>true</c> if all glyph indices are valid and advances were retrieved; otherwise, <c>false</c>.</returns>
        public bool TryGetAdvances(
            ReadOnlySpan<ushort> glyphIndices,
            Span<ushort> advances,
            VvarTable? vvar = null,
            ReadOnlySpan<float> activeCoords = default)
        {
            if (advances.Length < glyphIndices.Length)
            {
                return false;
            }

            if (_numOfVMetrics == 0)
            {
                return false;
            }

            var data = _data.Span;

            // Cache the last advance height for glyphs beyond numOfVMetrics
            ushort? lastAdvanceHeight = null;
            var hasVvar = vvar is not null && !activeCoords.IsEmpty;

            for (int i = 0; i < glyphIndices.Length; i++)
            {
                ushort glyphIndex = glyphIndices[i];

                if (glyphIndex >= _numGlyphs)
                {
                    return false;
                }

                ushort raw;
                if (glyphIndex < _numOfVMetrics)
                {
                    raw = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(glyphIndex * 4, 2));
                }
                else
                {
                    if (!lastAdvanceHeight.HasValue)
                    {
                        lastAdvanceHeight = BinaryPrimitives.ReadUInt16BigEndian(
                            data.Slice((_numOfVMetrics - 1) * 4, 2));
                    }
                    raw = lastAdvanceHeight.Value;
                }

                if (hasVvar && vvar!.TryGetAdvanceHeightDelta(glyphIndex, activeCoords, out var delta) && delta != 0f)
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
        /// Attempts to retrieve vertical glyph metrics for multiple glyphs in a single
        /// operation, optionally applying VVAR variation deltas in the same pass.
        /// </summary>
        /// <param name="glyphIndices">Read-only span of glyph indices to query.</param>
        /// <param name="metrics">Output span; must be at least as long as <paramref name="glyphIndices"/>.</param>
        /// <param name="vvar">Optional VVAR table for variation-adjusted advances + TSBs.</param>
        /// <param name="activeCoords">Normalized variation coordinates in fvar axis order.</param>
        /// <returns><c>true</c> if all glyph indices are valid and metrics were retrieved; otherwise, <c>false</c>.</returns>
        public bool TryGetMetrics(
            ReadOnlySpan<ushort> glyphIndices,
            Span<VerticalGlyphMetric> metrics,
            VvarTable? vvar = null,
            ReadOnlySpan<float> activeCoords = default)
        {
            if (metrics.Length < glyphIndices.Length)
            {
                return false;
            }

            if (_numOfVMetrics == 0)
            {
                return false;
            }

            var data = _data.Span;

            // Cache the last advance height for glyphs beyond numOfVMetrics
            ushort? lastAdvanceHeight = null;
            var hasVvar = vvar is not null && !activeCoords.IsEmpty;

            for (int i = 0; i < glyphIndices.Length; i++)
            {
                ushort glyphIndex = glyphIndices[i];

                if (glyphIndex >= _numGlyphs)
                {
                    return false;
                }

                ushort advanceHeight;
                short topSideBearing;

                if (glyphIndex < _numOfVMetrics)
                {
                    var entryOffset = glyphIndex * 4;
                    advanceHeight = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(entryOffset, 2));
                    topSideBearing = BinaryPrimitives.ReadInt16BigEndian(data.Slice(entryOffset + 2, 2));
                }
                else
                {
                    if (!lastAdvanceHeight.HasValue)
                    {
                        lastAdvanceHeight = BinaryPrimitives.ReadUInt16BigEndian(
                            data.Slice((_numOfVMetrics - 1) * 4, 2));
                    }
                    advanceHeight = lastAdvanceHeight.Value;

                    var tsbIndex = glyphIndex - _numOfVMetrics;
                    var tsbOffset = _numOfVMetrics * 4 + tsbIndex * 2;

                    // A truncated table may omit some or all of the trailing topSideBearing array;
                    // treat a missing entry as zero rather than reading past the end.
                    topSideBearing = tsbOffset + 2 <= data.Length
                        ? BinaryPrimitives.ReadInt16BigEndian(data.Slice(tsbOffset, 2))
                        : (short)0;
                }

                if (hasVvar)
                {
                    if (vvar!.TryGetAdvanceHeightDelta(glyphIndex, activeCoords, out var advDelta) && advDelta != 0f)
                    {
                        var adjusted = advanceHeight + (int)MathF.Round(advDelta);
                        advanceHeight = adjusted < 0
                            ? (ushort)0
                            : (ushort)Math.Min(adjusted, ushort.MaxValue);
                    }

                    if (vvar.TryGetTopSideBearingDelta(glyphIndex, activeCoords, out var tsbDelta) && tsbDelta != 0f)
                    {
                        var adjusted = topSideBearing + (int)MathF.Round(tsbDelta);
                        topSideBearing = (short)Math.Clamp(adjusted, short.MinValue, short.MaxValue);
                    }
                }

                metrics[i] = new VerticalGlyphMetric(advanceHeight, topSideBearing);
            }

            return true;
        }
    }
}
