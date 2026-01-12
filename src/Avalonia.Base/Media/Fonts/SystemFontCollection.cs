using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    internal class SystemFontCollection : FontCollectionBase
    {
        private readonly IFontManagerImpl _platformImpl;

        public SystemFontCollection(IFontManagerImpl platformImpl)
        {
            _platformImpl = platformImpl ?? throw new ArgumentNullException(nameof(platformImpl));

            var familyNames = _platformImpl.GetInstalledFontFamilyNames().Where(x => !string.IsNullOrEmpty(x));

            foreach (var familyName in familyNames)
            {
                AddFontFamily(familyName);
            }
        }

        public override Uri Key => FontManager.SystemFontsKey;

        public override bool TryGetGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
            FontStretch stretch, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
        {
            var typeface = new Typeface(familyName, style, weight, stretch).Normalize(out familyName);

            if (base.TryGetGlyphTypeface(familyName, style, weight, stretch, out glyphTypeface))
            {
                return true;
            }

            var key = typeface.ToFontCollectionKey();

            //Check cache first to avoid unnecessary calls to the font manager
            if (_glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces) && glyphTypefaces.TryGetValue(key, out glyphTypeface))
            {
                return glyphTypeface != null;
            }

            //Try to create the glyph typeface via system font manager
            if (!_platformImpl.TryCreateGlyphTypeface(familyName, style, weight, stretch, out var platformTypeface))
            {
                //Add null to cache to avoid future calls
                TryAddGlyphTypeface(familyName, key, null);

                return false;
            }

            glyphTypeface = new GlyphTypeface(platformTypeface);

            //Add to cache with platform typeface family name first
            TryAddGlyphTypeface(platformTypeface.FamilyName, key, glyphTypeface);

            //Add to cache
            if (!TryAddGlyphTypeface(glyphTypeface))
            {
                // Another thread may have added an entry for this key while we were creating the glyph typeface.
                // Re-check the cache and yield the existing glyph typeface if present.
                if (_glyphTypefaceCache.TryGetValue(familyName, out var existingMap) && existingMap.TryGetValue(key, out var existingTypeface) && existingTypeface != null)
                {
                    glyphTypeface = existingTypeface;

                    return true;
                }

                return false;
            }

            //Requested glyph typeface should be in cache now
            return base.TryGetGlyphTypeface(familyName, style, weight, stretch, out glyphTypeface);
        }

        public override bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
        {
            return _platformImpl.TryGetFamilyTypefaces(familyName, out familyTypefaces);
        }

        public override bool TryMatchCharacter(int codepoint, FontStyle style, FontWeight weight, FontStretch stretch, string? familyName,
           CultureInfo? culture, out Typeface match)
        {
            var requestedKey = new FontCollectionKey { Style = style, Weight = weight, Stretch = stretch };

            if (base.TryMatchCharacter(codepoint, style, weight, stretch, familyName, culture, out match))
            {
                var matchKey = match.ToFontCollectionKey();

                if (requestedKey == matchKey)
                {
                    return true;
                }
            }

            if (_platformImpl.TryMatchCharacter(codepoint, style, weight, stretch, familyName, culture, out var platformTypeface))
            {
                // Construct the resulting Typeface
                match = new Typeface(platformTypeface.FamilyName, platformTypeface.Style, platformTypeface.Weight,
                       platformTypeface.Stretch);

                // Compute the key for cache lookup this can be different from the requested key
                var key = match.ToFontCollectionKey();

                // Check cache first: if an entry exists and is non-null, match succeeded and we can return true.
                if (_glyphTypefaceCache.TryGetValue(platformTypeface.FamilyName, out var glyphTypefaces) && glyphTypefaces.TryGetValue(key, out var existing))
                {
                    return existing != null;
                }

                // Not in cache yet: create glyph typeface and try to add it.
                var glyphTypeface = new GlyphTypeface(platformTypeface);

                // Try adding with the platform typeface family name first.
                TryAddGlyphTypeface(platformTypeface.FamilyName, key, glyphTypeface);

                // Try adding the glyph typeface with the matched key.
                if (TryAddGlyphTypeface(glyphTypeface, key))
                {
                    return true;
                }

                // TryAddGlyphTypeface failed: another thread may have added an entry. Re-check the cache.
                if (_glyphTypefaceCache.TryGetValue(platformTypeface.FamilyName, out glyphTypefaces) && glyphTypefaces.TryGetValue(key, out existing))
                {
                    return existing != null;
                }

                return false;
            }

            return false;
        }
    }
}
