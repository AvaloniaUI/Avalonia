using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// Represents a collection of font families and provides methods for querying and managing font typefaces
    /// within the collection.
    /// </summary>
    /// <remarks>Implementations of this interface allow applications to retrieve font families, match
    /// characters to typefaces, and obtain glyph typefaces based on specific font properties.</remarks>
    public interface IFontCollection : IReadOnlyList<FontFamily>, IDisposable
    {
        /// <summary>
        /// Get the font collection's key.
        /// </summary>
        Uri Key { get; }

        /// <summary>
        /// Try to get a glyph typeface for given parameters.
        /// </summary>
        /// <param name="familyName">The family name.</param>
        /// <param name="style">The font style.</param>
        /// <param name="weight">The font weight.</param>
        /// <param name="stretch">The font stretch.</param>
        /// <param name="glyphTypeface">The glyph typeface.</param>
        /// <returns>Returns <c>true</c> if a glyph typface can be found; otherwise, <c>false</c></returns>
        bool TryGetGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
            FontStretch stretch, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface);

        /// <summary>
        ///     Tries to match a specified character to a <see cref="Typeface"/> that supports specified font properties.
        /// </summary>
        /// <param name="codepoint">The codepoint to match against.</param>
        /// <param name="fontStyle">The font style.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <param name="fontStretch">The font stretch.</param>
        /// <param name="familyName">The family name. This is optional and used for fallback lookup.</param>
        /// <param name="culture">The culture.</param>
        /// <param name="typeface">The matching <see cref="Typeface"/>.</param>
        /// <returns>
        ///     <c>True</c>, if the <see cref="FontManager"/> could match the character to specified parameters, <c>False</c> otherwise.
        /// </returns>
        bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight,
            FontStretch fontStretch, string? familyName, CultureInfo? culture, out Typeface typeface);

        /// <summary>
        /// Tries to get a list of typefaces for the specified family name.
        /// </summary>
        /// <param name="familyName">The family name.</param>
        /// <param name="familyTypefaces">The list of typefaces.</param>
        /// <returns>
        ///     <c>True</c>, if the <see cref="IFontCollection"/> could get the list of typefaces, <c>False</c> otherwise.
        /// </returns>
        bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces);

        /// <summary>
        /// Try to get a synthetic glyph typeface for given parameters.
        /// </summary>
        /// <param name="glyphTypeface">The glyph typeface we try to synthesize.</param>
        /// <param name="style">The font style.</param>
        /// <param name="weight">The font weight.</param>
        /// <param name="stretch">The font stretch.</param>
        /// <param name="syntheticGlyphTypeface"></param>
        /// <returns>Returns <c>true</c> if a synthetic glyph typface can be created; otherwise, <c>false</c></returns>
        bool TryCreateSyntheticGlyphTypeface(GlyphTypeface glyphTypeface, FontStyle style, FontWeight weight, FontStretch stretch,
            [NotNullWhen(true)] out GlyphTypeface? syntheticGlyphTypeface);

        /// <summary>
        /// Attempts to retrieve the glyph typeface that most closely matches the specified font family name, style,
        /// weight, and stretch.
        /// </summary>
        /// <remarks>This method searches for a glyph typeface in the font collection cache that matches
        /// the specified parameters. If an exact match is not found, fallback mechanisms are applied to find the
        /// closest available match based on the specified style, weight, and stretch. If no suitable match is found,
        /// the method returns <see langword="false"/> and <paramref name="glyphTypeface"/> is set to <see
        /// langword="null"/>.</remarks>
        /// <param name="familyName">The name of the font family to search for. This parameter cannot be <see langword="null"/> or empty.</param>
        /// <param name="style">The desired font style.</param>
        /// <param name="weight">The desired font weight.</param>
        /// <param name="stretch">The desired font stretch.</param>
        /// <param name="glyphTypeface">When this method returns, contains the <see cref="GlyphTypeface"/> that most closely matches the specified
        /// parameters, if a match is found; otherwise, <see langword="null"/>. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if a matching glyph typeface is found; otherwise, <see langword="false"/>.</returns>
        bool TryGetNearestMatch(string familyName, FontStyle style, FontWeight weight, FontStretch stretch, 
            [NotNullWhen(true)] out GlyphTypeface? glyphTypeface);
    }
}
