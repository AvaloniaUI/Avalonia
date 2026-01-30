using System;

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
            _numOfVMetrics = numOfVMetrics;
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

            if (glyphIndex >= _numGlyphs)
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

                reader.Seek(tsbOffset);

                short tsb = reader.ReadInt16();

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

            if (glyphIndex >= _numGlyphs)
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
        /// </summary>
        /// <param name="glyphIndices">Read-only span of glyph indices to query.</param>
        /// <param name="advances">Output span to write the advance heights. Must be at least as long as <paramref name="glyphIndices"/>.</param>
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

            // Cache the last advance height for glyphs beyond numOfVMetrics
            ushort? lastAdvanceHeight = null;

            for (int i = 0; i < glyphIndices.Length; i++)
            {
                ushort glyphIndex = glyphIndices[i];

                if (glyphIndex >= _numGlyphs)
                {
                    return false;
                }

                if (glyphIndex < _numOfVMetrics)
                {
                    reader.Seek(glyphIndex * 4);
                    advances[i] = reader.ReadUInt16();
                }
                else
                {
                    // All glyphs beyond numOfVMetrics share the same advance height
                    if (!lastAdvanceHeight.HasValue)
                    {
                        reader.Seek((_numOfVMetrics - 1) * 4);
                        lastAdvanceHeight = reader.ReadUInt16();
                    }

                    advances[i] = lastAdvanceHeight.Value;
                }
            }

            return true;
        }

        /// <summary>
        /// Attempts to retrieve vertical glyph metrics for multiple glyphs in a single operation.
        /// </summary>
        /// <param name="glyphIndices">Read-only span of glyph indices to query.</param>
        /// <param name="metrics">Output span to write the metrics. Must be at least as long as <paramref name="glyphIndices"/>.</param>
        /// <returns><c>true</c> if all glyph indices are valid and metrics were retrieved; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method is more efficient than calling <see cref="TryGetMetrics(ushort, out VerticalGlyphMetric)"/> multiple times as it reuses
        /// the same reader and span reference. If any glyph index is invalid, the method returns <c>false</c>
        /// and the contents of <paramref name="metrics"/> are undefined.
        /// </remarks>
        public bool TryGetMetrics(ReadOnlySpan<ushort> glyphIndices, Span<VerticalGlyphMetric> metrics)
        {
            if (metrics.Length < glyphIndices.Length)
            {
                return false;
            }

            var data = _data.Span;
            var reader = new BigEndianBinaryReader(data);

            // Cache the last advance height for glyphs beyond numOfVMetrics
            ushort? lastAdvanceHeight = null;

            for (int i = 0; i < glyphIndices.Length; i++)
            {
                ushort glyphIndex = glyphIndices[i];

                if (glyphIndex >= _numGlyphs)
                {
                    return false;
                }

                if (glyphIndex < _numOfVMetrics)
                {
                    reader.Seek(glyphIndex * 4);

                    ushort advanceHeight = reader.ReadUInt16();
                    short topSideBearing = reader.ReadInt16();

                    metrics[i] = new VerticalGlyphMetric(advanceHeight, topSideBearing);
                }
                else
                {
                    // All glyphs beyond numOfVMetrics share the same advance height
                    if (!lastAdvanceHeight.HasValue)
                    {
                        reader.Seek((_numOfVMetrics - 1) * 4);
                        lastAdvanceHeight = reader.ReadUInt16();
                    }

                    int tsbIndex = glyphIndex - _numOfVMetrics;
                    int tsbOffset = _numOfVMetrics * 4 + tsbIndex * 2;

                    reader.Seek(tsbOffset);
                    short topSideBearing = reader.ReadInt16();

                    metrics[i] = new VerticalGlyphMetric(lastAdvanceHeight.Value, topSideBearing);
                }
            }

            return true;
        }
    }
}
