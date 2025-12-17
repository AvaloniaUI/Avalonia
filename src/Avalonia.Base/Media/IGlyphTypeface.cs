using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Avalonia.Media.Fonts;
using Avalonia.Media.Fonts.Tables.Colr;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.Platform;

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
    [NotClientImplementable]
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
        int GlyphCount { get; }

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

        /// <summary>
        /// Retrieves a glyph drawing for the specified glyph ID, applying the given transformation and optional font
        /// variation settings.
        /// </summary>
        /// <param name="glyphId">The identifier of the glyph to retrieve. Must correspond to a valid glyph in the font.</param>
        /// <param name="variation">Optional font variation settings to use when selecting the glyph outline. If null, default font variations
        /// are used.</param>
        /// <returns>An object representing the glyph drawing with the applied transformation and variation settings, or null if
        /// the glyph cannot be found.</returns>
        IGlyphDrawing? GetGlyphDrawing(ushort glyphId, FontVariationSettings? variation = null);

        /// <summary>
        /// Retrieves the vector outline geometry for the specified glyph, applying the given transformation and font
        /// variation settings.
        /// </summary>
        /// <param name="glyphId">The identifier of the glyph to retrieve the outline for. Must correspond to a valid glyph in the font.</param>
        /// <param name="transform">A transformation matrix to apply to the glyph outline. This can be used to scale, rotate, or otherwise
        /// transform the geometry.</param>
        /// <param name="variation">Optional font variation settings to use when retrieving the glyph outline. If null, the default font
        /// variations are used.</param>
        /// <returns>A Geometry object representing the transformed outline of the specified glyph, or null if the glyph does not
        /// have an outline.</returns>
        Geometry? GetGlyphOutline(ushort glyphId, Matrix transform, FontVariationSettings? variation = null);
    }

    /// <summary>
    /// Defines the contract for a glyph drawing, providing direct rendering capabilities.
    /// </summary>
    /// <remarks>Implementations of this interface represent a visual glyph that can be rendered
    /// directly to a drawing context. The internal structure (layers, paints, etc.) is an implementation
    /// detail.</remarks>
    public interface IGlyphDrawing
    {
        /// <summary>
        /// Gets the drawing type used to render the glyph.
        /// </summary>
        GlyphDrawingType Type { get; }

        /// <summary>
        /// Gets the bounds of the glyph drawing.
        /// </summary>
        Rect Bounds { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="origin"></param>
        void Draw(DrawingContext context, Point origin);
    }
}
