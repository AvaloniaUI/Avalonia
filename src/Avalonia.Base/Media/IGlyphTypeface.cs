﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Avalonia.Media.Fonts;
using Avalonia.Metadata;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a typeface that provides access to font-related information and operations,  such as glyph metrics,
    /// supported OpenType features, and culture-specific names.
    /// </summary>
    /// <remarks>The <see cref="IGlyphTypeface"/> interface is designed for advanced text rendering and layout
    /// scenarios.  It provides detailed information about a font's characteristics, including its family name, style,
    /// weight,  and stretch, as well as mappings between Unicode code points and glyph indices. <para> This interface
    /// also supports retrieving culture-specific names for font families and faces,  accessing OpenType features, and
    /// obtaining glyph metrics for precise text shaping and rendering. </para> <para> Implementations of this interface
    /// are expected to be disposable, as they may hold unmanaged resources  related to font handling. </para></remarks>
    [Unstable]
    public interface IGlyphTypeface : IDisposable
    {
        /// <summary>
        /// Gets the family name.
        /// </summary>
        string FamilyName { get; }

        /// <summary>
        /// Gets the typographic family name.
        /// </summary>
        /// <remarks>
        /// The typographic family name is an alternate family name that may be used for stylistic or typographic purposes.
        /// <para>
        /// Example: For the fonts "Inter Light" and "Inter Condensed", the <c>FamilyName</c> values are "Inter Light" and "Inter Condensed" respectively,
        /// but both share the same <c>TypographicFamilyName</c> of "Inter".
        /// </para>
        /// </remarks>
        string TypographicFamilyName { get; }

        /// <summary>
        /// Gets a read-only dictionary that maps culture-specific information to the family name.
        /// </summary>
        /// <remarks>This property provides localized family names for different cultures. The dictionary is never empty.
        /// If a specific culture is not present in the dictionary, the caller may need to handle fallback logic to a default culture
        /// or name.</remarks>
        IReadOnlyDictionary<CultureInfo, string> FamilyNames { get; }

        /// <summary>
        /// Gets a read-only list of supported OpenType features.
        /// </summary>
        IReadOnlyList<OpenTypeTag> SupportedFeatures { get; }

        /// <summary>
        /// Gets a read-only dictionary that maps culture-specific information to corresponding face names.
        /// </summary>
        /// <remarks>
        /// The dictionary provides a way to retrieve face names localized for specific cultures.
        /// If a culture is not present in the dictionary, it indicates that no face name is defined for that
        /// culture.
        /// <para>
        /// Example: For a font family "Arial", common face names include "Regular", "Bold", "Italic", "Bold Italic".
        /// The dictionary might contain entries such as:
        /// <code>
        /// en-US: "Bold Italic"
        /// de-DE: "Fett Kursiv"
        /// </code>
        /// </para>
        /// </remarks>
        IReadOnlyDictionary<CultureInfo, string> FaceNames { get; }

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
        ///     Gets the number of glyphs held by this <see cref="IGlyphTypeface"/> object.
        /// </summary>
        uint GlyphCount { get; }

        /// <summary>
        ///     Gets the algorithmic style simulations applied to <see cref="IGlyphTypeface"/> object.
        /// </summary>
        FontSimulations FontSimulations { get; }

        /// <summary>
        /// Gets the font metrics associated with the current font.
        /// </summary>
        FontMetrics Metrics { get; }

        /// <summary>
        /// Gets the nominal mapping of a Unicode code point to a glyph index as defined by the font 'CMAP' table.
        /// </summary>
        IReadOnlyDictionary<int, ushort> CharacterToGlyphMap { get; }

        /// <summary>
        /// Gets the glyph typeface associated with the <see cref="IGlyphTypeface"/>.
        /// </summary>
        IPlatformTypeface PlatformTypeface { get; }

        /// <summary>
        /// Gets the typeface used for text shaping operations.
        /// </summary>
        /// <remarks>The typeface is used to determine glyphs and their positioning during text shaping. 
        /// This property is typically used in scenarios involving advanced text layout or rendering.</remarks>
        ITextShaperTypeface TextShaperTypeface { get; }

        /// <summary>
        ///     Returns the glyph advance for the specified glyph.
        /// </summary>
        /// <param name="glyph">The glyph.</param>
        /// <returns>
        ///     The advance.
        /// </returns>
        ushort GetGlyphAdvance(ushort glyph);

        /// <summary>
        ///     Tries to get a glyph's metrics in em units.
        /// </summary>
        /// <param name="glyph">The glyph id.</param>
        /// <param name="metrics">The glyph metrics.</param>
        /// <returns>
        ///   <c>true</c> if an glyph's metrics was found, <c>false</c> otherwise.
        /// </returns>
        bool TryGetGlyphMetrics(ushort glyph, out GlyphMetrics metrics);
    }

    public interface IPlatformTypeface : IFontMemory
    {
        /// <summary>
        /// Gets the designed weight of the font represented by the <see cref="IPlatformTypeface"/> object.
        /// </summary>
        FontWeight Weight { get; }

        /// <summary>
        /// Gets the style for the <see cref="IPlatformTypeface"/> object.
        /// </summary>
        FontStyle Style { get; }

        /// <summary>
        /// Gets the <see cref="FontStretch"/> value for the <see cref="IPlatformTypeface"/> object.
        /// </summary>
        FontStretch Stretch { get; }

        /// <summary>
        /// Returns the font file stream represented by the <see cref="IGlyphTypeface"/>.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>Returns <c>true</c> if the stream can be obtained, otherwise <c>false</c>.</returns>
        bool TryGetStream([NotNullWhen(true)] out Stream? stream);
    }

    public interface ITextShaperTypeface : IDisposable
    {

    }

    public interface IFontMemory : IDisposable
    {
        /// <summary>
        /// Attempts to retrieve the memory block associated with the specified OpenType table tag.
        /// </summary>
        /// <param name="tag">The OpenType table tag identifying the table to retrieve.</param>
        /// <param name="table">When this method returns, contains the memory block of the specified table if the operation succeeds;
        /// otherwise, contains an empty memory block. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the memory block for the specified table tag was successfully retrieved;
        /// otherwise, <see langword="false"/>.</returns>
        bool TryGetTable(OpenTypeTag tag, out ReadOnlyMemory<byte> table);
    }
}
