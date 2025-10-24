using System;

namespace Avalonia.Media.Fonts.Tables.Metrics
{
    internal class HorizontalMetricsTable
    {
        public const string TagName = "hmtx";
        public static OpenTypeTag Tag { get; } = OpenTypeTag.Parse(TagName);

        private readonly ReadOnlyMemory<byte> _data;
        private readonly ushort _numOfHMetrics;
        private readonly uint _numGlyphs;

        private HorizontalMetricsTable(ReadOnlyMemory<byte> data, ushort numOfHMetrics, uint numGlyphs)
        {
            _data = data;
            _numOfHMetrics = numOfHMetrics;
            _numGlyphs = numGlyphs;
        }

        internal static HorizontalMetricsTable? Load(IGlyphTypeface glyphTypeface, ushort numberOfHMetrics, uint glyphCount)
        {
            if (glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var table))
            {
                return new HorizontalMetricsTable(table, numberOfHMetrics, glyphCount);
            }

            return null;
        }

        /// <summary>
        /// Retrieves the horizontal glyph metrics for the specified glyph index.
        /// </summary>
        /// <remarks>This method retrieves the horizontal metrics for a single glyph by its index. The
        /// returned metrics include information such as advance width, left side bearing, and other glyph-specific
        /// data.</remarks>
        /// <param name="glyphIndex">The index of the glyph for which to retrieve metrics. Must be a valid glyph index within the font.</param>
        /// <returns>A <see cref="HorizontalGlyphMetric"/> object containing the horizontal metrics for the specified glyph.</returns>
        public HorizontalGlyphMetric GetMetrics(ushort glyphIndex)
        {
            // Validate glyph index
            if (glyphIndex >= _numGlyphs)
            {
                throw new ArgumentOutOfRangeException(nameof(glyphIndex), $"Glyph index {glyphIndex} is out of range.");
            }

            var reader = new BigEndianBinaryReader(_data.Span);

            if (glyphIndex < _numOfHMetrics)
            {
                // Each record is 4 bytes
                reader.Seek(glyphIndex * 4);

                ushort advanceWidth = reader.ReadUInt16();
                short leftSideBearing = reader.ReadInt16();

                return new HorizontalGlyphMetric(advanceWidth, leftSideBearing);
            }
            else
            {
                // Last advance width
                reader.Seek((_numOfHMetrics - 1) * 4);

                ushort lastAdvanceWidth = reader.ReadUInt16();

                // Offset into trailing LSB array
                int lsbIndex = glyphIndex - _numOfHMetrics;
                int lsbOffset = _numOfHMetrics * 4 + lsbIndex * 2;

                reader.Seek(lsbOffset);

                short leftSideBearing = reader.ReadInt16();

                return new HorizontalGlyphMetric(lastAdvanceWidth, leftSideBearing);
            }
        }

        /// <summary>
        /// Retrieves the advance width for a single glyph.
        /// </summary>
        /// <param name="glyphIndex">Glyph index to query.</param>
        /// <returns>Advance width for the glyph.</returns>
        public ushort GetAdvance(ushort glyphIndex)
        {
            // Validate glyph index
            if (glyphIndex >= _numGlyphs)
            {
                throw new ArgumentOutOfRangeException(nameof(glyphIndex));
            }

            var reader = new BigEndianBinaryReader(_data.Span);

            if (glyphIndex < _numOfHMetrics)
            {
                // Each record is 4 bytes
                reader.Seek(glyphIndex * 4);

                ushort advanceWidth = reader.ReadUInt16();

                return advanceWidth;
            }
            else
            {
                // Last advance width
                reader.Seek((_numOfHMetrics - 1) * 4);

                ushort lastAdvanceWidth = reader.ReadUInt16();

                return lastAdvanceWidth;
            }
        }
    }
}
