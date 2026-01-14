using System;

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
        /// </summary>
        /// <param name="glyphIndices">Read-only span of glyph indices to query.</param>
        /// <param name="advances">Output span to write the advance widths. Must be at least as long as <paramref name="glyphIndices"/>.</param>
        /// <returns><c>true</c> if all glyph indices are valid and advances were retrieved; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method is more efficient than calling <see cref="TryGetAdvance"/> multiple times as it reuses
        /// the same reader and span reference. If any glyph index is invalid, the method returns <c>false</c>
        /// and the contents of <paramref name="advances"/> are undefined.
        /// </remarks>
        public bool TryGetAdvances(ReadOnlySpan<ushort> glyphIndices, Span<ushort> advances)
        {
            if (advances.Length < glyphIndices.Length)
            {
                return false;
            }

            var data = _data.Span;
            var reader = new BigEndianBinaryReader(data);

            // Cache the last advance width for glyphs beyond numOfHMetrics
            ushort? lastAdvanceWidth = null;

            for (int i = 0; i < glyphIndices.Length; i++)
            {
                ushort glyphIndex = glyphIndices[i];

                if (glyphIndex >= _numGlyphs)
                {
                    return false;
                }

                if (glyphIndex < _numOfHMetrics)
                {
                    reader.Seek(glyphIndex * 4);
                    advances[i] = reader.ReadUInt16();
                }
                else
                {
                    // All glyphs beyond numOfHMetrics share the same advance width
                    if (!lastAdvanceWidth.HasValue)
                    {
                        reader.Seek((_numOfHMetrics - 1) * 4);
                        lastAdvanceWidth = reader.ReadUInt16();
                    }

                    advances[i] = lastAdvanceWidth.Value;
                }
            }

            return true;
        }

        /// <summary>
        /// Attempts to retrieve horizontal glyph metrics for multiple glyphs in a single operation.
        /// </summary>
        /// <param name="glyphIndices">Read-only span of glyph indices to query.</param>
        /// <param name="metrics">Output span to write the metrics. Must be at least as long as <paramref name="glyphIndices"/>.</param>
        /// <returns><c>true</c> if all glyph indices are valid and metrics were retrieved; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method is more efficient than calling <see cref="TryGetMetrics(ushort, out HorizontalGlyphMetric)"/> multiple times as it reuses
        /// the same reader and span reference. If any glyph index is invalid, the method returns <c>false</c>
        /// and the contents of <paramref name="metrics"/> are undefined.
        /// </remarks>
        public bool TryGetMetrics(ReadOnlySpan<ushort> glyphIndices, Span<HorizontalGlyphMetric> metrics)
        {
            if (metrics.Length < glyphIndices.Length)
            {
                return false;
            }

            var data = _data.Span;
            var reader = new BigEndianBinaryReader(data);

            // Cache the last advance width for glyphs beyond numOfHMetrics
            ushort? lastAdvanceWidth = null;

            for (int i = 0; i < glyphIndices.Length; i++)
            {
                ushort glyphIndex = glyphIndices[i];

                if (glyphIndex >= _numGlyphs)
                {
                    return false;
                }

                if (glyphIndex < _numOfHMetrics)
                {
                    reader.Seek(glyphIndex * 4);

                    ushort advanceWidth = reader.ReadUInt16();
                    short leftSideBearing = reader.ReadInt16();

                    metrics[i] = new HorizontalGlyphMetric(advanceWidth, leftSideBearing);
                }
                else
                {
                    // All glyphs beyond numOfHMetrics share the same advance width
                    if (!lastAdvanceWidth.HasValue)
                    {
                        reader.Seek((_numOfHMetrics - 1) * 4);
                        lastAdvanceWidth = reader.ReadUInt16();
                    }

                    int lsbIndex = glyphIndex - _numOfHMetrics;
                    int lsbOffset = _numOfHMetrics * 4 + lsbIndex * 2;

                    reader.Seek(lsbOffset);
                    short leftSideBearing = reader.ReadInt16();

                    metrics[i] = new HorizontalGlyphMetric(lastAdvanceWidth.Value, leftSideBearing);
                }
            }

            return true;
        }
    }
}
