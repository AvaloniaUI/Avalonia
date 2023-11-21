using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Avalonia.Media;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable]
    public interface IFontManagerImpl
    {
        /// <summary>
        ///     Gets the system's default font family's name.
        /// </summary>
        string GetDefaultFontFamilyName();

        /// <summary>
        ///     Get all installed fonts in the system.
        /// <param name="checkForUpdates">If <c>true</c> the font collection is updated.</param>
        /// </summary>
        string[] GetInstalledFontFamilyNames(bool checkForUpdates = false);

        /// <summary>
        ///     Tries to match a specified character to a typeface that supports specified font properties.
        /// </summary>
        /// <param name="codepoint">The codepoint to match against.</param>
        /// <param name="fontStyle">The font style.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <param name="fontStretch">The font stretch.</param>
        /// <param name="culture">The culture.</param>
        /// <param name="typeface">The matching typeface.</param>
        /// <returns>
        ///     <c>True</c>, if the <see cref="IFontManagerImpl"/> could match the character to specified parameters, <c>False</c> otherwise.
        /// </returns>
        bool TryMatchCharacter(int codepoint, FontStyle fontStyle,
            FontWeight fontWeight, FontStretch fontStretch, CultureInfo? culture, out Typeface typeface);

        /// <summary>
        ///     Tries to get a glyph typeface for specified parameters.
        /// </summary>
        /// <param name="familyName">The family name.</param>
        /// <param name="style">The font style.</param>
        /// <param name="weight">The font weiht.</param>
        /// <param name="stretch">The font stretch.</param>
        /// <param name="glyphTypeface">The created glyphTypeface</param>
        /// <returns>
        ///     <c>True</c>, if the <see cref="IFontManagerImpl"/> could create the glyph typeface, <c>False</c> otherwise.
        /// </returns>
        bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
            FontStretch stretch, [NotNullWhen(returnValue: true)] out IGlyphTypeface? glyphTypeface);

        /// <summary>
        ///     Tries to create a glyph typeface from specified stream.
        /// </summary>
        /// <param name="stream">A stream that holds the font's data.</param>
        /// <param name="fontSimulations">Specifies algorithmic style simulations.</param>
        /// <param name="glyphTypeface">The created glyphTypeface</param>
        /// <returns>
        ///     <c>True</c>, if the <see cref="IFontManagerImpl"/> could create the glyph typeface, <c>False</c> otherwise.
        /// </returns>
        bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations, [NotNullWhen(returnValue: true)] out IGlyphTypeface? glyphTypeface);
    }
}
