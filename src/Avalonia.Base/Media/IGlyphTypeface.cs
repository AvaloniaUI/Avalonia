using System;
using Avalonia.Metadata;

namespace Avalonia.Media
{
    [Unstable]
    public interface IGlyphTypeface : IDisposable
    {
        /// <summary>
        /// Gets the family name for the <see cref="IGlyphTypeface"/> object.
        /// </summary>
        string FamilyName { get; }

        /// <summary>
        /// Gets the designed weight of the font represented by the <see cref="IGlyphTypeface"/> object.
        /// </summary>
        FontWeight Weight { get; }

        /// <summary>
        /// Gets the style for the <see cref="IGlyphTypeface"/> object.
        /// </summary>
        FontStyle Style { get; }

        /// <summary>
        /// Gets the <see cref="FontStretch"/> value for the <see cref="IGlyphTypeface"/> object.
        /// </summary>
        FontStretch Stretch { get; }

        /// <summary>
        ///     Gets the number of glyphs held by this glyph typeface. 
        /// </summary>
        int GlyphCount { get; }

        /// <summary>
        ///     Gets the font metrics.
        /// </summary>
        /// <returns>
        ///     The font metrics.
        /// </returns>
        FontMetrics Metrics { get; }

        /// <summary>
        ///     Gets the algorithmic style simulations applied to this glyph typeface.
        /// </summary>
        FontSimulations FontSimulations { get; }

        /// <summary>
        ///     Tries to get a glyph's metrics in em units.
        /// </summary>
        /// <param name="glyph">The glyph id.</param>
        /// <param name="metrics">The glyph metrics.</param>
        /// <returns>
        ///   <c>true</c> if an glyph's metrics was found, <c>false</c> otherwise.
        /// </returns>
        bool TryGetGlyphMetrics(ushort glyph, out GlyphMetrics metrics);
        
        /// <summary>
        ///     Returns an glyph index for the specified codepoint.
        /// </summary>
        /// <remarks>
        ///     Returns <c>0</c> if a glyph isn't found.
        /// </remarks>
        /// <param name="codepoint">The codepoint.</param>
        /// <returns>
        ///     A glyph index.
        /// </returns>
        ushort GetGlyph(uint codepoint);

        /// <summary>
        ///     Tries to get an glyph index for specified codepoint.
        /// </summary>
        /// <param name="codepoint">The codepoint.</param>
        /// <param name="glyph">A glyph index.</param>
        /// <returns>
        ///     <c>true</c> if an glyph index was found, <c>false</c> otherwise.
        /// </returns>
        bool TryGetGlyph(uint codepoint, out ushort glyph);

        /// <summary>
        ///     Returns an array of glyph indices. Codepoints that are not represented by the font are returned as <code>0</code>.
        /// </summary>
        /// <param name="codepoints">The codepoints to map.</param>
        /// <returns>
        ///     An array of glyph indices.
        /// </returns>
        ushort[] GetGlyphs(ReadOnlySpan<uint> codepoints);

        /// <summary>
        ///     Returns the glyph advance for the specified glyph.
        /// </summary>
        /// <param name="glyph">The glyph.</param>
        /// <returns>
        ///     The advance.
        /// </returns>
        int GetGlyphAdvance(ushort glyph);

        /// <summary>
        ///     Returns an array of glyph advances in design em size.
        /// </summary>
        /// <param name="glyphs">The glyph indices.</param>
        /// <returns>
        ///     An array of glyph advances.
        /// </returns>
        int[] GetGlyphAdvances(ReadOnlySpan<ushort> glyphs);

        /// <summary>
        ///     Returns the contents of the table data for the specified tag.
        /// </summary>
        /// <param name="tag">The table tag to get the data for.</param>
        /// <param name="table">The contents of the table data for the specified tag.</param>
        /// <returns>Returns <c>true</c> if the content exists, otherwise <c>false</c>.</returns>
        bool TryGetTable(uint tag, out byte[] table);
    }
}
