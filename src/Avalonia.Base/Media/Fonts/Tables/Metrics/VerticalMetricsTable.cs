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

        public static VerticalMetricsTable? Load(IGlyphTypeface glyphTypeface, ushort numberOfVMetrics, int glyphCount)
        {
            if (glyphTypeface.PlatformTypeface.TryGetTable(Tag, out var table))
            {
                return new VerticalMetricsTable(table, numberOfVMetrics, glyphCount);
            }

            return null;
        }

        /// <summary>
        /// Retrieves the vertical glyph metrics for the specified glyph index.
        /// </summary>
        /// <param name="glyphIndex">The index of the glyph for which to retrieve metrics.</param>
        /// <returns>A <see cref="VerticalGlyphMetric"/> containing the vertical metrics for the specified glyph.</returns>
        public VerticalGlyphMetric GetMetrics(ushort glyphIndex)
        {
            // Validate glyph index
            if (glyphIndex >= _numGlyphs)
            {
                throw new ArgumentOutOfRangeException(nameof(glyphIndex), $"Glyph index {glyphIndex} is out of range.");
            }

            var reader = new BigEndianBinaryReader(_data.Span);

            if (glyphIndex < _numOfVMetrics)
            {
                // Each record is 4 bytes
                reader.Seek(glyphIndex * 4);

                ushort advanceHeight = reader.ReadUInt16();
                short topSideBearing = reader.ReadInt16();

                return new VerticalGlyphMetric(advanceHeight, topSideBearing);
            }
            else
            {
                // Last advance height
                reader.Seek((_numOfVMetrics - 1) * 4);

                ushort lastAdvanceHeight = reader.ReadUInt16();

                // Offset into trailing TSB array
                int tsbIndex = glyphIndex - _numOfVMetrics;
                int tsbOffset = _numOfVMetrics * 4 + tsbIndex * 2;

                reader.Seek(tsbOffset);

                short tsb = reader.ReadInt16();

                return new VerticalGlyphMetric(lastAdvanceHeight, tsb);
            }
        }

        /// <summary>
        /// Retrieves the advance height for a single glyph.
        /// </summary>
        /// <param name="glyphIndex">Glyph index to query.</param>
        /// <returns>Advance height for the glyph.</returns>
        public ushort GetAdvance(ushort glyphIndex)
        {
            // Validate glyph index
            if (glyphIndex >= _numGlyphs)
            {
                throw new ArgumentOutOfRangeException(nameof(glyphIndex));
            }

            var reader = new BigEndianBinaryReader(_data.Span);

            if (glyphIndex < _numOfVMetrics)
            {
                // Each record is 4 bytes
                reader.Seek(glyphIndex * 4);

                ushort advanceHeight = reader.ReadUInt16();

                return advanceHeight;
            }
            else
            {
                // Last advance height
                reader.Seek((_numOfVMetrics - 1) * 4);

                ushort lastAdvanceHeight = reader.ReadUInt16();

                return lastAdvanceHeight;
            }
        }
    }
}
