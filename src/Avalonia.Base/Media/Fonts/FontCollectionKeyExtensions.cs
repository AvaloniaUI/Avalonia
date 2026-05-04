using System;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    internal static class FontCollectionKeyExtensions
    {
        /// <summary>
        /// Creates a new FontCollectionKey based on the style, weight, and stretch of the specified Typeface.
        /// </summary>
        /// <param name="typeface">The Typeface from which to extract style, weight, and stretch information. Cannot be null.</param>
        /// <returns>A FontCollectionKey representing the style, weight, and stretch of the specified Typeface.</returns>
        public static FontCollectionKey ToFontCollectionKey(this Typeface typeface)
        {
            return new FontCollectionKey(typeface.Style, typeface.Weight, typeface.Stretch);
        }

        /// <summary>
        /// Creates a new FontCollectionKey based on the style, weight, and stretch of the specified GlyphTypeface.
        /// </summary>
        /// <param name="glyphTypeface">The GlyphTypeface instance from which to extract style, weight, and stretch information. Cannot be null.</param>
        /// <returns>A FontCollectionKey representing the style, weight, and stretch of the specified glyph typeface.</returns>
        /// <exception cref="ArgumentNullException">Thrown if glyphTypeface is null.</exception>
        public static FontCollectionKey ToFontCollectionKey(this GlyphTypeface glyphTypeface)
        {
            if (glyphTypeface == null)
            {
                throw new ArgumentNullException(nameof(glyphTypeface));
            }

            return new FontCollectionKey(glyphTypeface.Style, glyphTypeface.Weight, glyphTypeface.Stretch);
        }

        /// <summary>
        /// Creates a new FontCollectionKey based on the style, weight, and stretch of the specified platform typeface.
        /// </summary>
        /// <param name="platformTypeface">The platform typeface from which to extract style, weight, and stretch information. Cannot be null.</param>
        /// <returns>A FontCollectionKey representing the style, weight, and stretch of the specified platform typeface.</returns>
        /// <exception cref="ArgumentNullException">Thrown if platformTypeface is null.</exception>
        public static FontCollectionKey ToFontCollectionKey(this IPlatformTypeface platformTypeface)
        {
            if (platformTypeface == null)
            {
                throw new ArgumentNullException(nameof(platformTypeface));
            }

            return new FontCollectionKey(platformTypeface.Style, platformTypeface.Weight, platformTypeface.Stretch);
        }
    }
}
