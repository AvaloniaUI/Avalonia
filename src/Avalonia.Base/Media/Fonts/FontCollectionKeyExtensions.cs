using System;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    public static class FontCollectionKeyExtensions
    {
        public static FontCollectionKey ToFontCollectionKey(this Typeface typeface)
        {
            return new FontCollectionKey(typeface.Style, typeface.Weight, typeface.Stretch);
        }

        public static FontCollectionKey ToFontCollectionKey(this IGlyphTypeface glyphTypeface)
        {
            if (glyphTypeface == null)
            {
                throw new ArgumentNullException(nameof(glyphTypeface));
            }

            return new FontCollectionKey(glyphTypeface.Style, glyphTypeface.Weight, glyphTypeface.Stretch);
        }

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
