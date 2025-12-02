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

            //Add to cache
            if (!TryAddGlyphTypeface(glyphTypeface))
            {
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
                var glyphTypeface = new GlyphTypeface(platformTypeface);

                match = new Typeface(platformTypeface.FamilyName, platformTypeface.Style, platformTypeface.Weight,
                       platformTypeface.Stretch);

                // Add to cache if not already present
                return TryAddGlyphTypeface(glyphTypeface);
            }

            return false;
        }
    }
}
