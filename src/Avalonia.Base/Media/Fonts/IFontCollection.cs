using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    public interface IFontCollection : IReadOnlyList<FontFamily>, IDisposable
    {
        /// <summary>
        /// Get the font collection's key.
        /// </summary>
        Uri Key { get; }

        /// <summary>
        /// Initializes the font collection.
        /// </summary>
        /// <param name="fontManager">The font manager the collection is registered with.</param>
        void Initialize(IFontManagerImpl fontManager);

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
            FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface);
    }
}
